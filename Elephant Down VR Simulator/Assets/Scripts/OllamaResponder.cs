using UnityEngine;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class OllamaResponder : MonoBehaviour
{
    [Header("Ollama Settings")]
    [SerializeField] private string url = "http://localhost:11434/api/generate";
    [SerializeField] private string model = "mistral";
    
    [SerializeField] private ElephantPersonality personality;

    public Action<string> OnResponseReady;
    public Action<string> OnEmotionDetected;

    private static HttpClient client = new HttpClient();

    public async void GenerateResponse(string text)
    {
        string engineeredPrompt = personality.BuildPrompt(text);

        var json = JsonUtility.ToJson(new Request
        {
            model = model,
            prompt = engineeredPrompt,
            stream = false
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();

            var parsed = JsonUtility.FromJson<Response>(result);

            string finalText = parsed.response;

            Debug.Log("RAW LLM OUTPUT: " + finalText);

            // Extract emotion
            string emotion = "neutral";
            var match = System.Text.RegularExpressions.Regex.Match(
                finalText,
                @"\[EMOTION:\s*(happy|angry|neutral)\]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            if (match.Success)
            {
                emotion = match.Groups[1].Value.ToLower();
            }

            // Clean text
            string cleanedText = finalText;

            // Remove emotion tag
            cleanedText = System.Text.RegularExpressions.Regex.Replace(
                cleanedText,
                @"\[EMOTION:\s*(happy|angry|neutral)\]",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Remove timestamps
            cleanedText = System.Text.RegularExpressions.Regex
                .Replace(cleanedText, @"\[\d{2}:\d{2}:\d{2}.*?\]", "");

            // Final trim
            cleanedText = cleanedText.Trim();

            
            OnEmotionDetected?.Invoke(emotion);
            OnResponseReady?.Invoke(cleanedText);

        }
        catch (Exception e)
        {
            Debug.LogError("Ollama error: " + e.Message);
        }
    }

    [Serializable]
    class Request
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    [Serializable]
    class Response
    {
        public string response;
    }
}