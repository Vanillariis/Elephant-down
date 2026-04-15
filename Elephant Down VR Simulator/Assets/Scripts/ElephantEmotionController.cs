using UnityEngine;

public class ElephantEmotionController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void SetEmotion(string emotion)
    {
        // Reset all states first
        animator.SetBool("Happy", false);
        animator.SetBool("Sad", false);
        animator.SetBool("Angry", false);
        animator.SetBool("Neutral", false);

        switch (emotion)
        {
            case "happy":
                animator.SetBool("Happy", true);
                break;

            case "sad":
                animator.SetBool("Sad", true);
                break;

            case "angry":
                animator.SetBool("Angry", true);
                break;

            default:
                animator.SetBool("Neutral", true);
                break;
        }
    }
}