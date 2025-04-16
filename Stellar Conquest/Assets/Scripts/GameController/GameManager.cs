using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Для Linq методов типа Count()

public class GameManager : MonoBehaviour {
    // --- Singleton Pattern ---
    private static GameManager _instance;
    public static GameManager Instance {
        get {
            if (_instance == null) _instance = FindObjectOfType<GameManager>();
            // Опционально: создать, если не найден
            return _instance;
        }
    }

    // --- Game State ---
    public enum GameState { Pregame, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Pregame;

    // --- Player Management ---
    public class PlayerInfo {
        public int PlayerId;
        public string PlayerName; // Например, "Игрок 1", "Компьютер"
        public Color PlayerColor;  // Цвет для юнитов/зданий
        public bool IsHuman;      // Человек или ИИ?
        public CommandCenter CommandCenter; // Ссылка на главный штаб
        public bool IsDefeated = false;
    }

    public List<PlayerInfo> Players = new List<PlayerInfo>();
    public int LocalPlayerId = 1; // ID игрока, который управляет этой копией игры

    void Awake() {
        // Ensure Singleton
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // Опционально
    }

    void Start() {
        // TODO: Инициализация игры
        // - Найти или создать игроков
        // - Инициализировать ресурсы для каждого игрока через ResourceManager
        // - Найти командные центры (или создать их при старте карты)
        // - Установить начальное состояние игры
        InitializeGame(); // Пример метода инициализации
    }

    void InitializeGame() {
        Debug.Log("Initializing Game...");
        // Пример: Добавляем двух игроков (один локальный, один ИИ)
        // В реальной игре это будет зависеть от настроек лобби или карты
        Players.Add(new PlayerInfo { PlayerId = 1, PlayerName = "Player 1", PlayerColor = Color.blue, IsHuman = true });
        Players.Add(new PlayerInfo { PlayerId = 2, PlayerName = "AI Opponent", PlayerColor = Color.red, IsHuman = false });

        // Инициализируем ресурсы для всех
        foreach (var player in Players) {
            // TODO: Задать стартовые ресурсы в настройках карты/игры
            ResourceManager.Instance.InitializePlayerResources(player.PlayerId, 250f, 15f);

            // TODO: Найти или создать командный центр для игрока
            // CommandCenter cc = FindCommandCenterForPlayer(player.PlayerId);
            // if (cc != null) {
            //    player.CommandCenter = cc;
            //    cc.SetOwner(player.PlayerId);
            // } else {
            //     Debug.LogError($"Command Center for Player {player.PlayerId} not found!");
            //     // Возможно, нужно создать его здесь
            // }
        }

        CurrentState = GameState.Playing;
        Debug.Log("Game Started! State: Playing");
    }

    // Вызывается из CommandCenter.Die()
    public void PlayerLost(int playerId) {
        if (CurrentState != GameState.Playing) return; // Игра уже закончена или не началась

        PlayerInfo defeatedPlayer = Players.Find(p => p.PlayerId == playerId);
        if (defeatedPlayer != null && !defeatedPlayer.IsDefeated) {
            defeatedPlayer.IsDefeated = true;
            Debug.LogWarning($"Player {defeatedPlayer.PlayerName} (ID: {playerId}) has been defeated!");

            // TODO: Уничтожить все оставшиеся юниты/здания игрока? (Опционально)

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
            // TODO: Показать экран победы/поражения
        }
        else {
            Debug.Log("Game Over! It's a draw?"); // Ситуация возможна, если все проиграли одновременно
                                                  // TODO: Показать экран ничьи
        }

        // Можно остановить время или заблокировать управление
        // Time.timeScale = 0f;
    }

    // --- Другие возможные методы ---
    public void PauseGame() {
        if (CurrentState == GameState.Playing) {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f; // Останавливаем время
            Debug.Log("Game Paused");
            // TODO: Показать меню паузы
        }
    }

    public void ResumeGame() {
        if (CurrentState == GameState.Paused) {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f; // Возобновляем время
            Debug.Log("Game Resumed");
            // TODO: Скрыть меню паузы
        }
    }

    public PlayerInfo GetPlayerInfo(int playerId) {
        return Players.Find(p => p.PlayerId == playerId);
    }
}