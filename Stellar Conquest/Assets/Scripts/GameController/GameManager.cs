using UnityEngine;
using System.Collections.Generic;
using System.Linq; // ��� Linq ������� ���� Count()

public class GameManager : MonoBehaviour {
    // --- Singleton Pattern ---
    private static GameManager _instance;
    public static GameManager Instance {
        get {
            if (_instance == null) _instance = FindObjectOfType<GameManager>();
            // �����������: �������, ���� �� ������
            return _instance;
        }
    }

    // --- Game State ---
    public enum GameState { Pregame, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Pregame;

    // --- Player Management ---
    public class PlayerInfo {
        public int PlayerId;
        public string PlayerName; // ��������, "����� 1", "���������"
        public Color PlayerColor;  // ���� ��� ������/������
        public bool IsHuman;      // ������� ��� ��?
        public CommandCenter CommandCenter; // ������ �� ������� ����
        public bool IsDefeated = false;
    }

    public List<PlayerInfo> Players = new List<PlayerInfo>();
    public int LocalPlayerId = 1; // ID ������, ������� ��������� ���� ������ ����

    void Awake() {
        // Ensure Singleton
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // �����������
    }

    void Start() {
        // TODO: ������������� ����
        // - ����� ��� ������� �������
        // - ���������������� ������� ��� ������� ������ ����� ResourceManager
        // - ����� ��������� ������ (��� ������� �� ��� ������ �����)
        // - ���������� ��������� ��������� ����
        InitializeGame(); // ������ ������ �������������
    }

    void InitializeGame() {
        Debug.Log("Initializing Game...");
        // ������: ��������� ���� ������� (���� ���������, ���� ��)
        // � �������� ���� ��� ����� �������� �� �������� ����� ��� �����
        Players.Add(new PlayerInfo { PlayerId = 1, PlayerName = "Player 1", PlayerColor = Color.blue, IsHuman = true });
        Players.Add(new PlayerInfo { PlayerId = 2, PlayerName = "AI Opponent", PlayerColor = Color.red, IsHuman = false });

        // �������������� ������� ��� ����
        foreach (var player in Players) {
            // TODO: ������ ��������� ������� � ���������� �����/����
            ResourceManager.Instance.InitializePlayerResources(player.PlayerId, 250f, 15f);

            // TODO: ����� ��� ������� ��������� ����� ��� ������
            // CommandCenter cc = FindCommandCenterForPlayer(player.PlayerId);
            // if (cc != null) {
            //    player.CommandCenter = cc;
            //    cc.SetOwner(player.PlayerId);
            // } else {
            //     Debug.LogError($"Command Center for Player {player.PlayerId} not found!");
            //     // ��������, ����� ������� ��� �����
            // }
        }

        CurrentState = GameState.Playing;
        Debug.Log("Game Started! State: Playing");
    }

    // ���������� �� CommandCenter.Die()
    public void PlayerLost(int playerId) {
        if (CurrentState != GameState.Playing) return; // ���� ��� ��������� ��� �� ��������

        PlayerInfo defeatedPlayer = Players.Find(p => p.PlayerId == playerId);
        if (defeatedPlayer != null && !defeatedPlayer.IsDefeated) {
            defeatedPlayer.IsDefeated = true;
            Debug.LogWarning($"Player {defeatedPlayer.PlayerName} (ID: {playerId}) has been defeated!");

            // TODO: ���������� ��� ���������� �����/������ ������? (�����������)

            CheckEndGameCondition();
        }
    }

    private void CheckEndGameCondition() {
        int activePlayers = Players.Count(p => !p.IsDefeated);

        if (activePlayers <= 1) {
            EndGame();
        }
    }

    private void EndGame() {
        CurrentState = GameState.GameOver;
        PlayerInfo winner = Players.FirstOrDefault(p => !p.IsDefeated);

        if (winner != null) {
            Debug.Log($"Game Over! Winner: Player {winner.PlayerName} (ID: {winner.PlayerId})");
            // TODO: �������� ����� ������/���������
        }
        else {
            Debug.Log("Game Over! It's a draw?"); // �������� ��������, ���� ��� ��������� ������������
                                                  // TODO: �������� ����� �����
        }

        // ����� ���������� ����� ��� ������������� ����������
        // Time.timeScale = 0f;
    }

    // --- ������ ��������� ������ ---
    public void PauseGame() {
        if (CurrentState == GameState.Playing) {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f; // ������������� �����
            Debug.Log("Game Paused");
            // TODO: �������� ���� �����
        }
    }

    public void ResumeGame() {
        if (CurrentState == GameState.Paused) {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f; // ������������ �����
            Debug.Log("Game Resumed");
            // TODO: ������ ���� �����
        }
    }

    public PlayerInfo GetPlayerInfo(int playerId) {
        return Players.Find(p => p.PlayerId == playerId);
    }
}