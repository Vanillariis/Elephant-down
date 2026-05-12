using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;

public class WhisperLocal : MonoBehaviour
{
    [Header("Whisper")]
    [SerializeField] private WhisperManager whisper;

    public Action<string> OnTranscriptionReady;

    public bool IsBusy { get; private set; }

    // ✅ SAME ENTRY POINT (used by VoicePipelineConnector)
    public async void Transcribe(AudioClip clip)
    {
        if (clip == null || IsBusy)
            return;

        IsBusy = true;

        try
        {
            Debug.Log("Starting Whisper transcription...");

            // ✅ Convert AudioClip → float[]
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            int frequency = clip.frequency;
            int channels = clip.channels;

            // ✅ Run Whisper
            var result = await whisper.GetTextAsync(samples, frequency, channels);

            if (result == null)
            {
                Debug.LogWarning("Whisper returned null");
                return;
            }

            string text = result.Result;

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("Empty transcription");
                return;
            }

            Debug.Log("Whisper transcription: " + text);

            // ✅ Send to next step (LLM)
            OnTranscriptionReady?.Invoke(text);
        }
        catch (Exception e)
        {
            Debug.LogError("Whisper error: " + e.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}