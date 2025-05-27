using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    void Start()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.Play();
    }
}