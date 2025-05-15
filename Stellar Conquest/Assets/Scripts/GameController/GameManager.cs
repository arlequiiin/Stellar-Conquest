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

    public enum GameState { Playing, Paused, GameOver }
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
        EndGame();
    }

    private void EndGame() {
        CurrentState = GameState.GameOver;
        Debug.Log("Игрок проиграл");
        // TODO: Показать экран поражения
        // Time.timeScale = 0f;
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
