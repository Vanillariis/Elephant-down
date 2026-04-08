using UnityEngine;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class OllamaResponder : MonoBehaviour
{
    [Header("Ollama Settings")]
    [SerializeField] private string url = "http://localhost:11434/api/generate";
    [SerializeField] private string model = "mistral"; // Maybe change to LLama 3.1
    
    [SerializeField] private ElephantPersonality personality;

    public Action<string> OnResponseReady;
    public Action<string> OnPartialResponse;

    private static HttpClient client = new HttpClient();

    public async void GenerateResponse(string text)
    {
        string engineeredPrompt = personality.BuildPrompt(text);

        var json = JsonUtility.ToJson(new Request
        {
            model = model,
            prompt = engineeredPrompt,
            stream = true
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var reader = new System.IO.StreamReader(stream))
        {
            string line;
            StringBuilder fullResponse = new StringBuilder();
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                // Each line is a JSON object with a partial response
                var parsed = JsonUtility.FromJson<Response>(line);
                fullResponse.Append(parsed.response);
                OnPartialResponse?.Invoke(fullResponse.ToString());
            }
            // Final callback for compatibility
            OnResponseReady?.Invoke(fullResponse.ToString());
        }
    }

    [Serializable]
    class Request { public string model; public string prompt; public bool stream; }

    [Serializable]
    class Response { public string response; }
}