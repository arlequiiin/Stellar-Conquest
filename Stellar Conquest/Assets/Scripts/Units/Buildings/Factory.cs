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
        public float PowerCost = 0f;     // cтоимость в энергии (мгновенная при заказе?)
    }

    protected override void Start() {
        base.Start();
        if (_spawnPoint == null) _spawnPoint = transform; 
        if (_rallyPoint == null) _rallyPoint = _spawnPoint; 
        Debug.Log($"{gameObject.name} построен. Можно производить юнитов. Тратит {_powerConsumption} энергии");
    }

    void Update() {
        if (IsPowered) {
            ProcessProductionQueue();
        }
        else {
            // питание пропало во время производства, ставим на паузу?
            if (_currentProduction != null) {
                Debug.Log($"{gameObject.name} питания нет, пауза производства: {_currentProduction.UnitName}");
            }
        }
    }

    private void ProcessProductionQueue() {
        if (_currentProduction == null && _productionQueue.Count > 0) {
            _currentProduction = _productionQueue.Dequeue(); 
            _currentProductionTimer = 0f; 
            Debug.Log($"{gameObject.name} начал производить {_currentProduction.UnitName}, время: {_currentProduction.ProductionTime}s");
        }

        if (_currentProduction != null) {
            _currentProductionTimer += Time.deltaTime;
            // Прогресс производства (для UI) = _currentProductionTimer / _currentProduction.ProductionTime

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
            Debug.LogError("Невозможно создать юнита");
            return false;
        }

        if (!IsPowered) {
            Debug.LogWarning($"{gameObject.name} невозможно добавить в очередь: нет энергии");
            // Сообщить игроку через UI
            return false;
        }

        if (_productionQueue.Count >= _maxQueueSize) {
            Debug.LogWarning($"{gameObject.name} нельзя добавить в очередь, так как очередь заполнена: ({_maxQueueSize}).");
            // Сообщить игроку через UI
            return false;
        }

        // if (ResourceManager.Instance(OwnerPlayerId).CanAfford(unitInfo.ResourceType, unitInfo.ResourceCost))
        bool canAfford = true; // Заглушка для проверки ресурсов
        if (canAfford) {
            // Списываем ресурсы
            // TODO: ResourceManager.Instance(OwnerPlayerId).SpendResource(unitInfo.ResourceType, unitInfo.ResourceCost);

            _productionQueue.Enqueue(unitInfo);
            Debug.Log($"{gameObject.name} queued production of {unitInfo.UnitName}. Queue size: {_productionQueue.Count}");
            // Обновить UI очереди
            return true;
        }
        else {
            Debug.LogWarning($"{gameObject.name} cannot queue {unitInfo.UnitName}: Not enough resources.");
            // Сообщить игроку через UI
            return false;
        }
    }

    /// <summary>
    /// Пытается добавить юнита в очередь по его префабу (находит инфо в списке _producibleUnits)
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
    /// Отменяет производство юнита в очереди.
    /// </summary>
    /// <param name="queueIndex">Индекс в очереди (0 - текущий производимый, 1 - первый в очереди и т.д.).</param>
    public void CancelProduction(int queueIndex) {
        if (queueIndex == 0 && _currentProduction != null) // Отмена текущего
        {
            Debug.Log($"{gameObject.name} cancelled current production of {_currentProduction.UnitName}.");
            // TODO: Вернуть ресурсы игроку ResourceManager.Instance(OwnerPlayerId).AddResource(...)
            _currentProduction = null;
            _currentProductionTimer = 0;
            // TODO: Обновить UI
            ProcessProductionQueue(); // Пытаемся начать следующий из очереди
        }
        else if (queueIndex > 0 && queueIndex <= _productionQueue.Count) // Отмена из очереди
        {
            // Преобразуем очередь в список для удаления по индексу
            List<UnitProductionInfo> queueList = new List<UnitProductionInfo>(_productionQueue);
            UnitProductionInfo cancelledUnit = queueList[queueIndex - 1]; // Индекс в списке смещен на 1
            queueList.RemoveAt(queueIndex - 1);
            _productionQueue = new Queue<UnitProductionInfo>(queueList); // Создаем новую очередь

            Debug.Log($"{gameObject.name} cancelled queued production of {cancelledUnit.UnitName}. Queue size: {_productionQueue.Count}");
            // TODO: Вернуть ресурсы игроку
            // TODO: Обновить UI
        }
        else {
            Debug.LogWarning($"Cannot cancel production: Invalid queue index {queueIndex}.");
        }
    }

    private void SpawnUnit(UnitProductionInfo unitInfo) {
        if (unitInfo.UnitPrefab == null) return;

        // Создаем юнита из префаба
        Units newUnit = Instantiate(unitInfo.UnitPrefab, _spawnPoint.position, _spawnPoint.rotation);
        newUnit.SetOwner(this.OwnerPlayerId); // Устанавливаем владельца

        Debug.Log($"{gameObject.name} produced unit {newUnit.name} for player {OwnerPlayerId}.");

        // Отправляем юнита в точку сбора
        newUnit.MoveTo(_rallyPoint.position);

        // TODO: Добавить событие OnUnitProduced, если нужно
    }

    // Реакция на изменение питания
    protected override void OnPowerChanged(bool isPowered) {
        if (isPowered) {
            Debug.Log($"{gameObject.name} power restored, resuming production if possible.");
            // Можно включить анимации/эффекты работы
        }
        else {
            Debug.Log($"{gameObject.name} lost power, production paused.");
            // Можно выключить анимации/эффекты работы
        }
    }
}