using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour {
    [Header("Screens")]
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;

    private void Awake() {
        GameManager.Instance.OnGameEnd += HandleGameEnd;   // подпишемс€ на событие
    }

    private void OnDestroy() {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameEnd -= HandleGameEnd;
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape) && GameManager.Instance.IsPlaying)
            TogglePause();
    }

    public void TogglePause() {
        if (pauseScreen.activeSelf)
            Resume();
        else
            Pause();
    }

    public void Pause() {
        pauseScreen.SetActive(true);
        GameManager.Instance.PauseGame();
    }

    public void Resume() {
        pauseScreen.SetActive(false);
        GameManager.Instance.ResumeGame();
    }

    private void HandleGameEnd(bool defeated) {
        if (defeated) loseScreen.SetActive(true);
        else winScreen.SetActive(true);
    }

    //  нопки на экране конца игры
    public void OnMainMenu() => SceneManager.LoadScene("MainMenu");
    public void OnRestart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
