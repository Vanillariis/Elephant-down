using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ElephantChunk
{
    public string id;
    public string title;
    public string topic;
    public string source;
    public int page;
    public string unit;
    public string lesson;
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

    public string CreateRagPrompt(string userQuestion)
    {
        if (embedder == null)
        {
            Debug.LogError("Embedder is not assigned!");
            return userQuestion;
        }

        if (chunks == null || chunks.Length == 0)
        {
            Debug.LogError("Chunks not loaded!");
            return userQuestion;
        }

        float[] queryEmbedding = embedder.GenerateEmbedding(userQuestion);

        string context = GetTopKContext(queryEmbedding, 5);
        string prompt = BuildRagPrompt(userQuestion, context);

        Debug.Log("=== RAG PROMPT SENT TO LLM ===");
        Debug.Log(prompt);

        return prompt;
    }

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

    public string GetTopKContext(float[] queryEmbedding, int topK = 5)
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
            ElephantChunk c = results[i].chunk;

            context += "[Source " + (i + 1) + "]\n";
            context += "Title: " + c.title + "\n";
            context += "Topic: " + c.topic + "\n";
            context += "Source: " + c.source + "\n";
            context += "Page: " + c.page + "\n";
            context += "Unit: " + c.unit + "\n";
            context += "Lesson: " + c.lesson + "\n";
            context += "Similarity score: " + results[i].score + "\n";
            context += "Text: " + c.text + "\n\n";
        }

        return context;
    }

    public void TestSearchTopK(float[] queryEmbedding, int topK = 5)
    {
        Debug.Log(GetTopKContext(queryEmbedding, topK));
    }

    public string BuildRagPrompt(string userQuestion, string context)
    {
        return
            "Use the following retrieved knowledge as factual grounding.\n" +
            "Do not copy it word for word. Use it naturally.\n" +
            "If the answer is not supported by the context, say you do not know.\n\n" +
            "Retrieved context:\n" +
            context +
            "\nUser question:\n" +
            userQuestion;
    }

    void Start()
    {
        if (jsonFile == null)
        {
            Debug.LogError("No JSON file assigned.");
            return;
        }

        ElephantWrapper wrapper = JsonUtility.FromJson<ElephantWrapper>(jsonFile.text);

        if (wrapper == null || wrapper.items == null || wrapper.items.Length == 0)
        {
            Debug.LogError("Could not load chunks from JSON.");
            return;
        }

        chunks = wrapper.items;

        Debug.Log("RAG system loaded. Chunks: " + chunks.Length);
        Debug.Log("First title: " + chunks[0].title);
        Debug.Log("Embedding length: " + chunks[0].embedding.Length);
    }
    
    
    //FOR TESTING
    /*void Start()
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
    }*/
}