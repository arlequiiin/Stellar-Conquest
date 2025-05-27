using UnityEngine;
using UnityEngine.EventSystems;

public class GlobalEventSystem : MonoBehaviour {
    private static GlobalEventSystem instance;

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
