using UnityEngine;
using UnityEngine.XR;
using System;

public class MicrophoneLocal : MonoBehaviour
{
    [Header("Microphone")]
    [SerializeField] private SupertonicTTSLocal tts;

    public Action<AudioClip> OnRecordingFinished;

    private AudioClip clip;
    private bool recording;

    // ✅ Keyboard key (easy to change in Inspector if needed)
    [Header("Input")]
    [SerializeField] private KeyCode recordKey = KeyCode.W;

    private void OnDestroy()
    {
        if (recording)
        {
            Microphone.End(null);
        }
    }

    public void StartRecording()
    {
        recording = true;

        clip = Microphone.Start(null, false, 60, 44100);

        // Interrupt any ongoing speech
        if (tts != null)
        {
            tts.Stop();
        }

        Debug.Log("Recording started...");
    }

    private void StopRecording()
    {
        recording = false;

        Microphone.End(null);

        Debug.Log("Recording stopped.");

        OnRecordingFinished?.Invoke(clip);
    }

    private void Update()
    {
        // =====================================
        // ✅ VR CONTROLLER INPUT (unchanged)
        // =====================================
        var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (device.isValid &&
            device.TryGetFeatureValue(CommonUsages.primaryButton, out var pressed))
        {
            if (pressed && !recording)
            {
                StartRecording();
            }

            if (!pressed && recording)
            {
                StopRecording();
            }
        }

        // =====================================
        // ✅ KEYBOARD INPUT (NEW)
        // =====================================

        // Key pressed → start recording
        if (Input.GetKeyDown(recordKey) && !recording)
        {
            StartRecording();
        }

        // Key released → stop recording
        if (Input.GetKeyUp(recordKey) && recording)
        {
            StopRecording();
        }
    }

    // =====================================
    // ✅ PUBLIC STATE
    // =====================================
    public bool IsRecording
    {
        get { return recording; }
    }
}