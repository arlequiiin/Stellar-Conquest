using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class Factory : Buildings {
    [SerializeField] private List<EntityData> _producibleUnits;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform _rallyPoint; 
    [SerializeField] private int _maxQueueSize = 5;

    private Queue<EntityData> _productionQueue = new Queue<EntityData>();
    private EntityData _currentProduction = null;
    private float _currentProductionTimer = 0f;

    public event Action<List<EntityData>, EntityData> OnQueueChanged;
    public event Action<float> OnProductionProgressUpdated;
    public event Action<string> OnProductionMessage;

    public List<EntityData> ProducibleUnits => _producibleUnits;
    public int MaxQueueSize => _maxQueueSize;
    public Queue<EntityData> ProductionQueue => _productionQueue; 
    public EntityData CurrentProduction => _currentProduction;
    public float CurrentProductionTimer => _currentProductionTimer;
    public float CurrentHealthInt => GetCurrentHealth;

    protected override void Start() {
        base.Start();
        if (_spawnPoint == null) _spawnPoint = transform; 
        if (_rallyPoint == null) _rallyPoint = _spawnPoint; 
    }

    void Update() {
        if (IsCompleted) // ������������ �������� ������ ��� ����������� ������
        {
            ProcessProductionQueue();
            if (_currentProduction != null)
                OnProductionProgressUpdated?.Invoke(_currentProductionTimer / (_currentProduction.buildTime > 0 ? _currentProduction.buildTime : 1f));
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

            if (_currentProductionTimer >= _currentProduction.buildTime) {
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
            Debug.LogWarning($"�������� ������ �����: {unitIndex}");
            OnProductionMessage?.Invoke("�������� ��� �����");
            return false;
        }
        EntityData unitInfo = _producibleUnits[unitIndex];
        return QueueUnitProduction(unitInfo);
    }

    public bool QueueUnitProduction(EntityData unitInfo) {
        if (unitInfo == null || unitInfo.prefab == null) {
            Debug.Log("���������� ������� �����");
            OnProductionMessage?.Invoke("���������� ������� �����: ���������� ��������");
            return false;
        }

        if (_productionQueue.Count >= _maxQueueSize) {
            Debug.Log($"{gameObject.name} ������ �������� � �������, ��� ��� ������� ���������: ({_maxQueueSize})");
            OnProductionMessage?.Invoke($"������� ������������ ��������� ({_maxQueueSize})");
            return false;
        }

        if (ResourceManager.Instance.CanAfford(unitInfo.uraniumCost, unitInfo.energyCost)) {
            ResourceManager.Instance.SpendResources(unitInfo.uraniumCost, unitInfo.energyCost);
            _productionQueue.Enqueue(unitInfo);
            OnQueueChanged?.Invoke(_productionQueue.ToList(), _currentProduction);
            return true;
        }
        else {
            OnProductionMessage?.Invoke($"������������ ��������");
            return false;
        }
    }
    public void CancelCurrentProduction() {
        if (_currentProduction != null) {
            ResourceManager.Instance.RefundResources(_currentProduction.uraniumCost, _currentProduction.energyCost);
            OnProductionMessage?.Invoke($"������������ {_currentProduction.entityName} ��������");
            _currentProduction = null;
            _currentProductionTimer = 0;
            OnQueueChanged?.Invoke(_productionQueue.ToList(), _currentProduction);
            ProcessProductionQueue();
        }
        else {
            OnProductionMessage?.Invoke($"��� �������� ������������ ��� ������");
        }
    }

    private void SpawnUnit(EntityData unitInfo) {
        if (unitInfo.prefab == null) return;

        GameObject go = Instantiate(unitInfo.prefab, _spawnPoint.position, _spawnPoint.rotation);
        Units newUnit = go.GetComponent<Units>();

        newUnit.SetOwner(this.OwnerPlayerId);
        newUnit.MoveTo(_rallyPoint.position);
    }
}