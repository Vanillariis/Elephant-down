using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FPSLogger : MonoBehaviour
{
    [Header("FPS Settings")]
    public float logInterval = 1f; // How often to calculate FPS
    private float timer = 0f;
    private int frameCount = 0;

    [Header("Auto-Save Settings")]
    public float autoSaveInterval = 10f; // Saves the file every 10 seconds so you don't lose data
    private float autoSaveTimer = 0f;

    private List<string> logLines = new List<string>();
    private string filePath;
    public static FPSLogger instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void Start()
    {
        // Path works for PC and VR (Quest)
        string directoryPath = Application.persistentDataPath;
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        filePath = Path.Combine(directoryPath, $"FPS_Log_{timestamp}.csv");

        logLines.Add("Time (s),FPS,Scene,Event,UserText,ElephantText,Emotion");
        logLines.Add($"0.00,Start,{SceneManager.GetActiveScene().name}");

        Debug.Log("FPS Logger Started. Saving to: " + filePath);
    }

    void Update()
    {
        // 1. Calculate FPS
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= logInterval)
        {
            float fps = frameCount / timer;
            float timeSinceStart = Time.time; // Total time game has been running
            logLines.Add($"{timeSinceStart:F2},{fps:F2},{SceneManager.GetActiveScene().name}");

            timer = 0f;
            frameCount = 0;
        }

        // 2. Auto-Save Logic (Prevents data loss on VR/Crashes)
        autoSaveTimer += Time.unscaledDeltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            SaveLog();
            autoSaveTimer = 0f;
        }
    }

    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        logLines.Add($"{Time.time:F2},SceneChanged,{newScene.name}");
    }

    void OnApplicationQuit()
    {
        SaveLog();
    }

    public void SaveLog()
    {
        try
        {
            // Writes all lines currently in the list to the CSV
            File.WriteAllLines(filePath, logLines);
            // On Quest, check Logcat to see this confirm message
            Debug.Log("FPS Log Updated: " + filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save log: " + e.Message);
        }
    }

    public void LogDialogue(string userText, string elephantText, string emotion)
    {
        float timeSinceStart = Time.time;

        // Escape commas so CSV doesn't break
        userText = userText.Replace(",", " ");
        elephantText = elephantText.Replace(",", " ");

        logLines.Add($"{timeSinceStart:F2},,,DIALOGUE,{userText},{elephantText},{emotion}");
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }
}