using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeScreen : MonoBehaviour
{
    [SerializeField] private Image overlay;
    [SerializeField] private float duration = 2f;

    public void FadeOut()
    {
        StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float time = 0f;

        Color color = overlay.color;

        while (time < duration)
        {
            float t = time / duration;
            t = Mathf.SmoothStep(0, 1, t); // nice easing

            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            overlay.color = color;

            time += Time.deltaTime;
            yield return null;
        }

        color.a = endAlpha;
        overlay.color = color;

        // ✅ optional: disable after fade
        gameObject.SetActive(false);
    }
}