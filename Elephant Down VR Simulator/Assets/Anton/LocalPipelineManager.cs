using UnityEngine;

public class LocalPipelineManager : MonoBehaviour
{
    [SerializeField] private MicrophoneLocal recorder;
    [SerializeField] private WhisperLocal whisper;
    [SerializeField] private LLMLocal ollama;
    [SerializeField] private SupertonicTTSLocal tts;
    [SerializeField] private AnimatorScript animatorScript;

    private void Start()
    {
        recorder.OnRecordingFinished += whisper.Transcribe;
        whisper.OnTranscriptionReady += ollama.GenerateResponse;
        ollama.OnEmotionDetected += animatorScript.SetEmotion;
        ollama.OnResponseReady += tts.Speak;

        //ollama.GenerateResponse("Introduce yourself briefly.");
        tts.Speak("You walk quietly for a human. I wonder if I should continue this route to the town.");
    }
}