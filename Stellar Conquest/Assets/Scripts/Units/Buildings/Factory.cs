using UnityEngine;
using System.Collections.Generic; 

public class Factory : Buildings {
    [SerializeField] private List<UnitProductionInfo> _producibleUnits; 
    [SerializeField] private Transform _spawnPoint;       
    [SerializeField] private Transform _rallyPoint;        
    [SerializeField] private int _maxQueueSize = 5;      

    private Queue<UnitProductionInfo> _productionQueue = new Queue<UnitProductionInfo>();
    private UnitProductionInfo _currentProduction = null;
    private float _currentProductionTimer = 0f;

    [System.Serializable]
    public class UnitProductionInfo {
        public string UnitName = "Unit";
        public Units UnitPrefab;         
        public float ProductionTime = 5f;
        public float ResourceCost = 100f;
        public ResourceType ResourceType = ResourceType.Resource; 
        public float PowerCost = 0f;     // c�������� � ������� (���������� ��� ������?)
    }

    protected override void Start() {
        base.Start();
        if (_spawnPoint == null) _spawnPoint = transform; 
        if (_rallyPoint == null) _rallyPoint = _spawnPoint; 
        Debug.Log($"{gameObject.name} ��������. ����� ����������� ������. ������ {_powerConsumption} �������");
    }

    void Update() {
        if (IsPowered) {
            ProcessProductionQueue();
        }
        else {
            // ������� ������� �� ����� ������������, ������ �� �����?
            if (_currentProduction != null) {
                Debug.Log($"{gameObject.name} ������� ���, ����� ������������: {_currentProduction.UnitName}");
            }
        }
    }

    private void ProcessProductionQueue() {
        if (_currentProduction == null && _productionQueue.Count > 0) {
            _currentProduction = _productionQueue.Dequeue(); 
            _currentProductionTimer = 0f; 
            Debug.Log($"{gameObject.name} ����� ����������� {_currentProduction.UnitName}, �����: {_currentProduction.ProductionTime}s");
        }

        if (_currentProduction != null) {
            _currentProductionTimer += Time.deltaTime;
            // �������� ������������ (��� UI) = _currentProductionTimer / _currentProduction.ProductionTime

            if (_currentProductionTimer >= _currentProduction.ProductionTime) {
                SpawnUnit(_currentProduction);
                _currentProduction = null; 
                _currentProductionTimer = 0f;
                ProcessProductionQueue();
            }
        }
    }

    public bool QueueUnitProduction(UnitProductionInfo unitInfo) {
        if (unitInfo == null || unitInfo.UnitPrefab == null) {
            Debug.LogError("���������� ������� �����");
            return false;
        }

        if (!IsPowered) {
            Debug.LogWarning($"{gameObject.name} ���������� �������� � �������: ��� �������");
            // �������� ������ ����� UI
            return false;
        }

        if (_productionQueue.Count >= _maxQueueSize) {
            Debug.LogWarning($"{gameObject.name} ������ �������� � �������, ��� ��� ������� ���������: ({_maxQueueSize}).");
            // �������� ������ ����� UI
            return false;
        }

        // if (ResourceManager.Instance(OwnerPlayerId).CanAfford(unitInfo.ResourceType, unitInfo.ResourceCost))
        bool canAfford = true; // �������� ��� �������� ��������
        if (canAfford) {
            // ��������� �������
            // TODO: ResourceManager.Instance(OwnerPlayerId).SpendResource(unitInfo.ResourceType, unitInfo.ResourceCost);

            _productionQueue.Enqueue(unitInfo);
            Debug.Log($"{gameObject.name} queued production of {unitInfo.UnitName}. Queue size: {_productionQueue.Count}");
            // �������� UI �������
            return true;
        }
        else {
            Debug.LogWarning($"{gameObject.name} cannot queue {unitInfo.UnitName}: Not enough resources.");
            // �������� ������ ����� UI
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
        if (queueIndex == 0 && _currentProduction != null) // ������ ��������
        {
            Debug.Log($"{gameObject.name} cancelled current production of {_currentProduction.UnitName}.");
            // TODO: ������� ������� ������ ResourceManager.Instance(OwnerPlayerId).AddResource(...)
            _currentProduction = null;
            _currentProductionTimer = 0;
            // TODO: �������� UI
            ProcessProductionQueue(); // �������� ������ ��������� �� �������
        }
        else if (queueIndex > 0 && queueIndex <= _productionQueue.Count) // ������ �� �������
        {
            // ����������� ������� � ������ ��� �������� �� �������
            List<UnitProductionInfo> queueList = new List<UnitProductionInfo>(_productionQueue);
            UnitProductionInfo cancelledUnit = queueList[queueIndex - 1]; // ������ � ������ ������ �� 1
            queueList.RemoveAt(queueIndex - 1);
            _productionQueue = new Queue<UnitProductionInfo>(queueList); // ������� ����� �������

            Debug.Log($"{gameObject.name} cancelled queued production of {cancelledUnit.UnitName}. Queue size: {_productionQueue.Count}");
            // TODO: ������� ������� ������
            // TODO: �������� UI
        }
        else {
            Debug.LogWarning($"Cannot cancel production: Invalid queue index {queueIndex}.");
        }
    }

    private void SpawnUnit(UnitProductionInfo unitInfo) {
        if (unitInfo.UnitPrefab == null) return;

        // ������� ����� �� �������
        Units newUnit = Instantiate(unitInfo.UnitPrefab, _spawnPoint.position, _spawnPoint.rotation);
        newUnit.SetOwner(this.OwnerPlayerId); // ������������� ���������

        Debug.Log($"{gameObject.name} produced unit {newUnit.name} for player {OwnerPlayerId}.");

        // ���������� ����� � ����� �����
        newUnit.MoveTo(_rallyPoint.position);

        // TODO: �������� ������� OnUnitProduced, ���� �����
    }

    // ������� �� ��������� �������
    protected override void OnPowerChanged(bool isPowered) {
        if (isPowered) {
            Debug.Log($"{gameObject.name} power restored, resuming production if possible.");
            // ����� �������� ��������/������� ������
        }
        else {
            Debug.Log($"{gameObject.name} lost power, production paused.");
            // ����� ��������� ��������/������� ������
        }
    }
}