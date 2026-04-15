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

        // Build paths at runtime (THIS is the key fix)
        string basePath = Path.Combine(Application.streamingAssetsPath, "piper");
        string exePath = Path.Combine(basePath, "piper.exe");
        string modelPath = Path.Combine(basePath, "en_US-joe-medium.onnx");

        await Task.Run(() =>
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = $"--model \"{modelPath}\" --output_file \"{output}\"";
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;

                p.Start();
                p.StandardInput.Write(text);
                p.StandardInput.Close();
                p.WaitForExit();
            }
        });

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