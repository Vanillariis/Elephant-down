using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class MiniLMEmbedder : MonoBehaviour
{
    public Unity.InferenceEngine.ModelAsset modelAsset;
    public TextAsset vocabFile;

    private Unity.InferenceEngine.Model runtimeModel;
    private Unity.InferenceEngine.Worker worker;

    private Dictionary<string, int> vocab;

    private const int MaxTokens = 128;

    void Start()
    {
        LoadVocab();

        runtimeModel = Unity.InferenceEngine.ModelLoader.Load(modelAsset);
        worker = new Unity.InferenceEngine.Worker(runtimeModel, Unity.InferenceEngine.BackendType.CPU);

        Debug.Log("MiniLM model loaded.");
        Debug.Log("Vocab size: " + vocab.Count);
    }

    void LoadVocab()
    {
        vocab = new Dictionary<string, int>();

        string[] lines = vocabFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            string token = lines[i].Trim();
            if (!vocab.ContainsKey(token))
                vocab.Add(token, i);
        }
    }

    public float[] GenerateEmbedding(string text)
    {
        if (worker == null)
        {
            Debug.LogError("MiniLM worker is not initialized yet.");
            return new float[384];
        }

        int[] inputIds = new int[MaxTokens];
        int[] attentionMask = new int[MaxTokens];
        int[] tokenTypeIds = new int[MaxTokens];

        Encode(text, inputIds, attentionMask, tokenTypeIds);

        Unity.InferenceEngine.TensorShape shape = new Unity.InferenceEngine.TensorShape(1, MaxTokens);

        using Unity.InferenceEngine.Tensor<int> inputIdsTensor = new Unity.InferenceEngine.Tensor<int>(shape, inputIds);
        using Unity.InferenceEngine.Tensor<int> attentionMaskTensor = new Unity.InferenceEngine.Tensor<int>(shape, attentionMask);
        using Unity.InferenceEngine.Tensor<int> tokenTypeIdsTensor = new Unity.InferenceEngine.Tensor<int>(shape, tokenTypeIds);

        worker.SetInput("input_ids", inputIdsTensor);
        worker.SetInput("attention_mask", attentionMaskTensor);
        worker.SetInput("token_type_ids", tokenTypeIdsTensor);

        worker.Schedule();

        Unity.InferenceEngine.Tensor<float> outputTensor = worker.PeekOutput() as Unity.InferenceEngine.Tensor<float>;

        if (outputTensor == null)
        {
            Debug.LogError("Model output was null or not Tensor<float>.");
            return new float[384];
        }

        float[] output = outputTensor.DownloadToArray();

        Debug.Log("Raw model output length: " + output.Length);

        float[] embedding = ExtractEmbedding(output);

        NormalizeInPlace(embedding);

        return embedding;
    }

    void Encode(string text, int[] inputIds, int[] attentionMask, int[] tokenTypeIds)
    {
        int clsId = vocab["[CLS]"];
        int sepId = vocab["[SEP]"];
        int padId = vocab["[PAD]"];
        int unkId = vocab["[UNK]"];

        List<int> ids = new List<int>();
        ids.Add(clsId);

        string[] words = text.ToLowerInvariant()
            .Replace("?", " ?")
            .Replace(".", " .")
            .Replace(",", " ,")
            .Replace("!", " !")
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
        {
            List<int> pieces = WordPieceTokenize(word, unkId);

            foreach (int piece in pieces)
            {
                if (ids.Count >= MaxTokens - 1)
                    break;

                ids.Add(piece);
            }

            if (ids.Count >= MaxTokens - 1)
                break;
        }

        ids.Add(sepId);

        for (int i = 0; i < MaxTokens; i++)
        {
            if (i < ids.Count)
            {
                inputIds[i] = ids[i];
                attentionMask[i] = 1;
                tokenTypeIds[i] = 0;
            }
            else
            {
                inputIds[i] = padId;
                attentionMask[i] = 0;
                tokenTypeIds[i] = 0;
            }
        }
    }

    List<int> WordPieceTokenize(string word, int unkId)
    {
        List<int> pieces = new List<int>();

        if (vocab.ContainsKey(word))
        {
            pieces.Add(vocab[word]);
            return pieces;
        }

        int start = 0;
        bool failed = false;

        while (start < word.Length)
        {
            int end = word.Length;
            string currentSubstr = null;

            while (start < end)
            {
                string substr = word.Substring(start, end - start);
                if (start > 0)
                    substr = "##" + substr;

                if (vocab.ContainsKey(substr))
                {
                    currentSubstr = substr;
                    break;
                }

                end--;
            }

            if (currentSubstr == null)
            {
                failed = true;
                break;
            }

            pieces.Add(vocab[currentSubstr]);
            start = end;
        }

        if (failed)
        {
            pieces.Clear();
            pieces.Add(unkId);
        }

        return pieces;
    }

    float[] ExtractEmbedding(float[] output)
    {
        if (output.Length == 384)
            return output;

        if (output.Length > 384)
        {
            float[] embedding = new float[384];

            // Try using first token / pooled-style output.
            Array.Copy(output, 0, embedding, 0, 384);

            return embedding;
        }

        Debug.LogError("Unexpected output length: " + output.Length);
        return new float[384];
    }

    void NormalizeInPlace(float[] vector)
    {
        float sum = 0f;

        for (int i = 0; i < vector.Length; i++)
            sum += vector[i] * vector[i];

        float magnitude = Mathf.Sqrt(sum);

        if (magnitude == 0f)
        {
            Debug.LogError("Generated embedding has zero magnitude.");
            return;
        }

        for (int i = 0; i < vector.Length; i++)
            vector[i] /= magnitude;
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }
}