using UnityEngine;

public class ElephantPersonalityLocal : MonoBehaviour
{
    [SerializeField] private string personalityInstruction = @"
        
        ";

    public string BuildPrompt(string rawUserQuestion, string ragPrompt, string memory)
    {
        return $@"
        ### SYSTEM:
        {personalityInstruction}

        STRICT FORMAT:
        First line:
        [EMOTION: <emotion>]
        
        Second line:
        Your response

    
    ### RECENT CONVERSATION:
    {memory}

    ### RAG INPUT:
    {ragPrompt}

    ### HUMAN QUESTION:
    {rawUserQuestion}

    ### ELEPHANT:
    ";
    }
}