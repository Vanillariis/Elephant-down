using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.Net.Http;
using System.Collections.Generic;

//  Run 'ollama serve' in command prompt before starting the Unity scene
// 'ollama pull mistral' if ollama model not downloaded yet

namespace Samples.Whisper
{
    public class Whisper : MonoBehaviour
    {
        // Serialized fields
        [SerializeField] private Button recordButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private Text message;
        [SerializeField] private Dropdown dropdown;
        [SerializeField] private AudioSource audioSource;

        // File and model paths (consider moving to config if needed)
        private const string PiperPath = "C:/Users/anita/Downloads/piper_windows_amd64/piper/piper.exe";
        private const string PiperModel = "C:/Users/anita/Downloads/piper_windows_amd64/piper/models/en_US-joe-medium.onnx";
        private const string WhisperPath = "C:/Users/anita/Downloads/whisper-bin-x64/Release/whisper-cli.exe";
        private const string WhisperModelPath = "C:/Users/anita/Downloads/whisper-bin-x64/Release/models/ggml-base.en.bin";

        // Audio and recording settings
        private const string FileName = "output.wav";
        private const int Duration = 5;
        private const int SampleRate = 44100;
        private string piperOutput;

        // Ollama settings
        private const string OllamaUrl = "http://localhost:11434/api/generate";
        private const string OllamaModel = "mistral";
        private const string OllamaSystemPrompt = "You are a helpful teacher. Answer questions as if you are teaching a student. Keep responses clear and educational.";

        // State
        private AudioClip clip;
        private bool isRecording;
        private float time;

        private static readonly HttpClient httpClient = new HttpClient();

        private void Start()
        {
            piperOutput = Application.persistentDataPath + "/response.wav";

#if UNITY_WEBGL && !UNITY_EDITOR
            dropdown.options.Add(new Dropdown.OptionData("Microphone not supported on WebGL"));
#else
            foreach (var device in Microphone.devices)
            {
                dropdown.options.Add(new Dropdown.OptionData(device));
            }
            recordButton.onClick.AddListener(StartRecording);
            dropdown.onValueChanged.AddListener(ChangeMicrophone);

            var index = PlayerPrefs.GetInt("user-mic-device-index");
            dropdown.SetValueWithoutNotify(index);
#endif
        }


        private void ChangeMicrophone(int index)
        {
            PlayerPrefs.SetInt("user-mic-device-index", index);
        }

        private void StartRecording()
        {
            isRecording = true;
            recordButton.enabled = false;

            var index = PlayerPrefs.GetInt("user-mic-device-index");
#if !UNITY_WEBGL
            clip = Microphone.Start(dropdown.options[index].text, false, Duration, SampleRate);
#endif
        }

////////////////////

        private async void EndRecording()
        {
            message.text = "Transcribing...";

#if !UNITY_WEBGL
            Microphone.End(null);
#endif
            // Wait a frame so Unity finalizes the clip
            await System.Threading.Tasks.Task.Delay(100);

            UnityEngine.Debug.Log($"Clip samples: {clip.samples}");
            string audioPath = Application.persistentDataPath + "/" + FileName;
            byte[] data = SaveWav.Save(FileName, clip);
            System.IO.File.WriteAllBytes(audioPath, data);
            UnityEngine.Debug.Log($"Audio file: {audioPath}");

            string result = await System.Threading.Tasks.Task.Run(() =>
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = WhisperPath;
                    process.StartInfo.Arguments = $"-m \"{WhisperModelPath}\" -f \"{audioPath}\"";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    process.Close();
                    return output;
                }
            });

            progressBar.fillAmount = 0;
            message.text = result;

            // Query Ollama with the transcribed text
            string ollamaResponse = await QueryOllama(result);
            message.text = $"You said:{result}\n---\nTeacher says:{ollamaResponse}";

            // Generate speech
            await RunPiper(ollamaResponse);

            // Play audio
            StartCoroutine(PlayGeneratedAudio());
            recordButton.enabled = true;
        }

/////////////////////////////

        private async System.Threading.Tasks.Task<string> QueryOllama(string transcribedText)
        {
            try
            {
                message.text = "Processing with AI model...";

                string json = JsonUtility.ToJson(new OllamaRequest
                {
                    model = OllamaModel,
                    prompt = transcribedText,
                    stream = false
                });

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                httpClient.Timeout = System.TimeSpan.FromSeconds(120);

                HttpResponseMessage response = await httpClient.PostAsync(OllamaUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var ollamaResponse = JsonUtility.FromJson<OllamaResponse>(responseBody);
                    return ollamaResponse.response;
                }
                else
                {
                    return $"Error querying Ollama: {response.StatusCode}";
                }
            }
            catch (System.Exception e)
            {
                return "Error: " + e.Message;
            }
        }

    [System.Serializable]
    public class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    [System.Serializable]
    public class OllamaResponse
    {
        public string response;
        public string model;
        public int prompt_eval_count;
        public int eval_count;
    }

/////////////////////////////

        private void Update()
        {
            if (isRecording)
            {
                time += Time.deltaTime;
                progressBar.fillAmount = time / Duration;
                if (time >= Duration)
                {
                    time = 0;
                    isRecording = false;
                    EndRecording();
                }
            }
        }


        // Piper text-to-speech generation
        private async System.Threading.Tasks.Task RunPiper(string text)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = PiperPath;
                    process.StartInfo.Arguments = $"--model \"{PiperModel}\" --output_file \"{piperOutput}\"";
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    process.StandardInput.Write(text);
                    process.StandardInput.Close();
                    process.WaitForExit();
                }
            });
        }

        // Coroutine to play the generated audio file from Piper
        private System.Collections.IEnumerator PlayGeneratedAudio()
        {
            string audioPath = piperOutput;
            while (!System.IO.File.Exists(audioPath))
            {
                yield return null;
            }
            using (UnityEngine.Networking.UnityWebRequest request =
                UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + audioPath, AudioType.WAV))
            {
                yield return request.SendWebRequest();
                if (audioSource.clip != null)
                {
                    Destroy(audioSource.clip);
                }
                AudioClip loadedClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = loadedClip;
                audioSource.Play();
            }
        }

        private void OnDestroy()
        {
            if (audioSource != null && audioSource.clip != null)
            {
                Destroy(audioSource.clip);
            }
        }
    }
}
