using UnityEngine;
using System;
using System.Text.RegularExpressions;
using LLMUnity;
using System.Threading.Tasks;

public class LLMLocal : MonoBehaviour
{
    [Header("LLMUnity")]
    [SerializeField] private LLMAgent llmAgent;

    [Header("Custom Logic")]
    [SerializeField] private ElephantPersonalityLocal personality;
    [SerializeField] private ElephantLoader elephantLoader;
    [SerializeField] private ConversationMemory memory;

    public Action<string> OnResponseReady;      // cleaned (for TTS)
    public Action<string> OnRawResponseReady;   // raw full output (optional debug)
    public Action<string> OnEmotionDetected;

    public bool IsReady { get; private set; } = false;
    public bool IsBusy { get; private set; }

    public void Warmup(Action onDone)
    {
        Debug.Log("Warming up LLM...");

        _ = llmAgent.Warmup(() =>
        {
            Debug.Log("LLM Ready");
            IsReady = true;
            onDone?.Invoke();
        });
    }


    // ✅ MAIN ENTRY
    public async Task GenerateResponse(string userText)
    {
        if (IsBusy)
        {
            Debug.Log("Cancelling current LLM request...");
            Cancel();
        }

        if (string.IsNullOrWhiteSpace(userText))
            return;

        IsBusy = true;

        Debug.Log("USER SAID: " + userText);

        try
        {
            // =============================
            // Build prompt (RAG + Memory)
            // =============================
            string ragPrompt = elephantLoader != null
                ? elephantLoader.CreateRagPrompt(userText)
                : "";

            Debug.Log("RAG PROMPT USED:\n" + ragPrompt);

            string memoryText = memory != null
                ? memory.GetMemoryText()
                : "No previous conversation.";

            Debug.Log("MEMORY USED:\n" + memoryText);

            string engineeredPrompt = userText;

            if (personality != null)
            {
                engineeredPrompt = personality.BuildPrompt(userText, ragPrompt, memoryText);
            }
            Debug.Log("Prompt length: " + engineeredPrompt.Length);
            Debug.Log("FINAL PROMPT:\n" + engineeredPrompt);
            Debug.Log(">>> CALLING LLM");
            // ✅ Wait for full response from LLMAgent
            string finalText = await llmAgent.Chat(engineeredPrompt);
            Debug.Log("<<< LLM RETURNED");
            if (string.IsNullOrWhiteSpace(finalText))
            {
                Debug.LogWarning("Empty LLM response");
                return;
            }

            Debug.Log("RAW LLM OUTPUT:\n" + finalText);

            // =============================
            // Extract emotion
            // =============================
            string emotion = "neutral";

            // 1. [EMOTION: happy]
            var match = Regex.Match(finalText, @"\[EMOTION:\s*(.*?)\]", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                emotion = match.Groups[1].Value.ToLower();
            }
            else
            {
                // 2. [Happy]
                match = Regex.Match(finalText, @"^\s*\[(.*?)\]", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    emotion = match.Groups[1].Value.ToLower();
                }
                else
                {
                    // 3. fallback keywords (VERY important)
                    if (finalText.ToLower().Contains("happy")) emotion = "happy";
                    else if (finalText.ToLower().Contains("sad")) emotion = "sad";
                    else if (finalText.ToLower().Contains("angry")) emotion = "angry";
                }
            }

            Debug.Log("EXTRACTED EMOTION: " + emotion);

            // =============================
            // Clean output (for TTS)
            // =============================
            string cleanedText = finalText;

            // Remove [EMOTION: xxx]
            cleanedText = Regex.Replace(cleanedText, @"\[EMOTION:\s*.*?\]", "", RegexOptions.IgnoreCase);

            // ✅ Remove simple emotion tags like [Neutral], [Happy], etc.
            cleanedText = Regex.Replace(cleanedText, @"^\s*\[[^\]]+\]\s*", "");

            // Remove timestamps if any
            cleanedText = Regex.Replace(cleanedText, @"\[\d{2}:\d{2}:\d{2}.*?\]", "");

            // Trim final result
            cleanedText = cleanedText.Trim();

            Debug.Log("CLEANED TEXT:\n" + cleanedText);
            Debug.Log("TEXT SENT TO PIPER: >>>" + cleanedText + "<<<");

            // =============================
            // Save memory
            // =============================
            if (memory != null)
            {
                memory.AddTurn(userText, cleanedText);
            }

            // =============================
            // Fire events
            // =============================
            OnEmotionDetected?.Invoke(emotion);
            OnRawResponseReady?.Invoke(finalText);
            OnResponseReady?.Invoke(cleanedText);

            // =============================
            // FPS Logger (added from OllamaResponder)
            // =============================
            if (FPSLogger.instance != null)
            {
                FPSLogger.instance.LogDialogue(userText, cleanedText, emotion);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("LLM error: " + e.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ✅ Optional cancel (already good)
    public void Cancel()
    {
        if (!IsBusy) return;

        Debug.Log("LLM Cancel Requested");

        llmAgent.CancelRequests();
        IsBusy = false;
    }

}