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

    // ✅ Keyboard key (kept)
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

        // ✅ Stop speech immediately (same behavior as recorder)
        if (tts != null)
        {
            tts.Stop();
        }

        clip = Microphone.Start(null, false, 60, 44100);

        Debug.Log("Recording started...");
    }

    private void StopRecording()
    {
        recording = false;

        int position = Microphone.GetPosition(null);
        Microphone.End(null);

        // ✅ Safety check (from MicrophoneRecorder)
        if (position <= 0)
        {
            Debug.LogWarning("No microphone data recorded.");
            return;
        }

        // ✅ Trim audio (IMPORTANT)
        float[] samples = new float[position * clip.channels];
        clip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create(
            "TrimmedRecording",
            position,
            clip.channels,
            clip.frequency,
            false
        );

        trimmedClip.SetData(samples, 0);

        Debug.Log("Recording stopped. Length: " + position);

        OnRecordingFinished?.Invoke(trimmedClip);
    }

    private void Update()
    {
        // =====================================
        // ✅ VR CONTROLLER INPUT
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
        // ✅ KEYBOARD INPUT (kept)
        // =====================================
        if (Input.GetKeyDown(recordKey) && !recording)
        {
            StartRecording();
        }

        if (Input.GetKeyUp(recordKey) && recording)
        {
            StopRecording();
        }
    }

    // ✅ Public state (unchanged)
    public bool IsRecording
    {
        get { return recording; }
    }
}