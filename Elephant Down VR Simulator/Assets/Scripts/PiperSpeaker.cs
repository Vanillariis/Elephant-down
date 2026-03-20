using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Collections;

public class PiperSpeaker : MonoBehaviour
{
    //[SerializeField] private string piperPath;
    //[SerializeField] private string modelPath;

    private const string piperPath = "C:/Users/anita/Downloads/piper_windows_amd64/piper/piper.exe";
    private const string PiperModel = "C:/Users/anita/Downloads/piper_windows_amd64/piper/models/en_US-joe-medium.onnx";

    [SerializeField] private AudioSource audioSource;

    public async void Speak(string text)
    {
        string output = Application.persistentDataPath + "/response.wav";

        await Task.Run(() =>
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = piperPath;
                p.StartInfo.Arguments = $"--model \"{PiperModel}\" --output_file \"{output}\"";
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
}