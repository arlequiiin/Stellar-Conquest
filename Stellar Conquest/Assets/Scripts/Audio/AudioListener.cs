using UnityEngine;

public class GlobalAudioListener : MonoBehaviour {
    private static GlobalAudioListener instance;

    void Awake() {
        if (instance != null && instance != this) {
            Destroy(gameObject); 
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
