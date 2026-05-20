using UnityEngine;
using System.Collections;
using System.Reflection;
using Supertonic.Unity;
using System;

[RequireComponent(typeof(SupertonicTtsPlayer))]
public class SupertonicTTSLocal : MonoBehaviour
{
    private SupertonicTtsPlayer tts;

    private bool isReady = false;
    public bool IsReady => isReady;
    private bool isSpeaking = false;

    private string queuedText = null;

    public Action OnSpeechFinished;

    private void Awake()
    {
        tts = GetComponent<SupertonicTtsPlayer>();
    }

    private void Start()
    {
        // ✅ Wait until TTS finishes warmup
        StartCoroutine(WaitForReady());
    }

    private IEnumerator WaitForReady()
    {
        Debug.Log("Waiting for Supertonic warmup...");

        var loadedField = typeof(SupertonicTtsPlayer)
            .GetField("loaded", BindingFlags.NonPublic | BindingFlags.Instance);

        while (true)
        {
            bool loaded = (bool)loadedField.GetValue(tts);

            if (loaded)
                break;

            yield return null;
        }

        isReady = true;

        Debug.Log("Supertonic is REALLY ready");

        if (!string.IsNullOrEmpty(queuedText))
        {
            string text = queuedText;
            queuedText = null;
            Speak(text);
        }
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        Debug.Log("TTS request: " + text);

        if (!isReady)
        {
            Debug.Log("TTS not ready yet → queueing");
            queuedText = text;
            return;
        }

        if (isSpeaking)
        {
            Stop();
        }

        StartCoroutine(SpeakRoutine(text));
    }

    public void Stop()
    {
        var audio = GetComponent<AudioSource>();

        if (audio != null && audio.isPlaying)
        {
            audio.Stop();
        }

        isSpeaking = false;
    }

    private IEnumerator SpeakRoutine(string text)
    {
        isSpeaking = true;


        float ttsStartTime = Time.realtimeSinceStartup;
        Debug.Log(">>> TTS START");


        // ✅ Set utteranceText
        var field = typeof(SupertonicTtsPlayer)
            .GetField("utteranceText", BindingFlags.NonPublic | BindingFlags.Instance);

        if (field == null)
        {
            Debug.LogError("Could not access utteranceText");
            yield break;
        }

        field.SetValue(tts, text);

        Debug.Log("Injected text: " + text);

        // ✅ Wait a frame (important)
        yield return null;

        // ✅ Wait until NOT busy AND loaded
        var busyField = typeof(SupertonicTtsPlayer)
            .GetField("busy", BindingFlags.NonPublic | BindingFlags.Instance);

        var loadedField = typeof(SupertonicTtsPlayer)
            .GetField("loaded", BindingFlags.NonPublic | BindingFlags.Instance);

        while (!(bool)loadedField.GetValue(tts) || (bool)busyField.GetValue(tts))
        {
            Debug.Log("Waiting for Supertonic ready...");
            yield return null;
        }

        Debug.Log("Starting Supertonic TTS");

        // ✅ KEY FIX: bypass PlayFromCurrentSettings
        tts.StartCoroutine("GenerateAndPlay");

        // ✅ Get correct AudioSource
        var audioField = typeof(SupertonicTtsPlayer)
            .GetField("audioSource", BindingFlags.NonPublic | BindingFlags.Instance);

        AudioSource audio = audioField.GetValue(tts) as AudioSource;

        if (audio == null)
        {
            Debug.LogError("AudioSource not found!");
            yield break;
        }

        // wait for playback
        while (!audio.isPlaying)
        {
            yield return null;
        }

        while (audio.isPlaying)
        {
            yield return null;
        }


        float ttsEndTime = Time.realtimeSinceStartup;
        Debug.Log($"<<< TTS DONE: {(ttsEndTime - ttsStartTime):F2} seconds");

        isSpeaking = false;
        OnSpeechFinished?.Invoke();
    }
}
