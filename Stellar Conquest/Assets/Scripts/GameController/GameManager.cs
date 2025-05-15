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
        Debug.Log("������ ����");

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
        Debug.Log("����� ��������");
        // TODO: �������� ����� ���������
        // Time.timeScale = 0f;
    }

    public void PauseGame() {
        if (CurrentState == GameState.Playing) {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            Debug.Log("���� �� �����");
            // TODO: �������� ���� �����
        }
    }

    public void ResumeGame() {
        if (CurrentState == GameState.Paused) {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            Debug.Log("���� ������������");
            // TODO: ������ ���� �����
        }
    }

    public int GetPlayerId() {
        return playerId;
    }
}
