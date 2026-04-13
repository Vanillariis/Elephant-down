using UnityEngine;
using UnityEngine.InputSystem;

public class ElephantEmotionTester : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private static readonly int MoodHash = Animator.StringToHash("Mood");

    private enum Mood
    {
        Neutral = 0,
        Happy = 1,
        Angry = 2,
        Sad = 3
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Keyboard.current == null || animator == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SetMood(Mood.Neutral);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SetMood(Mood.Happy);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SetMood(Mood.Angry);

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SetMood(Mood.Sad);
    }

    private void SetMood(Mood mood)
    {
        animator.SetInteger(MoodHash, (int)mood);
    }
}