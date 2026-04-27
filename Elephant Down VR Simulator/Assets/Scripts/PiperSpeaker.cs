using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class PiperSpeaker : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    public async void Speak(string text)
    {
        string output = Path.Combine(Application.persistentDataPath, "response.wav");

        string basePath = Path.Combine(Application.streamingAssetsPath, "piper");
        string exePath = Path.Combine(basePath, "piper.exe");
        string modelPath = Path.Combine(basePath, "models/en_US-kristin-medium.onnx");

        UnityEngine.Debug.Log("Exe: " + exePath);
        UnityEngine.Debug.Log("Model: " + modelPath);

        if (!File.Exists(exePath))
        {
            UnityEngine.Debug.LogError("piper.exe NOT FOUND");
            return;
        }

        if (!File.Exists(modelPath))
        {
            UnityEngine.Debug.LogError("Model NOT FOUND");
            return;
        }

        await Task.Run(() =>
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = $"--model \"{modelPath}\" --output_file \"{output}\"";
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;

                p.Start();
                p.StandardInput.WriteLine(text);
                p.StandardInput.Close();
                p.WaitForExit();

                string error = p.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError("Piper error: " + error);
                }
            }
        });

        if (!File.Exists(output))
        {
            UnityEngine.Debug.LogError("WAV file not created!");
            return;
        }

        StartCoroutine(Play(output));
    }

    private IEnumerator Play(string path)
    {
        using (var req = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
        {
            yield return req.SendWebRequest();
            audioSource.clip = DownloadHandlerAudioClip.GetContent(req);
            audioSource.Play();
        }
    }

    public void Stop()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}