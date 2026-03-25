using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System;

public class MicrophoneRecorder : MonoBehaviour
{
    [Header("Microphone")]
    //[SerializeField] private Dropdown dropdown;
    [SerializeField] private Image progressBar;
    [SerializeField] private PiperSpeaker piper;

    public Action<AudioClip> OnRecordingFinished;

    private AudioClip clip;
    private bool recording;

    private void Start()
    {
        //foreach (var device in Microphone.devices)
        //    dropdown.options.Add(new Dropdown.OptionData(device));
    }

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

        Microphone.End(null);

        OnRecordingFinished?.Invoke(clip);

        progressBar.fillAmount = 0;
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

        if (recording)
        {
            // Optional: simple visual feedback while holding
            progressBar.fillAmount = Mathf.PingPong(Time.time, 1f);
        }
    }
}