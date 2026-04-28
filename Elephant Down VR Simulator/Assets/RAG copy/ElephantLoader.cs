using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ElephantChunk
{
    public string id;
    public string title;
    public string topic;
    public string[] keywords;
    public string species;
    public string region;
    public string text;
    public float[] embedding;
}

[Serializable]
public class ElephantWrapper
{
    public ElephantChunk[] items;
}

public class ElephantLoader : MonoBehaviour
{
    public TextAsset jsonFile;
    public ElephantChunk[] chunks;
    public MiniLMEmbedder embedder;

    float CosineSimilarity(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
        {
            Debug.LogError("Embedding size mismatch or missing embedding.");
            return -1f;
        }

        float dot = 0f;
        float magA = 0f;
        float magB = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0f || magB == 0f)
			{	
    		Debug.LogError("Zero-length embedding vector.");
    		return -1f;
			}

		return dot / (Mathf.Sqrt(magA) * Mathf.Sqrt(magB));
    }

    public void TestSearchTopK(float[] queryEmbedding, int topK = 3)
    {
        var results = new List<(ElephantChunk chunk, float score)>();

        foreach (ElephantChunk chunk in chunks)
        {
            float score = CosineSimilarity(queryEmbedding, chunk.embedding);
            results.Add((chunk, score));
        }

        results.Sort((a, b) => b.score.CompareTo(a.score));

        int count = Mathf.Min(topK, results.Count);

        for (int i = 0; i < count; i++)
        {
            Debug.Log("Rank " + (i + 1));
            Debug.Log("Title: " + results[i].chunk.title);
            Debug.Log("Score: " + results[i].score);
            Debug.Log("Text: " + results[i].chunk.text);
        }
    }

    public string GetTopKContext(float[] queryEmbedding, int topK = 3)
    {
        var results = new List<(ElephantChunk chunk, float score)>();

        foreach (ElephantChunk chunk in chunks)
        {
            float score = CosineSimilarity(queryEmbedding, chunk.embedding);
            results.Add((chunk, score));
        }

        results.Sort((a, b) => b.score.CompareTo(a.score));

        int count = Mathf.Min(topK, results.Count);
        string context = "";

        for (int i = 0; i < count; i++)
        {
            context += "[Source " + (i + 1) + ": " + results[i].chunk.title + "]\n";
            context += results[i].chunk.text + "\n\n";
        }

        return context;
    }
    
    public string BuildRagPrompt(string userQuestion, string context)
    {
        string prompt =
            "You are an assistant answering questions about elephants.\n" +
            "Use only the context below. If the answer is not in the context, say you do not know.\n\n" +
            "Context:\n" +
            context +
            "\nQuestion:\n" +
            userQuestion +
            "\n\nAnswer:";

        return prompt;
    }

    void Start()
    {
        if (jsonFile == null)
        {
            Debug.LogError("No JSON file assigned. Drag elephants_embeddings.json into the Json File field.");
            return;
        }

        ElephantWrapper wrapper = JsonUtility.FromJson<ElephantWrapper>(jsonFile.text);
        chunks = wrapper.items;

        Debug.Log("Loaded chunks: " + chunks.Length);
        Debug.Log("First title: " + chunks[0].title);
        Debug.Log("Embedding length: " + chunks[0].embedding.Length);

        string userQuestion = "How do elephants communicate?";

        // Generate embedding inside Unity
        float[] queryEmbedding = embedder.GenerateEmbedding(userQuestion);

        Debug.Log("Query embedding length: " + queryEmbedding.Length);

        // Debug view
        TestSearchTopK(queryEmbedding, 3);

        // Get RAG context
        string context = GetTopKContext(queryEmbedding, 3);

        Debug.Log("=== RAG CONTEXT ===");
        Debug.Log(context);

        // Build final prompt
        string prompt = BuildRagPrompt(userQuestion, context);

        Debug.Log("=== FINAL PROMPT ===");
        Debug.Log(prompt);
    }
}