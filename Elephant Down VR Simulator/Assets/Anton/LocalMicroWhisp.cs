using UnityEngine;
using UnityEngine.XR;
using Whisper.Utils;

public class LocalMicroWhisp : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private MicrophoneRecord micRecord;
    [SerializeField] private SupertonicTTSLocal tts;

    [Header("Input")]
    [SerializeField] private KeyCode recordKey = KeyCode.W;

    private void Update()
    {
        if (micRecord == null) return;

        var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (device.isValid &&
            device.TryGetFeatureValue(CommonUsages.primaryButton, out var pressed))
        {
            if (pressed && !micRecord.IsRecording)
                StartRecording();

            if (!pressed && micRecord.IsRecording)
                micRecord.StopRecord();
        }

        if (Input.GetKeyDown(recordKey) && !micRecord.IsRecording)
        {
            StartRecording();
        }

        if (Input.GetKeyUp(recordKey) && micRecord.IsRecording)
        {
            micRecord.StopRecord();
        }
    }

    private void StartRecording()
    {
        micRecord.StartRecord();

        // ✅ Stop current speech
        if (tts != null)
            tts.Stop();
    }
}