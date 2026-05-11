using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ConversationMemory : MonoBehaviour
{
    [SerializeField] private int maxTurns = 6;

    private readonly List<string> turns = new List<string>();

    public void AddTurn(string userText, string elephantText)
    {
        turns.Add("Human: " + userText);
        turns.Add("Elephant: " + elephantText);

        while (turns.Count > maxTurns * 2)
        {
            turns.RemoveAt(0);
        }
    }

    public string GetMemoryText()
    {
        if (turns.Count == 0)
            return "No previous conversation.";

        StringBuilder sb = new StringBuilder();

        foreach (string turn in turns)
        {
            sb.AppendLine(turn);
        }

        return sb.ToString();
    }

    public void ClearMemory()
    {
        turns.Clear();
    }
}
