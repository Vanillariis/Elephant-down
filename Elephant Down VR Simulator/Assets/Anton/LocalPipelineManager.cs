using UnityEngine;

public class LocalPipelineManager : MonoBehaviour
{
    [SerializeField] private MicrophoneLocal recorder;
    [SerializeField] private WhisperLocal whisper;
    [SerializeField] private LLMLocal ollama;
    [SerializeField] private SupertonicTTSLocal tts;
    [SerializeField] private AnimatorScript animatorScript;
    public void InitializePipeline()
    {
        Debug.Log("Initializing pipeline...");

        recorder.OnRecordingFinished += whisper.Transcribe;

        whisper.OnTranscriptionReady += async (text) =>
        {
            await ollama.GenerateResponse(text);
        };

        ollama.OnEmotionDetected += animatorScript.SetEmotion;
        ollama.OnResponseReady += tts.Speak;

        tts.Speak("Ah… a Human. We do not often get time to truly listen to one another, stay here with me for a while. You see an elephant standing before you, but I also carry generations of memory. We elephants survive by remembering. Humans survive by changing. Maybe that is why we misunderstand each other sometimes?");
    }
}