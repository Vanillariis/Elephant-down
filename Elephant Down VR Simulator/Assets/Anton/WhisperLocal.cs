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
    public async void Transcribe(AudioChunk chunk)
    {
        if (chunk.Data == null || chunk.Data.Length == 0 || IsBusy)
            return;

        IsBusy = true;

        try
        {

            // ✅ SMALL delay to avoid frame spike (very impactful)
            await Task.Delay(50);

            float start = Time.realtimeSinceStartup;
            Debug.Log(">>> WHISPER START");

            var result = await whisper.GetTextAsync(
                chunk.Data,
                chunk.Frequency,
                chunk.Channels
            );

            float end = Time.realtimeSinceStartup;
            Debug.Log($"<<< WHISPER DONE: {end - start:F2}s");

            if (result == null) return;

            string text = result.Result;

            if (string.IsNullOrWhiteSpace(text))
                return;

            Debug.Log("Transcription: " + text);

            OnTranscriptionReady?.Invoke(text);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}