using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class Factory : Buildings {
    [SerializeField] private List<UnitProductionInfo> _producibleUnits; 
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform _rallyPoint; 
    [SerializeField] private int _maxQueueSize = 5;
    
    private Queue<UnitProductionInfo> _productionQueue = new Queue<UnitProductionInfo>();
    private UnitProductionInfo _currentProduction = null;
    private float _currentProductionTimer = 0f;

    public event Action<List<UnitProductionInfo>, UnitProductionInfo> OnQueueChanged;
    public event Action<float> OnProductionProgressUpdated;
    public event Action<string> OnProductionMessage;

    [System.Serializable]
    public class UnitProductionInfo {
        public string UnitName = "Unit";
        public Units UnitPrefab;         
        public float ProductionTime = 5f;
        public float UranuimCost = 100f;
        public float EnergyCost = 0f;
        public Sprite UnitIcon;
    }

    public List<UnitProductionInfo> ProducibleUnits => _producibleUnits;
    public int MaxQueueSize => _maxQueueSize;
    public Queue<UnitProductionInfo> ProductionQueue => _productionQueue; 
    public UnitProductionInfo CurrentProduction => _currentProduction;
    public float CurrentProductionTimer => _currentProductionTimer;
    public float CurrentHealthInt => GetCurrentHealth;

    protected override void Start() {
        base.Start();
        if (_spawnPoint == null) _spawnPoint = transform; 
        if (_rallyPoint == null) _rallyPoint = _spawnPoint; 
    }

    void Update() {
        ProcessProductionQueue();
        if (_currentProduction != null) {
            OnProductionProgressUpdated?.Invoke(_currentProductionTimer / _currentProduction.ProductionTime);
        }
    }

    private void ProcessProductionQueue() {
        if (_currentProduction == null && _productionQueue.Count > 0) {
            _currentProduction = _productionQueue.Dequeue(); 
            _currentProductionTimer = 0f; 
            OnQueueChanged?.Invoke(_productionQueue.ToList(), _currentProduction);
            OnProductionProgressUpdated?.Invoke(0f);
        }

        if (_currentProduction != null) {
            _currentProductionTimer += Time.deltaTime;
            // Прогресс производства? = _currentProductionTimer / _currentProduction.ProductionTime

            if (_currentProductionTimer >= _currentProduction.ProductionTime) {
                SpawnUnit(_currentProduction);
                _currentProduction = null; 
                _currentProductionTimer = 0f;
                OnQueueChanged?.Invoke(_productionQueue.ToList(), _currentProduction);
                OnProductionProgressUpdated?.Invoke(0f);
                ProcessProductionQueue();
            }
        }
    }

    public bool TryQueueUnitByIndex(int unitIndex) {
        if (unitIndex < 0 || unitIndex >= _producibleUnits.Count) {
            Debug.LogWarning($"Неверный индекс юнита: {unitIndex}");
            OnProductionMessage?.Invoke("Неверный тип юнита");
            return false;
        }
        UnitProductionInfo unitInfo = _producibleUnits[unitIndex];
        return QueueUnitProduction(unitInfo);
    }

    public bool QueueUnitProduction(UnitProductionInfo unitInfo) {
        if (unitInfo == null || unitInfo.UnitPrefab == null) {
            Debug.Log("Невозможно создать юнита");
            OnProductionMessage?.Invoke("Невозможно создать юнита: информация неполная");
            return false;
        }

        if (_productionQueue.Count >= _maxQueueSize) {
            Debug.Log($"{gameObject.name} нельзя добавить в очередь, так как очередь заполнена: ({_maxQueueSize})");
            OnProductionMessage?.Invoke($"Очередь производства заполнена ({_maxQueueSize})");
            return false;
        }

        if (ResourceManager.Instance.CanAfford(unitInfo.UranuimCost, unitInfo.EnergyCost)) { 
            ResourceManager.Instance.SpendResources(unitInfo.UranuimCost, unitInfo.UranuimCost);

            _productionQueue.Enqueue(unitInfo);
            Debug.Log($"{gameObject.name} добавил в производство {unitInfo.UnitName}. Размер очереди: {_productionQueue.Count}");
            OnQueueChanged?.Invoke(_productionQueue.ToList(), _currentProduction);
            return true;
        }
        else {
            Debug.LogWarning($"{gameObject.name} не смог добавить в очередь {unitInfo.UnitName}: недостаточно ресурсов");
            OnProductionMessage?.Invoke($"Недостаточно ресурсов");
            return false;
        }
    }
    public void CancelProduction() {
        UnitProductionInfo cancelledUnit = null;
        bool success = false;

        if (_currentProduction != null) 
        {
            cancelledUnit = _currentProduction;
            Debug.Log($"{gameObject.name} отменил текущее производство {_currentProduction.UnitName}.");
            ResourceManager.Instance.RefundResources(_currentProduction.UranuimCost, _currentProduction.EnergyCost);
            _currentProduction = null;
            _currentProductionTimer = 0;
            success = true;

            ProcessProductionQueue(); 
        }
        else {
            OnProductionMessage?.Invoke($"Невозможно отменить: неверный индекс");
            Debug.LogWarning($"Недостаточно ресурсов");
        }

        if (success) {
            OnQueueChanged?.Invoke(_productionQueue.ToList(), _currentProduction);
            OnProductionMessage?.Invoke($"Производство {cancelledUnit.UnitName} отменено");
        }
    }

    private void SpawnUnit(UnitProductionInfo unitInfo) {
        if (unitInfo.UnitPrefab == null) 
            return;

        Units newUnit = Instantiate(unitInfo.UnitPrefab, _spawnPoint.position, _spawnPoint.rotation);
        newUnit.SetOwner(this.OwnerPlayerId);
        Debug.Log($"{gameObject.name} произведён {newUnit.name}");
        newUnit.MoveTo(_rallyPoint.position);

        // TODO: Добавить событие OnUnitProduced, если нужно
    }
}