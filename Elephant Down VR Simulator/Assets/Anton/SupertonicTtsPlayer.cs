using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Supertonic.Unity
{
    public enum SupertonicLanguage
    {
        en, ko, ja, ar, bg, cs, da, de,el, es, et, fi, fr, hi, hr, hu, id, it, lt, lv, nl, pl, pt, ro, ru, sk, sl, sv, tr, uk, vi
    }

    [RequireComponent(typeof(AudioSource))]
    public sealed class SupertonicTtsPlayer : MonoBehaviour
    {
        static readonly string[] DefaultVoiceStyles = { "F1", "F2", "F3", "F4", "F5", "M1", "M2", "M3", "M4", "M5" };

        static readonly HashSet<string> LanguageCodes = new HashSet<string>(
            Enum.GetNames(typeof(SupertonicLanguage)),
            StringComparer.Ordinal);

        [Header("Inference")]
        [SerializeField] BackendType backendType = BackendType.CPU;
        [SerializeField, Min(1)] int totalStep = 8;
        [SerializeField, Range(0.5f, 2.0f)] float speed = 1.05f;
        [SerializeField] string voiceStyleResourceName = "M1";

        [Header("Sentis FP16 Models")]
        [SerializeField] string durationPredictorModelFile = "duration_predictor_fp16.sentis";
        [SerializeField] string textEncoderModelFile = "text_encoder_fp16.sentis";
        [SerializeField] string vectorEstimatorModelFile = "vector_estimator_fp16.sentis";
        [SerializeField] string vocoderModelFile = "vocoder_fp16.sentis";

        [Header("Speech")]
        [SerializeField] SupertonicLanguage language = SupertonicLanguage.en;
        [SerializeField, TextArea(3, 8)]
        string utteranceText = "A gentle breeze moved through the open window while everyone listened to the story.";

        [Header("Template Text")]
        [SerializeField] bool updateTextOnLanguageChanged = true;
        [SerializeField] string anthemTextFile = "anthem.json";

        [Header("Scene UI")]
        [SerializeField] Dropdown languageDropdown;
        [SerializeField] Dropdown voiceStyleDropdown;
        [SerializeField] InputField textInput;
        [SerializeField] Button playButton;
        [SerializeField] Text buttonText;
        [SerializeField] Text generationTimeText;

        AudioSource audioSource;
        Worker durationWorker;
        Worker textEncoderWorker;
        Worker vectorEstimatorWorker;
        Worker vocoderWorker;
        int sampleRate = 22050;
        int baseChunkSize = 512;
        int chunkCompressFactor = 4;
        int latentDim = 128;
        int[] unicodeIndexer;
        readonly Dictionary<SupertonicLanguage, string> anthemTextsByLanguage = new Dictionary<SupertonicLanguage, string>();
        bool loaded;
        bool busy;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            BindSceneUi();
        }

        void Start()
        {
            if (updateTextOnLanguageChanged)
                StartCoroutine(LoadAnthemTexts());

            StartCoroutine(Warmup());
        }

        void OnDestroy()
        {
            durationWorker?.Dispose();
            textEncoderWorker?.Dispose();
            vectorEstimatorWorker?.Dispose();
            vocoderWorker?.Dispose();
        }

        public void PlayFromCurrentSettings()
        {
            if (!busy)
                StartCoroutine(GenerateAndPlay());
        }

        void BindSceneUi()
        {
            if (buttonText == null && playButton != null)
                buttonText = playButton.GetComponentInChildren<Text>();

            if (languageDropdown != null)
            {
                var languageNames = Enum.GetNames(typeof(SupertonicLanguage));
                SetDropdownOptions(languageDropdown, languageNames);

                languageDropdown.value = (int)language;
                languageDropdown.RefreshShownValue();
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            }

            if (voiceStyleDropdown != null)
            {
                var voiceStyles = GetAvailableVoiceStyles();
                SetDropdownOptions(voiceStyleDropdown, voiceStyles);

                var selectedIndex = voiceStyles.IndexOf(voiceStyleResourceName);
                if (selectedIndex < 0)
                {
                    selectedIndex = Mathf.Max(0, voiceStyles.IndexOf("M1"));
                    voiceStyleResourceName = voiceStyles[selectedIndex];
                }

                voiceStyleDropdown.value = selectedIndex;
                voiceStyleDropdown.RefreshShownValue();
                voiceStyleDropdown.onValueChanged.AddListener(OnVoiceStyleChanged);
            }

            if (textInput != null)
            {
                textInput.text = utteranceText;
                textInput.onValueChanged.AddListener(OnTextChanged);
            }

            if (playButton != null)
                playButton.onClick.AddListener(PlayFromCurrentSettings);

            SetGenerationTimeText("Generation: --");
        }

        void OnLanguageChanged(int index)
        {
            language = (SupertonicLanguage)Mathf.Clamp(index, 0, Enum.GetValues(typeof(SupertonicLanguage)).Length - 1);

            if (updateTextOnLanguageChanged)
                ApplyAnthemTextForLanguage(language);
        }

        void OnVoiceStyleChanged(int index)
        {
            if (voiceStyleDropdown == null || voiceStyleDropdown.options.Count == 0)
                return;

            var clampedIndex = Mathf.Clamp(index, 0, voiceStyleDropdown.options.Count - 1);
            voiceStyleResourceName = voiceStyleDropdown.options[clampedIndex].text;
        }

        void OnTextChanged(string value)
        {
            utteranceText = value;
        }

        IEnumerator LoadAnthemTexts()
        {
            Exception failure = null;

            yield return ReadStreamingAssetText(
                ParseAnthemTexts,
                error => failure = error,
                "Supertonic",
                "Anthems",
                anthemTextFile);

            if (failure != null)
            {
                Debug.LogWarning($"Failed to load Supertonic anthem text presets: {failure.Message}");
                yield break;
            }

            ApplyAnthemTextForLanguage(language);
        }

        void ParseAnthemTexts(string json)
        {
            anthemTextsByLanguage.Clear();
            var root = JObject.Parse(json);

            foreach (var property in root.Properties())
            {
                if (property.Name.StartsWith("_", StringComparison.Ordinal))
                    continue;

                if (!Enum.TryParse(property.Name, out SupertonicLanguage parsedLanguage))
                    continue;

                var text = property.Value.Type == JTokenType.Object
                    ? property.Value["text"]?.Value<string>()
                    : property.Value.Value<string>();

                if (!string.IsNullOrWhiteSpace(text))
                    anthemTextsByLanguage[parsedLanguage] = text.Trim();
            }
        }

        void ApplyAnthemTextForLanguage(SupertonicLanguage selectedLanguage)
        {
            if (!anthemTextsByLanguage.TryGetValue(selectedLanguage, out var anthemText) ||
                string.IsNullOrWhiteSpace(anthemText))
                return;

            utteranceText = anthemText;

            if (textInput == null)
                return;

            textInput.SetTextWithoutNotify(anthemText);
            textInput.caretPosition = textInput.text.Length;
        }

        static void SetDropdownOptions(Dropdown dropdown, IReadOnlyList<string> options)
        {
            if (dropdown.options.Count == options.Count && dropdown.options.Select(option => option.text).SequenceEqual(options))
                return;

            dropdown.ClearOptions();
            dropdown.AddOptions(options.ToList());
        }

        static List<string> GetAvailableVoiceStyles()
        {
            var voiceStyles = new List<string>();
            var voiceStyleDirectory = GetStreamingAssetPath("Supertonic", "VoiceStyles");

            if (CanReadStreamingAssetsWithFileApi() && Directory.Exists(voiceStyleDirectory))
            {
                voiceStyles = Directory.GetFiles(voiceStyleDirectory, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList();
            }

            if (voiceStyles.Count == 0)
                voiceStyles.AddRange(DefaultVoiceStyles);

            return voiceStyles;
        }

        IEnumerator GenerateAndPlay()
        {
            busy = true;
            SetButtonInteractable(false, "Generating...");
            SetGenerationTimeText("Generation: running...");
            var generationWatch = System.Diagnostics.Stopwatch.StartNew();
            yield return null;

            Exception failure = null;
            var voiceStyleJson = string.Empty;

            yield return EnsureLoaded(error => failure = error);

            if (failure == null)
            {
                yield return ReadStreamingAssetText(
                    text => voiceStyleJson = text,
                    error => failure = error,
                    "Supertonic",
                    "VoiceStyles",
                    $"{GetVoiceStyleAssetName(voiceStyleResourceName)}.json");
            }

            if (failure == null)
            {
                VoiceStyle style = null;
                float[] wav = null;

                try
                {
                    style = ParseVoiceStyle(voiceStyleJson);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }

                try
                {
                    if (failure == null)
                        yield return AwaitResult(
                            SynthesizeAsync(utteranceText, language.ToString(), totalStep, speed, style),
                            result => wav = result,
                            error => failure = error);
                }
                finally
                {
                    style?.Dispose();
                }

                if (failure == null)
                {
                    try
                    {
                        var clip = AudioClip.Create("Supertonic Speech", wav.Length, 1, sampleRate, false);
                        clip.SetData(wav, 0);
                        audioSource.Stop();
                        audioSource.clip = clip;
                        audioSource.Play();
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                    }
                }
            }

            if (failure != null)
                Debug.LogError($"Supertonic synthesis failed: {failure}");

            generationWatch.Stop();
            SetGenerationTimeText(failure == null
                ? $"Generation: {FormatElapsed(generationWatch.Elapsed)}"
                : "Generation: failed");

            SetButtonInteractable(true, "Play");
            busy = false;
        }

        IEnumerator Warmup()
        {
            if (loaded || busy)
                yield break;

            busy = true;
            SetButtonInteractable(false, "Warming...");
            SetGenerationTimeText("Generation: warming up...");
            var warmupWatch = System.Diagnostics.Stopwatch.StartNew();

            Exception failure = null;
            var voiceStyleJson = string.Empty;
            yield return EnsureLoaded(error => failure = error);

            if (failure == null)
            {
                yield return ReadStreamingAssetText(
                    text => voiceStyleJson = text,
                    error => failure = error,
                    "Supertonic",
                    "VoiceStyles",
                    $"{GetVoiceStyleAssetName(voiceStyleResourceName)}.json");
            }

            if (failure == null)
            {
                VoiceStyle style = null;

                try
                {
                    style = ParseVoiceStyle(voiceStyleJson);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }

                try
                {
                    if (failure == null)
                        yield return AwaitResult(
                            SynthesizeAsync(".", language.ToString(), Mathf.Clamp(totalStep, 1, 2), speed, style),
                            _ => { },
                            error => failure = error);
                }
                finally
                {
                    style?.Dispose();
                }
            }

            warmupWatch.Stop();
            if (failure != null)
            {
                Debug.LogError($"Supertonic warmup failed: {failure}");
                SetGenerationTimeText("Generation: warmup failed");
            }
            else
            {
                SetGenerationTimeText($"Generation: warmup {FormatElapsed(warmupWatch.Elapsed)}");
            }

            SetButtonInteractable(true, "Play");
            busy = false;
        }

        IEnumerator EnsureLoaded(Action<Exception> onError)
        {
            if (loaded)
                yield break;

            Exception failure = null;
            var configJson = string.Empty;
            var unicodeIndexerJson = string.Empty;

            yield return ReadStreamingAssetText(
                text => configJson = text,
                error => failure = error,
                "Supertonic",
                "Config",
                "tts.json");

            if (failure == null)
            {
                yield return ReadStreamingAssetText(
                    text => unicodeIndexerJson = text,
                    error => failure = error,
                    "Supertonic",
                    "Config",
                    "unicode_indexer.json");
            }

            if (failure != null)
            {
                onError?.Invoke(failure);
                yield break;
            }

            try
            {
                LoadConfig(configJson);
                LoadUnicodeIndexer(unicodeIndexerJson);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
                yield break;
            }

            Model durationModel = null;
            Model textEncoderRuntimeModel = null;
            Model vectorEstimatorModelRuntime = null;
            Model vocoderRuntimeModel = null;

            yield return LoadStreamingSentisModel(model => durationModel = model, error => failure = error, durationPredictorModelFile);
            if (failure == null)
                yield return LoadStreamingSentisModel(model => textEncoderRuntimeModel = model, error => failure = error, textEncoderModelFile);
            if (failure == null)
                yield return LoadStreamingSentisModel(model => vectorEstimatorModelRuntime = model, error => failure = error, vectorEstimatorModelFile);
            if (failure == null)
                yield return LoadStreamingSentisModel(model => vocoderRuntimeModel = model, error => failure = error, vocoderModelFile);

            if (failure != null)
            {
                onError?.Invoke(failure);
                yield break;
            }

            try
            {
                durationWorker = new Worker(durationModel, backendType);
                textEncoderWorker = new Worker(textEncoderRuntimeModel, backendType);
                vectorEstimatorWorker = new Worker(vectorEstimatorModelRuntime, backendType);
                vocoderWorker = new Worker(vocoderRuntimeModel, backendType);
                loaded = true;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        }

        static IEnumerator LoadStreamingSentisModel(
            Action<Model> onSuccess,
            Action<Exception> onError,
            string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                onError?.Invoke(new InvalidOperationException("Missing .sentis model file name."));
                yield break;
            }

            var path = GetStreamingAssetPath("Supertonic", "Models", fileName);

            if (CanReadStreamingAssetsWithFileApi() && File.Exists(path))
            {
                try
                {
                    onSuccess?.Invoke(ModelLoader.Load(path));
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                }

                yield break;
            }

            var uri = ToStreamingAssetUri(path);
            using var request = UnityWebRequest.Get(uri);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(new IOException($"Failed to read StreamingAssets model: {uri} ({request.error})"));
                yield break;
            }

            try
            {
                using var stream = new MemoryStream(request.downloadHandler.data);
                onSuccess?.Invoke(ModelLoader.Load(stream));
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        }

        void LoadConfig(string json)
        {
            var root = JObject.Parse(json);
            sampleRate = root["ae"]?["sample_rate"]?.Value<int>() ?? sampleRate;
            baseChunkSize = root["ae"]?["base_chunk_size"]?.Value<int>() ?? baseChunkSize;
            chunkCompressFactor = root["ttl"]?["chunk_compress_factor"]?.Value<int>() ?? chunkCompressFactor;
            latentDim = root["ttl"]?["latent_dim"]?.Value<int>() ?? latentDim;
        }

        void LoadUnicodeIndexer(string json)
        {
            unicodeIndexer = JArray.Parse(json)
                .Select(v => v.Value<int>())
                .ToArray();
        }

        async Awaitable<float[]> SynthesizeAsync(string text, string languageCode, int steps, float speechSpeed, VoiceStyle style)
        {
            var chunks = ChunkText(text, languageCode is "ko" or "ja" ? 120 : 300);
            var output = new List<float>();

            for (int i = 0; i < chunks.Count; i++)
            {
                var (wav, duration) = await InferSingleChunkAsync(chunks[i], languageCode, style, steps, speechSpeed);
                var wantedLength = Mathf.Clamp(Mathf.RoundToInt(duration * sampleRate), 0, wav.Length);

                if (i > 0)
                    output.AddRange(new float[Mathf.RoundToInt(0.3f * sampleRate)]);

                for (int n = 0; n < wantedLength; n++)
                    output.Add(Mathf.Clamp(wav[n], -1.0f, 1.0f));
            }

            return output.Count == 0 ? new float[1] : output.ToArray();
        }

        async Awaitable<(float[] wav, float duration)> InferSingleChunkAsync(
            string text,
            string languageCode,
            VoiceStyle style,
            int steps,
            float speechSpeed)
        {
            var (textIds, textMask, textLength) = ProcessText(text, languageCode);
            var textShape = new TensorShape(1, textLength);
            var textMaskShape = new TensorShape(1, 1, textLength);

            using var textIdsTensor = new Tensor<int>(textShape, textIds);
            using var textMaskTensor = new Tensor<float>(textMaskShape, textMask);

            durationWorker.SetInput("text_ids", textIdsTensor);
            durationWorker.SetInput("style_dp", style.StyleDp);
            durationWorker.SetInput("text_mask", textMaskTensor);
            durationWorker.Schedule();
            var duration = await ReadFloatOutputAsync(durationWorker, "duration");
            for (int i = 0; i < duration.Length; i++)
                duration[i] /= speechSpeed;

            textEncoderWorker.SetInput("text_ids", textIdsTensor);
            textEncoderWorker.SetInput("style_ttl", style.StyleTtl);
            textEncoderWorker.SetInput("text_mask", textMaskTensor);
            textEncoderWorker.Schedule();
            var textEmbeddingTensor = textEncoderWorker.PeekOutput("text_emb") as Tensor<float>;
            if (textEmbeddingTensor == null)
                throw new InvalidOperationException("text_encoder did not produce text_emb.");

            var (noise, latentMask, latentLength, latentChannels) = SampleNoisyLatent(duration);
            var latentShape = new TensorShape(1, latentChannels, latentLength);
            var latentMaskShape = new TensorShape(1, 1, latentLength);

            using var latentMaskTensor = new Tensor<float>(latentMaskShape, latentMask);
            using var totalStepTensor = new Tensor<float>(new TensorShape(1), new[] { (float)steps });
            var noisyLatentTensor = new Tensor<float>(latentShape, noise);

            try
            {
                for (int step = 0; step < steps; step++)
                {
                    using var currentStepTensor = new Tensor<float>(new TensorShape(1), new[] { (float)step });
                    vectorEstimatorWorker.SetInput("noisy_latent", noisyLatentTensor);
                    vectorEstimatorWorker.SetInput("text_emb", textEmbeddingTensor);
                    vectorEstimatorWorker.SetInput("style_ttl", style.StyleTtl);
                    vectorEstimatorWorker.SetInput("text_mask", textMaskTensor);
                    vectorEstimatorWorker.SetInput("latent_mask", latentMaskTensor);
                    vectorEstimatorWorker.SetInput("total_step", totalStepTensor);
                    vectorEstimatorWorker.SetInput("current_step", currentStepTensor);
                    vectorEstimatorWorker.Schedule();

                    var denoised = await ReadFloatOutputAsync(vectorEstimatorWorker, "denoised_latent");
                    noisyLatentTensor.Dispose();
                    noisyLatentTensor = new Tensor<float>(latentShape, denoised);
                }

                vocoderWorker.SetInput("latent", noisyLatentTensor);
                vocoderWorker.Schedule();
                var wav = await ReadFloatOutputAsync(vocoderWorker, "wav_tts");
                return (wav, duration.Length > 0 ? duration[0] : wav.Length / (float)sampleRate);
            }
            finally
            {
                noisyLatentTensor.Dispose();
            }
        }

        static async Awaitable<float[]> ReadFloatOutputAsync(Worker worker, string outputName)
        {
            var outputTensor = worker.PeekOutput(outputName) as Tensor<float>;
            if (outputTensor == null)
                throw new InvalidOperationException($"Model did not produce {outputName}.");

            using var cpuTensor = await outputTensor.ReadbackAndCloneAsync();
            return cpuTensor.DownloadToArray();
        }

        static IEnumerator AwaitResult<T>(Awaitable<T> awaitable, Action<T> onResult, Action<Exception> onError)
        {
            var awaiter = awaitable.GetAwaiter();
            var completed = false;
            T result = default;
            Exception failure = null;

            void Complete()
            {
                try
                {
                    result = awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    completed = true;
                }
            }

            if (awaiter.IsCompleted)
                Complete();
            else
                awaiter.OnCompleted(Complete);

            while (!completed)
                yield return null;

            if (failure != null)
                onError?.Invoke(failure);
            else
                onResult?.Invoke(result);
        }

        (int[] textIds, float[] textMask, int length) ProcessText(string text, string languageCode)
        {
            var processed = PreprocessText(text, languageCode);
            var textIds = new int[processed.Length];

            for (int i = 0; i < processed.Length; i++)
            {
                var code = processed[i];
                if (unicodeIndexer != null && code < unicodeIndexer.Length)
                    textIds[i] = unicodeIndexer[code];
            }

            var textMask = new float[processed.Length];
            Array.Fill(textMask, 1.0f);
            return (textIds, textMask, processed.Length);
        }

        static string PreprocessText(string text, string languageCode)
        {
            if (!LanguageCodes.Contains(languageCode))
                throw new ArgumentException($"Invalid language: {languageCode}");

            text = (text ?? string.Empty).Normalize(NormalizationForm.FormKD);
            text = RemoveEmojis(text);

            var replacements = new Dictionary<string, string>
            {
                { "–", "-" },
                { "‑", "-" },
                { "—", "-" },
                { "_", " " },
                { "\u201C", "\"" },
                { "\u201D", "\"" },
                { "\u2018", "'" },
                { "\u2019", "'" },
                { "´", "'" },
                { "`", "'" },
                { "[", " " },
                { "]", " " },
                { "|", " " },
                { "/", " " },
                { "#", " " },
                { "→", " " },
                { "←", " " },
                { "@", " at " },
                { "e.g.,", "for example, " },
                { "i.e.,", "that is, " }
            };

            foreach (var pair in replacements)
                text = text.Replace(pair.Key, pair.Value);

            text = Regex.Replace(text, @"[♥☆♡©\\]", "");
            text = Regex.Replace(text, @" ,", ",");
            text = Regex.Replace(text, @" \.", ".");
            text = Regex.Replace(text, @" !", "!");
            text = Regex.Replace(text, @" \?", "?");
            text = Regex.Replace(text, @" ;", ";");
            text = Regex.Replace(text, @" :", ":");
            text = Regex.Replace(text, @" '", "'");

            while (text.Contains("\"\""))
                text = text.Replace("\"\"", "\"");
            while (text.Contains("''"))
                text = text.Replace("''", "'");
            while (text.Contains("``"))
                text = text.Replace("``", "`");

            text = Regex.Replace(text, @"\s+", " ").Trim();
            if (text.Length == 0)
                text = ".";

            if (!Regex.IsMatch(text, @"[.!?;:,'\u0022\u201C\u201D\u2018\u2019)\]}…。」』】〉》›»]$"))
                text += ".";

            return $"<{languageCode}>{text}</{languageCode}>";
        }

        static string RemoveEmojis(string text)
        {
            var builder = new StringBuilder(text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                int codePoint;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    i++;
                }
                else
                {
                    codePoint = text[i];
                }

                if (IsEmoji(codePoint))
                    continue;

                builder.Append(codePoint > 0xFFFF ? char.ConvertFromUtf32(codePoint) : (char)codePoint);
            }

            return builder.ToString();
        }

        static bool IsEmoji(int codePoint)
        {
            return (codePoint >= 0x1F600 && codePoint <= 0x1F64F) ||
                   (codePoint >= 0x1F300 && codePoint <= 0x1F5FF) ||
                   (codePoint >= 0x1F680 && codePoint <= 0x1F6FF) ||
                   (codePoint >= 0x1F700 && codePoint <= 0x1F77F) ||
                   (codePoint >= 0x1F780 && codePoint <= 0x1F7FF) ||
                   (codePoint >= 0x1F800 && codePoint <= 0x1F8FF) ||
                   (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) ||
                   (codePoint >= 0x1FA00 && codePoint <= 0x1FA6F) ||
                   (codePoint >= 0x1FA70 && codePoint <= 0x1FAFF) ||
                   (codePoint >= 0x2600 && codePoint <= 0x26FF) ||
                   (codePoint >= 0x2700 && codePoint <= 0x27BF) ||
                   (codePoint >= 0x1F1E6 && codePoint <= 0x1F1FF);
        }

        (float[] noise, float[] mask, int latentLength, int latentChannels) SampleNoisyLatent(IReadOnlyList<float> duration)
        {
            var wavLengthMax = duration.Max() * sampleRate;
            var chunkSize = baseChunkSize * chunkCompressFactor;
            var latentLength = Mathf.Max(1, Mathf.CeilToInt(wavLengthMax / chunkSize));
            var latentChannels = latentDim * chunkCompressFactor;

            var random = new System.Random();
            var noise = new float[latentChannels * latentLength];
            for (int i = 0; i < noise.Length; i++)
                noise[i] = NextGaussian(random);

            var wavLength = Mathf.RoundToInt(duration[0] * sampleRate);
            var activeLatentLength = Mathf.Clamp((wavLength + chunkSize - 1) / chunkSize, 0, latentLength);
            var mask = new float[latentLength];
            for (int i = 0; i < activeLatentLength; i++)
                mask[i] = 1.0f;

            for (int c = 0; c < latentChannels; c++)
            {
                var channelOffset = c * latentLength;
                for (int t = 0; t < latentLength; t++)
                    noise[channelOffset + t] *= mask[t];
            }

            return (noise, mask, latentLength, latentChannels);
        }

        static float NextGaussian(System.Random random)
        {
            var u1 = 1.0 - random.NextDouble();
            var u2 = 1.0 - random.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }

        static string GetVoiceStyleAssetName(string styleName)
        {
            return string.IsNullOrWhiteSpace(styleName) ? "M1" : styleName.Trim();
        }

        static VoiceStyle ParseVoiceStyle(string json)
        {
            var root = JObject.Parse(json);
            var ttlShape = ReadDims(root["style_ttl"]?["dims"], 1);
            var dpShape = ReadDims(root["style_dp"]?["dims"], 1);
            var ttl = Flatten3D(root["style_ttl"]?["data"]);
            var dp = Flatten3D(root["style_dp"]?["data"]);

            return new VoiceStyle(
                new Tensor<float>(new TensorShape(ttlShape), ttl),
                new Tensor<float>(new TensorShape(dpShape), dp));
        }

        static IEnumerator ReadStreamingAssetText(
            Action<string> onSuccess,
            Action<Exception> onError,
            params string[] relativePath)
        {
            var path = GetStreamingAssetPath(relativePath);

            if (CanReadStreamingAssetsWithFileApi() && File.Exists(path))
            {
                try
                {
                    onSuccess?.Invoke(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                }

                yield break;
            }

            var uri = ToStreamingAssetUri(path);
            using var request = UnityWebRequest.Get(uri);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(new InvalidOperationException($"Failed to read StreamingAssets file: {uri}. {request.error}"));
                yield break;
            }

            onSuccess?.Invoke(request.downloadHandler.text);
        }

        static string GetStreamingAssetPath(params string[] relativePath)
        {
            var path = Application.streamingAssetsPath;
            foreach (var part in relativePath)
                path = Path.Combine(path, part);

            return path;
        }

        static bool CanReadStreamingAssetsWithFileApi()
        {
            return Application.platform != RuntimePlatform.Android &&
                   Application.platform != RuntimePlatform.WebGLPlayer;
        }

        static string ToStreamingAssetUri(string path)
        {
            return path.IndexOf("://", StringComparison.Ordinal) >= 0 ? path : $"file://{path}";
        }

        static int[] ReadDims(JToken token, int batchSize)
        {
            if (token == null)
                throw new FormatException("Voice style dims are missing.");

            var dims = token.Select(v => v.Value<int>()).ToArray();
            if (dims.Length == 0)
                throw new FormatException("Voice style dims are empty.");

            dims[0] = batchSize;
            return dims;
        }

        static float[] Flatten3D(JToken token)
        {
            if (token == null)
                throw new FormatException("Voice style data is missing.");

            var values = new List<float>();
            foreach (var batch in token.Children())
            foreach (var row in batch.Children())
            foreach (var value in row.Children())
                values.Add(value.Value<float>());

            return values.ToArray();
        }

        static List<string> ChunkText(string text, int maxLength)
        {
            text = string.IsNullOrWhiteSpace(text) ? "." : text.Trim();
            var chunks = new List<string>();
            var paragraphs = Regex.Split(text, @"\n\s*\n+")
                .Select(p => p.Trim())
                .Where(p => p.Length > 0);

            foreach (var paragraph in paragraphs)
            {
                var sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+");
                var current = string.Empty;

                foreach (var sentence in sentences)
                {
                    if (sentence.Length == 0)
                        continue;

                    if (current.Length + sentence.Length + 1 <= maxLength)
                    {
                        current = current.Length == 0 ? sentence : current + " " + sentence;
                    }
                    else
                    {
                        if (current.Length > 0)
                            chunks.Add(current.Trim());

                        current = sentence;
                    }
                }

                if (current.Length > 0)
                    chunks.Add(current.Trim());
            }

            if (chunks.Count == 0)
                chunks.Add(text);

            return chunks;
        }

        void SetButtonInteractable(bool interactable, string label)
        {
            if (playButton != null)
                playButton.interactable = interactable;
            if (languageDropdown != null)
                languageDropdown.interactable = interactable;
            if (voiceStyleDropdown != null)
                voiceStyleDropdown.interactable = interactable;
            if (textInput != null)
                textInput.interactable = interactable;
            if (buttonText != null)
                buttonText.text = label;
        }

        void SetGenerationTimeText(string value)
        {
            if (generationTimeText != null)
                generationTimeText.text = value;
        }

        static string FormatElapsed(TimeSpan elapsed)
        {
            return elapsed.TotalSeconds >= 10.0
                ? $"{elapsed.TotalSeconds:0.0}s"
                : $"{elapsed.TotalSeconds:0.00}s";
        }

        sealed class VoiceStyle : IDisposable
        {
            public VoiceStyle(Tensor<float> styleTtl, Tensor<float> styleDp)
            {
                StyleTtl = styleTtl;
                StyleDp = styleDp;
            }

            public Tensor<float> StyleTtl { get; }
            public Tensor<float> StyleDp { get; }

            public void Dispose()
            {
                StyleTtl.Dispose();
                StyleDp.Dispose();
            }
        }
    }
}
