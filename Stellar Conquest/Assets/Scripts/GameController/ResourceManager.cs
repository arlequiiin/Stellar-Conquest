using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public enum ResourceType { Energy, Uranium } 

public class ResourceManager : MonoBehaviour {
    private static ResourceManager _instance;
    public static ResourceManager Instance =>  _instance;
   
    [SerializeField] private float startingUranium = 100f;
    [SerializeField] private float startingEnergy = 100f;

    private float currentUranium;
    private float currentEnergy;
    private float totalUraniumProduction;
    private float totalEnergyProduction;

    public float UraniumProductionRate => totalUraniumProduction;
    public float EnergyProductionRate => totalEnergyProduction;

    public event Action<ResourceType, float> OnResourceChanged;

    void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;


        SceneManager.sceneLoaded += OnSceneLoaded;
        ResetResources();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        Debug.Log("ResourceManager: Сцена перезагружена, сброс ресурсов...");
        ResetResources();
        UpdateAll();
        Debug.Log($"ResourceManager: Ресурсы сброшены - Уран: {currentUranium}, Энергия: {currentEnergy}");
    }

    void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_instance == this) _instance = null;
    }
    public void ForceReset() {
        ResetResources();
        UpdateAll();
    }
    public void ResetResources() {
        currentUranium = startingUranium;
        currentEnergy = startingEnergy;
        totalUraniumProduction = 0f;
        totalEnergyProduction = 0f;
    }
    public void UpdateAll() {
        OnResourceChanged?.Invoke(ResourceType.Uranium, currentUranium);
        OnResourceChanged?.Invoke(ResourceType.Energy, currentEnergy);
    }

    public float GetUranium() => currentUranium;
    public float GetEnergy() => currentEnergy;

    public bool CanAfford(float uraniumCost, float energyCost) {
        return currentUranium >= uraniumCost && currentEnergy >= energyCost;
    }

    public bool SpendResources(float uranium, float energy) {
        if (!CanAfford(uranium, energy)) {
            Debug.LogWarning($"Недостаточно ресурсов: нужно урана {uranium}, энергии {energy}");
            return false;
        }

        currentUranium -= uranium;
        currentEnergy -= energy;
        OnResourceChanged?.Invoke(ResourceType.Uranium, currentUranium);
        OnResourceChanged?.Invoke(ResourceType.Energy, currentEnergy);
        return true;
    }

    public void RefundResources(float uranium, float energy) {
        currentUranium += uranium;
        currentEnergy += energy;
        OnResourceChanged?.Invoke(ResourceType.Uranium, currentUranium);
        OnResourceChanged?.Invoke(ResourceType.Energy, currentEnergy);
    }

    public void AddUranium(float amount) {
        if (amount <= 0) return;
        currentUranium += amount;
        OnResourceChanged?.Invoke(ResourceType.Uranium, currentUranium);
    }

    public void AddEnergy(float amount) {
        if (amount <= 0) return;
        currentEnergy += amount;
        OnResourceChanged?.Invoke(ResourceType.Energy, currentEnergy);
    }

    public void OnResourceMined(ResourceType type, float amount) {
        if (amount <= 0) return;

        switch (type) {
            case ResourceType.Uranium:
                AddUranium(amount);
                break;
            case ResourceType.Energy:
                AddEnergy(amount);
                break;
        }
    }

    public void AddProduction(ResourceType type, float amount) {
        if (amount <= 0) return;
        switch (type) {
            case ResourceType.Uranium:
                totalUraniumProduction += amount;
                break;
            case ResourceType.Energy:
                totalEnergyProduction += amount;
                break;
        }
    }

    public void RemoveProduction(ResourceType type, float amount) {
        if (amount <= 0) return;

        switch (type) {
            case ResourceType.Uranium:
                totalUraniumProduction -= amount;
                break;
            case ResourceType.Energy:
                totalEnergyProduction -= amount;
                break;
        }
    }

}