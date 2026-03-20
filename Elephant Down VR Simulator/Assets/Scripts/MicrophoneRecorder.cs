using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System;
using System.Collections.Generic;

public class MicrophoneRecorder : MonoBehaviour
{
    [Header("Microphone")]
    [SerializeField] private Dropdown dropdown;
    [SerializeField] private Image progressBar;

    [Header("Recording")]
    [SerializeField] private int duration = 5;
    [SerializeField] private int sampleRate = 44100;

    public Action<AudioClip> OnRecordingFinished;

    private AudioClip clip;
    private float time;
    private bool recording;

    private void Start()
    {
        foreach (var device in Microphone.devices)
            dropdown.options.Add(new Dropdown.OptionData(device));
    }

    private void OnDestroy()
    {
        if (recording)
        {
            Microphone.End(null);
        }
    }

    private void StartRecording()
    {
        recording = true;
        time = 0;

        int index = dropdown.value;
        clip = Microphone.Start(dropdown.options[index].text, false, duration, sampleRate);
    }

    private void Update()
    {
        // Check for input to start recording
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);

        if (!recording && device.isValid &&
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out var pressed) &&
            pressed)
        {
            StartRecording();
        }

        if (!recording) return;

        time += Time.deltaTime;
        progressBar.fillAmount = time / duration;

        if (time >= duration)
        {
            recording = false;
            Microphone.End(null);

            OnRecordingFinished?.Invoke(clip);

            progressBar.fillAmount = 0;
        }
    }
}