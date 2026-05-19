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
        // piper.Speak("You walk quietly for a human. I wonder if I should continue this route to the town.");
        piper.Speak("A Human. We do not often get time to truly listen to one another, stay here with me for a while. You see an elephant standing before you, but I also carry generations of memory. We elephants survive by remembering. Humans survive by changing. Maybe that is why we misunderstand each other sometimes?");
    }
}