using System.IO;
using UnityEngine;

public class EnsureLLMFolder : MonoBehaviour
{
    void Awake()
    {
        CreateLLMFolder();
    }

    void CreateLLMFolder()
    {
        string dir = Path.Combine(Application.persistentDataPath, "LLMModels");

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            Debug.Log("Created LLMModels directory: " + dir);
        }
        else
        {
            Debug.Log("LLMModels directory already exists: " + dir);
        }
    }
}
