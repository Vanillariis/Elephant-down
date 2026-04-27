using UnityEngine;

public class VoicePipelineConnector : MonoBehaviour
{
    [SerializeField] private MicrophoneRecorder recorder;
    [SerializeField] private WhisperTranscriber whisper;
    [SerializeField] private OllamaResponder ollama;
    [SerializeField] private PiperSpeaker piper;
    [SerializeField] private AnimatorScript animatorScript;

    private void Start()
    {
        recorder.OnRecordingFinished += whisper.Transcribe;
        whisper.OnTranscriptionReady += ollama.GenerateResponse;
        ollama.OnEmotionDetected += animatorScript.SetEmotion;
        ollama.OnResponseReady += piper.Speak;

        //ollama.GenerateResponse("Introduce yourself briefly.");
        piper.Speak("I am an elephant. You may speak.");
    }
    private void OnDestroy()
    {
        if (recorder != null && whisper != null)
            recorder.OnRecordingFinished -= whisper.Transcribe;

        if (whisper != null && ollama != null)
            whisper.OnTranscriptionReady -= ollama.GenerateResponse;

        if (ollama != null)
        {
            if (animatorScript != null)
                ollama.OnEmotionDetected -= animatorScript.SetEmotion;

            if (piper != null)
                ollama.OnResponseReady -= piper.Speak;
        }
    }
}