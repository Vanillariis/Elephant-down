using UnityEngine;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Samples.Whisper;

public class WhisperTranscriber : MonoBehaviour
{

    private const string whisperPath = "C:/Users/anita/Downloads/whisper-bin-x64/Release/whisper-cli.exe";
    private const string whisperModel = "C:/Users/anita/Downloads/whisper-bin-x64/Release/models/ggml-base.en.bin";

    public Action<string> OnTranscriptionReady;

    public async void Transcribe(AudioClip clip)
    {
        string path = Application.persistentDataPath + "/output.wav";

        byte[] data = SaveWav.Save("output.wav", clip);
        System.IO.File.WriteAllBytes(path, data);

        string result = await Task.Run(() =>
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = whisperPath;
                p.StartInfo.Arguments = $"-m \"{whisperModel}\" -f \"{path}\"";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;

                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output;
            }
        });

        OnTranscriptionReady?.Invoke(result);
    }
}