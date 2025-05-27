using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour {
    [SerializeField] private GameObject settingsPanel;

    public void OnStartClicked() => SceneManager.LoadScene("LevelSelect");
    public void OnSettingsClicked() => settingsPanel.SetActive(!settingsPanel.activeSelf);
    public void OnExitClicked() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
