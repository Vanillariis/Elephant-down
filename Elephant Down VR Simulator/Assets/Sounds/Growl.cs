using UnityEngine;

public class Growl : MonoBehaviour
{
    public AudioSource growlSource;

    public void PlayGrowl()
    {
        Debug.Log("HELLO");
        growlSource.Play();
    }
}