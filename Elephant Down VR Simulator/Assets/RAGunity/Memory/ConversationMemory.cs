using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ConversationMemory : MonoBehaviour
{
    [SerializeField] private int maxTurns = 6;

    private readonly List<string> turns = new List<string>();

    [TextArea]
    [SerializeField] private string openingSentence =
        "Ah… a Human. We do not often get time to truly listen to one another, stay here with me for a while. You see an elephant standing before you, but I also carry generations of memory. We elephants survive by remembering. Humans survive by changing. Maybe that is why we misunderstand each other sometimes?";

    private void Awake()
        {
            // Treat this as the FIRST REAL MESSAGE
            turns.Add("Elephant: " + openingSentence);
        }

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
