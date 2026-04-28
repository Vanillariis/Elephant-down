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

        // Stop new input, but do NOT interrupt current speech/recording
        if (recorder != null)
            recorder.enabled = false;

        // Wait until the user has finished recording
        while (recorder != null && recorder.IsRecording)
            yield return null;

        // Wait until the elephant has finished its current spoken response
        while (piper != null && piper.IsSpeaking)
            yield return null;

        // Now play final goodbye line
        if (piper != null)
            piper.Speak("Thank you for your conversation, I must go find my herd.");

        // Wait for final line to begin
        yield return new WaitForSeconds(0.2f);

        // Wait for final line to finish
        while (piper != null && piper.IsSpeaking)
            yield return null;

        // Then walk away
        if (animator != null)
            animator.SetTrigger(WalkAwayHash);

        isWalkingAway = true;
    }
}