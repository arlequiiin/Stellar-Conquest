using UnityEngine;

public class GameManager : MonoBehaviour {
    private static GameManager _instance;
    public static GameManager Instance {
        get {
            if (_instance == null) _instance = FindFirstObjectByType<GameManager>();
            return _instance;
        }
    }
    public int playerId = 1;
    public CommandCenter playerCommandCenter;
    public bool isDefeated = false;

    public enum GameState { Playing, Paused, GameOver, GameWin }
    public GameState CurrentState { get; private set; } = GameState.Playing;
    public event System.Action<bool> OnGameEnd;   

    public bool IsPlaying => CurrentState == GameState.Playing;

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start() {
        InitializeGame();
    }

    private void InitializeGame() {
        Debug.Log("Начало игры");
        Time.timeScale = 1f;

        CurrentState = GameState.Playing;
    }

    public void PlayerLost() {
        if (CurrentState == GameState.Paused)
            Time.timeScale = 1f;
        else if (CurrentState != GameState.Playing)
            return;

        isDefeated = true;
        EndGame(isDefeated);
    }

    public void PlayerWin() {
        if (CurrentState != GameState.Playing)
            return;
        isDefeated = false;
        EndGame(isDefeated);
    }

    private void EndGame(bool defeated) {
        CurrentState = defeated ? GameState.GameOver : GameState.GameWin;

        OnGameEnd?.Invoke(defeated);
        StartCoroutine(StopTime(2f));
    }

    private System.Collections.IEnumerator StopTime(float delay) {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 0f;
    }

    public void PauseGame() {
        if (CurrentState == GameState.Playing) {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            Debug.Log("Игра на паузе");
        }
    }

    public void ResumeGame() {
        if (CurrentState == GameState.Paused) {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            Debug.Log("Игра продолжается");
        }
    }

    public int GetPlayerId() {
        return playerId;
    }
}
