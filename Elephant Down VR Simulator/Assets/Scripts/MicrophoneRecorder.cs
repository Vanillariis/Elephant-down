using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System;

public class MicrophoneRecorder : MonoBehaviour
{
    [Header("Microphone")]
    [SerializeField] private PiperSpeaker piper;

    public Action<AudioClip> OnRecordingFinished;

    private AudioClip clip;
    private bool recording;

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

        clip = Microphone.Start(null, false, 60, 44100); // max 5 minutes recording
    }

    private void StopRecording()
    {
        recording = false;

        int position = Microphone.GetPosition(null);

        Microphone.End(null);

        if (position <= 0)
        {
            Debug.LogWarning("No microphone data recorded.");
            return;
        }

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

        OnRecordingFinished?.Invoke(trimmedClip);
    }

    private void Update()
    {
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);

        if (device.isValid &&
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out var pressed))
        {
            if (pressed && !recording)
            {
                piper.Stop(); // interrupt any ongoing speech
                StartRecording();
            }

            if (!pressed && recording)
            {
                StopRecording();
            }
        }
    }
    //---------------------------
    public bool IsRecording
    {
        get { return recording; }
    }
}