using GLTFast.Schema;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PipelineInitializer : MonoBehaviour
{
    [Header("Pipeline")]
    [SerializeField] private LocalPipelineManager pipeline;
    [SerializeField] private LLMLocal llm;
    [SerializeField] private SupertonicTTSLocal tts;
    [SerializeField] private MicrophoneLocal microphone;
    [SerializeField] private FadeScreen fadeScreen;

    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text buttonText;
    public GameObject uiRoot;

    private void Start()
    {
        startButton.interactable = false;
        buttonText.text = "Loading...";

        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        // ✅ Step 1 — Wait for TTS
        buttonText.text = "Loading TTS...";
        yield return new WaitUntil(() => tts.IsReady);
        Debug.Log("✅ TTS Ready");

        // ✅ Step 2 — Warmup LLM
        buttonText.text = "Warming up AI...";
        bool llmDone = false;

        llm.Warmup(() =>
        {
            llmDone = true;
        });

        yield return new WaitUntil(() => llmDone);
        Debug.Log("✅ LLM Ready");

        // ✅ DONE
        buttonText.text = "READY";
        startButton.interactable = true;

        Debug.Log("🔥 ALL SYSTEMS READY");
    }

    // ✅ Called by button
    public void StartExperience()
    {
        tts.OnSpeechFinished += microphone.EnableInput;

        fadeScreen.FadeOut();

        pipeline.InitializePipeline();

        uiRoot.SetActive(false);
    }
}