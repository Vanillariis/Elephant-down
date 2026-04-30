using UnityEngine;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Samples.Whisper;
using Debug = UnityEngine.Debug;

public class WhisperTranscriber : MonoBehaviour
{

    // private const string whisperPath = "E:/olive/Release/whisper-cli.exe";
    // private const string whisperModel = "E:/olive/whisper.cpp/models/ggml-base.en.bin";

    private const string whisperPath = "C:/Users/anita/Downloads/whisper-bin-x64/Release/whisper-cli.exe";
    private const string whisperModel = "C:/Users/anita/Downloads/whisper-bin-x64/Release/models/ggml-base.en.bin";

    public Action<string> OnTranscriptionReady;
    public bool IsBusy { get; private set; }
    public async void Transcribe(AudioClip clip)
    {
        IsBusy = true;

        try
        {
            if (clip == null)
                return;

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
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;

                    p.Start();

                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    return output;
                }
            });

            if (string.IsNullOrWhiteSpace(result))
                return;

            OnTranscriptionReady?.Invoke(result);
        }
        catch (Exception e)
        {
            Debug.LogError("Whisper error: " + e.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}