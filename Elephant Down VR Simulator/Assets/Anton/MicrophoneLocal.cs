using UnityEngine;
using UnityEngine.XR;
using System;
using Whisper.Utils;

public class MicrophoneLocal : MonoBehaviour
{
    [SerializeField] private MicrophoneRecord microphoneRecord;
    [SerializeField] private SupertonicTTSLocal tts;

    public Action<AudioChunk> OnRecordingFinished;

    private bool recording;

    [Header("Input")]
    [SerializeField] private KeyCode recordKey = KeyCode.W;

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
        yield return null; // let frame finish ✅

        OnRecordingFinished?.Invoke(chunk);
    }


    public void StartRecording()
    {
        if (recording) return;

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
}