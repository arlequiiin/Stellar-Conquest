using UnityEngine;
using System.Collections.Generic;
using System; 

public enum ResourceType { Energy, Nanites } 

public class ResourceManager : MonoBehaviour {
    // --- Singleton Pattern ---
    private static ResourceManager _instance;
    public static ResourceManager Instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<ResourceManager>();
                if (_instance == null) {
                    GameObject singletonObject = new GameObject("ResourceManager");
                    _instance = singletonObject.AddComponent<ResourceManager>();
                }
            }
            return _instance;
        }
    }

    // --- Player Resources Data ---
    private class PlayerResources {
        public float CurrentNanites = 0f;
        // Energy: Отслеживаем генерацию и потребление раздельно
        public float EnergyGeneration = 0f;
        public float EnergyConsumption = 0f;
        public float NetEnergy => EnergyGeneration - EnergyConsumption; // Чистый прирост/убыток энергии
        public bool HasPower => NetEnergy >= 0f; // Есть ли достаточно энергии?
    }

    private Dictionary<int, PlayerResources> _playerResources = new Dictionary<int, PlayerResources>();

    // --- Events ---
    // Событие вызывается при изменении количества Нанитов или статуса Энергии
    // Параметры: playerId, тип измененного ресурса, новое значение (для Нанитов) или NetEnergy (для Энергии)
    public event Action<int, ResourceType, float> OnResourceChanged;
    // Событие вызывается при изменении статуса питания (HasPower)
    public event Action<int, bool> OnPowerStatusChanged;


    void Awake() {
        // Ensure Singleton
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // Опционально: сделать менеджер постоянным между сценами
    }

    // Инициализация ресурсов для нового игрока
    public void InitializePlayerResources(int playerId, float startingNanites = 100f, float startingEnergyGeneration = 10f) // Пример стартовых значений
    {
        if (!_playerResources.ContainsKey(playerId)) {
            _playerResources[playerId] = new PlayerResources {
                CurrentNanites = startingNanites,
                EnergyGeneration = startingEnergyGeneration, // Например, от Командного Центра
                EnergyConsumption = 0f
            };
            Debug.Log($"Initialized resources for Player {playerId}. Nanites: {startingNanites}, Base Power Gen: {startingEnergyGeneration}");
            // Уведомляем UI и другие системы о начальных значениях
            OnResourceChanged?.Invoke(playerId, ResourceType.Nanites, _playerResources[playerId].CurrentNanites);
            OnResourceChanged?.Invoke(playerId, ResourceType.Energy, _playerResources[playerId].NetEnergy);
            OnPowerStatusChanged?.Invoke(playerId, _playerResources[playerId].HasPower);
        }
        else {
            Debug.LogWarning($"Player {playerId} resources already initialized.");
        }
    }

    // --- Nanite Management ---

    public float GetNanites(int playerId) {
        return _playerResources.TryGetValue(playerId, out PlayerResources res) ? res.CurrentNanites : 0f;
    }

    public bool CanAfford(int playerId, float naniteCost) {
        return _playerResources.TryGetValue(playerId, out PlayerResources res) && res.CurrentNanites >= naniteCost;
    }

    public bool SpendNanites(int playerId, float amount) {
        if (amount <= 0) return true; // Трата нуля или отрицательного количества всегда "успешна"

        if (_playerResources.TryGetValue(playerId, out PlayerResources res) && res.CurrentNanites >= amount) {
            res.CurrentNanites -= amount;
            Debug.Log($"Player {playerId} spent {amount} Nanites. Remaining: {res.CurrentNanites}");
            OnResourceChanged?.Invoke(playerId, ResourceType.Nanites, res.CurrentNanites);
            return true;
        }
        Debug.LogWarning($"Player {playerId} cannot spend {amount} Nanites. Not enough funds.");
        return false;
    }

    public void AddNanites(int playerId, float amount) {
        if (amount <= 0) return;

        if (_playerResources.TryGetValue(playerId, out PlayerResources res)) {
            res.CurrentNanites += amount;
            Debug.Log($"Player {playerId} gained {amount} Nanites. Total: {res.CurrentNanites}");
            OnResourceChanged?.Invoke(playerId, ResourceType.Nanites, res.CurrentNanites);
        }
        else {
            Debug.LogWarning($"Cannot add Nanites: Player {playerId} not found.");
        }
    }

    // --- Energy Management ---

    public float GetNetEnergy(int playerId) {
        return _playerResources.TryGetValue(playerId, out PlayerResources res) ? res.NetEnergy : 0f;
    }

    public float GetEnergyGeneration(int playerId) {
        return _playerResources.TryGetValue(playerId, out PlayerResources res) ? res.EnergyGeneration : 0f;
    }

    public float GetEnergyConsumption(int playerId) {
        return _playerResources.TryGetValue(playerId, out PlayerResources res) ? res.EnergyConsumption : 0f;
    }

    public bool HasPower(int playerId) {
        return _playerResources.TryGetValue(playerId, out PlayerResources res) && res.HasPower;
    }

    // Вызывается генераторами при постройке/уничтожении
    public void AddEnergyGeneration(int playerId, float amount) {
        if (amount <= 0) return;
        if (_playerResources.TryGetValue(playerId, out PlayerResources res)) {
            bool oldPowerStatus = res.HasPower;
            res.EnergyGeneration += amount;
            Debug.Log($"Player {playerId} added {amount} Energy Generation. Total Gen: {res.EnergyGeneration}, Net: {res.NetEnergy}");
            CheckPowerStatusChange(playerId, res, oldPowerStatus);
        }
    }

    public void RemoveEnergyGeneration(int playerId, float amount) {
        if (amount <= 0) return;
        if (_playerResources.TryGetValue(playerId, out PlayerResources res)) {
            bool oldPowerStatus = res.HasPower;
            res.EnergyGeneration -= amount;
            if (res.EnergyGeneration < 0) res.EnergyGeneration = 0; // Не может быть < 0
            Debug.Log($"Player {playerId} removed {amount} Energy Generation. Total Gen: {res.EnergyGeneration}, Net: {res.NetEnergy}");
            CheckPowerStatusChange(playerId, res, oldPowerStatus);
        }
    }

    // Вызывается зданиями-потребителями при постройке/уничтожении (или вкл/выкл)
    public void AddEnergyConsumption(int playerId, float amount) {
        if (amount <= 0) return;
        if (_playerResources.TryGetValue(playerId, out PlayerResources res)) {
            bool oldPowerStatus = res.HasPower;
            res.EnergyConsumption += amount;
            Debug.Log($"Player {playerId} added {amount} Energy Consumption. Total Con: {res.EnergyConsumption}, Net: {res.NetEnergy}");
            CheckPowerStatusChange(playerId, res, oldPowerStatus);
        }
    }

    public void RemoveEnergyConsumption(int playerId, float amount) {
        if (amount <= 0) return;
        if (_playerResources.TryGetValue(playerId, out PlayerResources res)) {
            bool oldPowerStatus = res.HasPower;
            res.EnergyConsumption -= amount;
            if (res.EnergyConsumption < 0) res.EnergyConsumption = 0; // Не может быть < 0
            Debug.Log($"Player {playerId} removed {amount} Energy Consumption. Total Con: {res.EnergyConsumption}, Net: {res.NetEnergy}");
            CheckPowerStatusChange(playerId, res, oldPowerStatus);
        }
    }

    // Вспомогательный метод для проверки изменения статуса питания и вызова событий
    private void CheckPowerStatusChange(int playerId, PlayerResources res, bool oldPowerStatus) {
        bool newPowerStatus = res.HasPower;
        // Вызываем событие изменения NetEnergy в любом случае
        OnResourceChanged?.Invoke(playerId, ResourceType.Energy, res.NetEnergy);

        // Если статус питания изменился, вызываем соответствующее событие
        if (oldPowerStatus != newPowerStatus) {
            Debug.Log($"Player {playerId} power status changed to: {(newPowerStatus ? "ON" : "OFF")}");
            OnPowerStatusChanged?.Invoke(playerId, newPowerStatus);
        }
    }
}