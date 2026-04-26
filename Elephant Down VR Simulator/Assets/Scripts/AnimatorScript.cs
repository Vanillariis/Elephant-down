using UnityEngine;
using UnityEngine.InputSystem;

public class AnimatorScript : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private bool enableKeyboardDebug = true;

    private static readonly int NeutralHash = Animator.StringToHash("Neutral");
    private static readonly int HappyHash = Animator.StringToHash("Happy");
    private static readonly int AngryHash = Animator.StringToHash("Angry");
    private static readonly int SadHash = Animator.StringToHash("Sad");

    public enum Mood
    {
        Neutral,
        Happy,
        Angry,
        Sad
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInParent<Animator>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        SetMood(Mood.Neutral);
    }

    private void Update()
    {
        if (!enableKeyboardDebug || Keyboard.current == null || animator == null)
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

    public void SetEmotion(string emotion)
    {

        

        if (string.IsNullOrWhiteSpace(emotion))
        {
            SetMood(Mood.Neutral);
            return;
        }
        if (string.IsNullOrWhiteSpace(emotion))
        {
            SetMood(Mood.Neutral);
            return;
        }

        switch (emotion.Trim().ToLower())
        {
            case "happy":
                SetMood(Mood.Happy);
                break;

            case "sad":
                SetMood(Mood.Sad);
                break;

            case "angry":
                SetMood(Mood.Angry);
                break;

            case "neutral":
            default:
                SetMood(Mood.Neutral);
                break;
        }
    }

    public void SetMood(Mood mood)
    {
       
        animator.SetBool(NeutralHash, false);
        animator.SetBool(HappyHash, false);
        animator.SetBool(AngryHash, false);
        animator.SetBool(SadHash, false);

        switch (mood)
        {
            case Mood.Neutral:
                animator.SetBool(NeutralHash, true);
                break;
            case Mood.Happy:
                animator.SetBool(HappyHash, true);
                break;
            case Mood.Angry:
                animator.SetBool(AngryHash, true);
                break;
            case Mood.Sad:
                animator.SetBool(SadHash, true);
                break;
        }

        Debug.Log($"Mood set to {mood}");
    }
}