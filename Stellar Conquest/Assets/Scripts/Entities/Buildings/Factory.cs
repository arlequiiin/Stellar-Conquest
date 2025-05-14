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

    public event Action<List<UnitProductionInfo>, UnitProductionInfo> OnQueueChanged; // ������ �������, ������� ����
    public event Action<float> OnProductionProgressUpdated; // �������� �� 0 �� 1
    public event Action<string> OnProductionMessage; // ��� ��������� �� ������� (��� ��������, ������� �����)

    [System.Serializable]
    public class UnitProductionInfo {
        public string UnitName = "Unit";
        public Units UnitPrefab;         
        public float ProductionTime = 5f;
        public float ResourceCost = 100f;
        public ResourceType ResourceType = ResourceType.Uranium; 
        public float PowerCost = 0f;     // c�������� � ������� (���������� ��� ������?)
        public Sprite UnitIcon;
    }

    public List<UnitProductionInfo> ProducibleUnits => _producibleUnits;
    public int MaxQueueSize => _maxQueueSize;
    public Queue<UnitProductionInfo> ProductionQueue => _productionQueue; 
    public UnitProductionInfo CurrentProduction => _currentProduction;
    public float CurrentProductionTimer => _currentProductionTimer;
    public int CurrentHealthInt => Mathf.CeilToInt(CurrentHealth);

    protected override void Start() {
        base.Start();
        if (_spawnPoint == null) _spawnPoint = transform; 
        if (_rallyPoint == null) _rallyPoint = _spawnPoint; 
        Debug.Log($"{gameObject.name} ��������. ����� ����������� ������");
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
            Debug.Log($"{gameObject.name} ����� ����������� {_currentProduction.UnitName}, �����: {_currentProduction.ProductionTime}s");
            OnQueueChanged?.Invoke(_productionQueue.ToList(), _currentProduction); // ������� ���������� (�����������), ������� ���� ��������
            OnProductionProgressUpdated?.Invoke(0f);
        }

        if (_currentProduction != null) {
            _currentProductionTimer += Time.deltaTime;
            // �������� ������������ (��� UI) = _currentProductionTimer / _currentProduction.ProductionTime

            if (_currentProductionTimer >= _currentProduction.ProductionTime) {
                SpawnUnit(_currentProduction);
                _currentProduction = null; 
                _currentProductionTimer = 0f;
                Debug.Log($"{gameObject.name} �������� ������������.");
                OnQueueChanged?.Invoke(_productionQueue.ToList(), _currentProduction); // ������� ���� �������� (���� null)
                OnProductionProgressUpdated?.Invoke(0f); // �������� �������� ���
                ProcessProductionQueue();
            }
        }
    }

    public bool TryQueueUnitByIndex(int unitIndex) {
        if (unitIndex < 0 || unitIndex >= _producibleUnits.Count) {
            Debug.LogError($"�������� ������ �����: {unitIndex}");
            OnProductionMessage?.Invoke("�������� ��� �����.");
            return false;
        }
        UnitProductionInfo unitInfo = _producibleUnits[unitIndex];
        return QueueUnitProduction(unitInfo); // �������� ������������ �����
    }

    public bool QueueUnitProduction(UnitProductionInfo unitInfo) {
        if (unitInfo == null || unitInfo.UnitPrefab == null) {
            Debug.LogError("���������� ������� �����");
            OnProductionMessage?.Invoke("���������� ������� �����: ���������� ��������.");
            return false;
        }

        if (_productionQueue.Count >= _maxQueueSize) {
            Debug.LogWarning($"{gameObject.name} ������ �������� � �������, ��� ��� ������� ���������: ({_maxQueueSize}).");
            OnProductionMessage?.Invoke($"������� ������������ ��������� ({_maxQueueSize}).");
            return false;
        }

        // if (ResourceManager.Instance(OwnerPlayerId).CanAfford(unitInfo.ResourceType, unitInfo.ResourceCost))
        bool canAfford = true; // �������� ��� �������� ��������
        if (canAfford) {
            // ��������� �������
            // TODO: ResourceManager.Instance(OwnerPlayerId).SpendResource(unitInfo.ResourceType, unitInfo.ResourceCost);

            _productionQueue.Enqueue(unitInfo);
            Debug.Log($"{gameObject.name} queued production of {unitInfo.UnitName}. Queue size: {_productionQueue.Count}");
            OnQueueChanged?.Invoke(_productionQueue.ToList(), _currentProduction);
            return true;
        }
        else {
            Debug.LogWarning($"{gameObject.name} cannot queue {unitInfo.UnitName}: Not enough resources.");
            OnProductionMessage?.Invoke($"������������ �������� ��� {unitInfo.UnitName}.");
            return false;
        }
    }

    /// <summary>
    /// �������� �������� ����� � ������� �� ��� ������� (������� ���� � ������ _producibleUnits)
    /// </summary>
    public bool QueueUnitProduction(Units unitPrefab) {
        foreach (var info in _producibleUnits) {
            if (info.UnitPrefab == unitPrefab) {
                return QueueUnitProduction(info);
            }
        }
        Debug.LogError($"Prefab {unitPrefab.name} not found in producible units list of {gameObject.name}.");
        return false;
    }

    /// <summary>
    /// �������� ������������ ����� � �������.
    /// </summary>
    /// <param name="queueIndex">������ � ������� (0 - ������� ������������, 1 - ������ � ������� � �.�.).</param>
    public void CancelProduction(int queueIndex) {
        UnitProductionInfo cancelledUnit = null;
        bool success = false;

        if (queueIndex == 0 && _currentProduction != null) // ������ ��������
        {
            cancelledUnit = _currentProduction;
            Debug.Log($"{gameObject.name} cancelled current production of {_currentProduction.UnitName}.");
            // TODO: ������� ������� ������ ResourceManager.Instance(OwnerPlayerId).AddResource(...)
            _currentProduction = null;
            _currentProductionTimer = 0;
            success = true;

            ProcessProductionQueue(); // �������� ������ ��������� �� �������
        }
        else if (queueIndex > 0 && queueIndex <= _productionQueue.Count) // ������ �� �������
        {
            // ����������� ������� � ������ ��� �������� �� �������
            List<UnitProductionInfo> queueList = new List<UnitProductionInfo>(_productionQueue);
            cancelledUnit = queueList[queueIndex - 1]; // ������ � ������ ������ �� 1
            queueList.RemoveAt(queueIndex - 1);
            _productionQueue = new Queue<UnitProductionInfo>(queueList); // ������� ����� �������

            Debug.Log($"{gameObject.name} cancelled queued production of {cancelledUnit.UnitName}. Queue size: {_productionQueue.Count}");
            // TODO: ������� ������� ������
            success = true;
        }
        else {
            OnProductionMessage?.Invoke($"���������� ��������: �������� ������.");
            Debug.LogWarning($"Cannot cancel production: Invalid queue index {queueIndex}.");
        }

        if (success) {
            OnQueueChanged?.Invoke(_productionQueue.ToList(), _currentProduction); // ������� ����������
            OnProductionMessage?.Invoke($"������������ {cancelledUnit?.UnitName ?? "�����"} ��������.");
        }
    }

    private void SpawnUnit(UnitProductionInfo unitInfo) {
        if (unitInfo.UnitPrefab == null) 
            return;


        Units newUnit = Instantiate(unitInfo.UnitPrefab, _spawnPoint.position, _spawnPoint.rotation);

        newUnit.SetOwner(this.OwnerPlayerId);

        Debug.Log($"{gameObject.name} produced unit {newUnit.name} for player {OwnerPlayerId}.");

        // ���������� ����� � ����� �����
        newUnit.MoveTo(_rallyPoint.position);

        // TODO: �������� ������� OnUnitProduced, ���� �����
    }
}