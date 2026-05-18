using UnityEngine;
using System;
using System.Threading.Tasks;
using LLMUnity;

public class LLMLocal_Simple : MonoBehaviour
{
    [SerializeField] private LLMAgent llmAgent;

    public Action<string> OnResponseReady;
    public bool IsBusy { get; private set; }

    public async Task GenerateResponse(string userText)
    {
        if (IsBusy)
        {
            Debug.LogWarning("LLM is busy, ignoring input");
            return;
        }

        if (string.IsNullOrWhiteSpace(userText))
            return;

        IsBusy = true;

        Debug.Log("USER SAID: " + userText);

        try
        {
            // ✅ VERY SMALL PROMPT
            string prompt = userText;


            float llmStartTime = Time.realtimeSinceStartup;
            Debug.Log("PROMPT LENGTH: " + prompt.Length);
            Debug.Log(">>> CALLING LLM");

            string response = await llmAgent.Chat(prompt);

            float llmEndTime = Time.realtimeSinceStartup;
            Debug.Log("<<< LLM RETURNED");
            Debug.Log($"LLM TIME: {(llmEndTime - llmStartTime):F2} seconds");

            if (string.IsNullOrWhiteSpace(response))
            {
                Debug.LogWarning("Empty LLM response");
                return;
            }

            Debug.Log("LLM RESPONSE:\n" + response);

            OnResponseReady?.Invoke(response);
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

    public void Cancel()
    {
        llmAgent.CancelRequests();
        IsBusy = false;
    }
}