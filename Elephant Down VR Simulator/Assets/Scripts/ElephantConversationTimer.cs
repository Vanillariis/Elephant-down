using System.Collections;
using UnityEngine;

public class ElephantConversationTimer : MonoBehaviour
{
    [Header("Conversation Settings")]
    [SerializeField] private float conversationDuration = 300f;

    [Header("References")]
    [SerializeField] private MicrophoneRecorder recorder;
    [SerializeField] private PiperSpeaker piper;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform elephantTransform;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 1.5f;
    
    [SerializeField] private WhisperTranscriber whisper;
    [SerializeField] private OllamaResponder ollama;

    private bool endingStarted = false;
    private bool isWalkingAway = false;

    private static readonly int WalkAwayHash = Animator.StringToHash("WalkAway");

    private void Start()
    {
        StartCoroutine(CountdownToEnding());
    }

    private void Update()
    {
        if (isWalkingAway && elephantTransform != null)
        {
            elephantTransform.position += elephantTransform.forward * walkSpeed * Time.deltaTime;
        }
    }

    private IEnumerator CountdownToEnding()
    {
        yield return new WaitForSeconds(conversationDuration);

        if (endingStarted)
            yield break;

        endingStarted = true;

        // Wait if the user is currently talking
        while (recorder != null && recorder.IsRecording)
            yield return null;

        if (recorder != null)
            recorder.enabled = false;

        while (whisper != null && whisper.IsBusy)
            yield return null;

        while (ollama != null && ollama.IsBusy)
            yield return null;

        while (piper != null && piper.IsBusy)
            yield return null;

        // Now play final goodbye line
        if (piper != null)
            piper.Speak("Thank you for your conversation, I must go find my herd.");

        // Wait for final line to finish
        while (piper != null && piper.IsBusy)
            yield return null;

        // Then walk away
        if (animator != null)
            animator.SetTrigger(WalkAwayHash);

        isWalkingAway = true;
    }
}