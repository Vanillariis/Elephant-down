using UnityEngine;

public class VoicePipelineConnector : MonoBehaviour
{
    [SerializeField] private MicrophoneRecorder recorder;
    [SerializeField] private WhisperTranscriber whisper;
    [SerializeField] private OllamaResponder ollama;
    [SerializeField] private PiperSpeaker piper;

    private void Start()
    {
        recorder.OnRecordingFinished += whisper.Transcribe;
        whisper.OnTranscriptionReady += ollama.GenerateResponse;
        ollama.OnResponseReady += piper.Speak;

        // Generate intro through LLM...
        ollama.GenerateResponse("Introduce yourself briefly.");
        // ... Or piper.Speak("I am an elephant. You may speak.");
    }
}