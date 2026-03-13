using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;


namespace Samples.Whisper
{
    public class Whisper : MonoBehaviour
    {
        [SerializeField] private Button recordButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private Text message;
        [SerializeField] private Dropdown dropdown;
        
        private readonly string fileName = "output.wav";
        private readonly int duration = 5;
        
        private AudioClip clip;
        private bool isRecording;
        private float time;

        private void Start()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            dropdown.options.Add(new Dropdown.OptionData("Microphone not supported on WebGL"));
            #else
            foreach (var device in Microphone.devices)
            {
                dropdown.options.Add(new Dropdown.OptionData(device));
            }
            recordButton.onClick.AddListener(StartRecording);
            dropdown.onValueChanged.AddListener(ChangeMicrophone);
            
            var index = PlayerPrefs.GetInt("user-mic-device-index");
            dropdown.SetValueWithoutNotify(index);
            #endif
        }

        private void ChangeMicrophone(int index)
        {
            PlayerPrefs.SetInt("user-mic-device-index", index);
        }
        
        private void StartRecording()
        {
            isRecording = true;
            recordButton.enabled = false;

            var index = PlayerPrefs.GetInt("user-mic-device-index");
            
            #if !UNITY_WEBGL
            clip = Microphone.Start(dropdown.options[index].text, false, duration, 44100);
            #endif
        }

////////////////////

        private async void EndRecording()
    {
        message.text = "Transcribing...";

    #if !UNITY_WEBGL
    Microphone.End(null);
    #endif

    // wait a frame so Unity finalizes the clip
    await System.Threading.Tasks.Task.Delay(100);

///////////

    UnityEngine.Debug.Log("Clip samples: " + clip.samples);

    string audioPath = Application.persistentDataPath + "/" + fileName;

    byte[] data = SaveWav.Save(fileName, clip);
    System.IO.File.WriteAllBytes(audioPath, data);

    UnityEngine.Debug.Log("Audio file: " + audioPath);

///////////
        //string whisperPath = "C:/Users/anita/Downloads/whisper-bin-x64/Release/whisper-stream.exe";
        string whisperPath = "C:/Users/anita/Downloads/whisper-bin-x64/Release/whisper-cli.exe";
        string modelPath = "C:/Users/anita/Downloads/whisper-bin-x64/Release/models/ggml-base.en.bin";

        string result = await System.Threading.Tasks.Task.Run(() =>
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = whisperPath;
                process.StartInfo.Arguments = "-m \"" + modelPath + "\" -f \"" + audioPath + "\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                process.Close();   // VERY IMPORTANT
                return output;
            }
        });

        progressBar.fillAmount = 0;
        message.text = result;
        recordButton.enabled = true;
    }

/////////////////////////////

        private void Update()
        {
            if (isRecording)
            {
                time += Time.deltaTime;
                progressBar.fillAmount = time / duration;
                
                if (time >= duration)
                {
                    time = 0;
                    isRecording = false;
                    EndRecording();
                }
            }
        }
    }
}
