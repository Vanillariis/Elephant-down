using UnityEngine;

public class VoicePipelineConnector : MonoBehaviour
{
    [SerializeField] private MicrophoneRecorder recorder;
    [SerializeField] private WhisperTranscriber whisper;
    [SerializeField] private OllamaResponder ollama;
    [SerializeField] private PiperSpeaker piper;
    [SerializeField] private ElephantEmotionController emotionController;

    private void Start()
    {
        recorder.OnRecordingFinished += whisper.Transcribe;
        whisper.OnTranscriptionReady += ollama.GenerateResponse;
        ollama.OnEmotionDetected += emotionController.SetEmotion;
        ollama.OnResponseReady += piper.Speak;

        //ollama.GenerateResponse("Introduce yourself briefly.");
        piper.Speak("I am an elephant. You may speak.");
    }
}