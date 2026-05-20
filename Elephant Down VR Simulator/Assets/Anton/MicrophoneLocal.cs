using UnityEngine;
using UnityEngine.XR;
using System;
using Whisper.Utils;

public class MicrophoneLocal : MonoBehaviour
{
    [SerializeField] private MicrophoneRecord microphoneRecord;
    [SerializeField] private SupertonicTTSLocal tts;

    [Header("Pipeline refs (IMPORTANT)")]
    [SerializeField] private WhisperLocal whisper;  // 👈 add this

    public Action<AudioChunk> OnRecordingFinished;

    private bool recording;

    [Header("Input")]
    [SerializeField] private KeyCode recordKey = KeyCode.W;

    [SerializeField] private LLMLocal llm;

    private bool inputEnabled = false;

    private void Awake()
    {
        // ✅ hook into Whisper system
        microphoneRecord.OnRecordStop += HandleRecordStop;
    }


    private void HandleRecordStop(AudioChunk chunk)
    {
        StartCoroutine(DelayedInvoke(chunk));
    }

    private System.Collections.IEnumerator DelayedInvoke(AudioChunk chunk)
    {
        // ✅ Let XR finish a few frames (BIG improvement)
        yield return new WaitForSeconds(0.05f);

        // ✅ Prevent Whisper overlap (very important)
        if (whisper != null && whisper.IsBusy)
        {
            yield return new WaitUntil(() => !whisper.IsBusy);
        }

        // ✅ Now safe to continue
        OnRecordingFinished?.Invoke(chunk);
    }

    public void StartRecording()
    {
        if (recording) return;

        if (llm != null)
            llm.Cancel();

        if (tts != null)
            tts.Stop();

        microphoneRecord.StartRecord(); // ✅ use their system
        recording = true;

        Debug.Log("Recording started...");
    }

    public void StopRecording()
    {
        if (!recording) return;

        microphoneRecord.StopRecord(); // ✅ buffered nicely
        recording = false;

        Debug.Log("Recording stopped...");
    }

    private void Update()
    {
        // ✅ Block input until experience starts
        if (!inputEnabled)
            return;

        // ✅ VR input
        var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (device.isValid &&
            device.TryGetFeatureValue(CommonUsages.primaryButton, out var pressed))
        {
            if (pressed && !recording)
                StartRecording();

            if (!pressed && recording)
                StopRecording();
        }

        // ✅ Keyboard input
        if (Input.GetKeyDown(recordKey) && !recording)
            StartRecording();

        if (Input.GetKeyUp(recordKey) && recording)
            StopRecording();
    }

    public bool IsRecording => recording;


    public void EnableInput()
    {
        inputEnabled = true;
        Debug.Log("🎤 Microphone input ENABLED");
    }

}