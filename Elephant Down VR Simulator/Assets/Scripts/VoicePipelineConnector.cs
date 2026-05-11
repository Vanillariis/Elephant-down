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
        piper.Speak("You walk quietly for a human. I wonder if I should continue this route to the town.");
    }
}