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

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        InitializeGame();
    }

    private void InitializeGame() {
        Debug.Log("Начало игры");

        CurrentState = GameState.Playing;
    }

    public void PlayerLost() {
        if (CurrentState != GameState.Playing)
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

    private void EndGame(bool isDefeated) {
        if (isDefeated) {
            Debug.Log("Игрок проиграл");
            CurrentState = GameState.GameOver;
        }
        else if (!isDefeated) {
            Debug.Log("Игрок победил");
            CurrentState = GameState.GameWin;
        }

        // TODO: Показать экран конца игры
        Time.timeScale = 0f;
    }

    public void PauseGame() {
        if (CurrentState == GameState.Playing) {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            Debug.Log("Игра на паузе");
            // TODO: Показать меню паузы
        }
    }

    public void ResumeGame() {
        if (CurrentState == GameState.Paused) {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            Debug.Log("Игра продолжается");
            // TODO: Скрыть меню паузы
        }
    }

    public int GetPlayerId() {
        return playerId;
    }
}
