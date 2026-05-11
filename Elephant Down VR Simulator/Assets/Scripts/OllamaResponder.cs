using UnityEngine;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class OllamaResponder : MonoBehaviour
{
    [Header("Ollama Settings")]
    [SerializeField] private string url = "http://localhost:11434/api/generate";
    //[SerializeField] private string model = "mistral";
    [SerializeField] private string model = "gemma4:e2b";
    
    [SerializeField] private ElephantPersonality personality;
	[SerializeField] private ElephantLoader elephantLoader;
    [SerializeField] private ConversationMemory memory;

    public Action<string> OnResponseReady;
    public Action<string> OnEmotionDetected;

    private static HttpClient client = new HttpClient();
    public bool IsBusy { get; private set; }

    public async void GenerateResponse(string text)
    {
        IsBusy = true;

        try
        {
            string ragPrompt = elephantLoader.CreateRagPrompt(text);
            Debug.Log("RAG PROMPT USED BY OLLAMA:\n" + ragPrompt);

            string memoryText = memory != null ? memory.GetMemoryText() : "No previous conversation.";
            Debug.Log("MEMORY USED BY OLLAMA:\n" + memoryText);

            string engineeredPrompt = personality.BuildPrompt(ragPrompt, memoryText);

            var json = JsonUtility.ToJson(new Request
            {
                model = model,
                prompt = engineeredPrompt,
                stream = false
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();

            var parsed = JsonUtility.FromJson<Response>(result);

            string finalText = parsed.response;

            Debug.Log("RAW LLM OUTPUT: " + finalText);

            string emotion = "neutral";
            var match = System.Text.RegularExpressions.Regex.Match(finalText, @"\[EMOTION:\s*(.*?)\]");
            if (match.Success)
            {
                emotion = match.Groups[1].Value.ToLower();
            }

            string cleanedText = finalText;

            cleanedText = System.Text.RegularExpressions.Regex
                .Replace(cleanedText, @"\[EMOTION:.*?\]", "");

            cleanedText = System.Text.RegularExpressions.Regex
                .Replace(cleanedText, @"\[\d{2}:\d{2}:\d{2}.*?\]", "");

            cleanedText = cleanedText.Trim();

            Debug.Log("CLEANED TEXT: " + cleanedText);
            Debug.Log("TEXT SENT TO PIPER: >>>" + cleanedText + "<<<");

            if (memory != null)
            {
                memory.AddTurn(text, cleanedText);
            }

            OnEmotionDetected?.Invoke(emotion);
            OnResponseReady?.Invoke(cleanedText);

            if (FPSLogger.instance != null)
            {
                FPSLogger.instance.LogDialogue(text, cleanedText, emotion);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Ollama error: " + e.Message);
        }
        finally
        {
            IsBusy = false;
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