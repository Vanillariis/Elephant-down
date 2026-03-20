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

    public Action<string> OnResponseReady;

    private static HttpClient client = new HttpClient();

    public async void GenerateResponse(string text)
    {
        var json = JsonUtility.ToJson(new Request
        {
            model = model,
            prompt = text,
            stream = false
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        string body = await response.Content.ReadAsStringAsync();

        var parsed = JsonUtility.FromJson<Response>(body);
        OnResponseReady?.Invoke(parsed.response);
    }

    [Serializable]
    class Request { public string model; public string prompt; public bool stream; }

    [Serializable]
    class Response { public string response; }
}