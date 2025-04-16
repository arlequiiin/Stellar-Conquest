using UnityEngine;

public class Buildings : Entity {
    [SerializeField] protected bool _isPowered = true;
    [SerializeField] protected float _powerConsumption = 0f;
    public bool IsPowered => !_requiresPower || _isCurrentlyPowered;
    protected bool _requiresPower = false; 
    protected bool _isCurrentlyPowered = false;

    protected override void Start() {
        base.Start();

        if (TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent)) {
            agent.enabled = false;
        }

        _requiresPower = _powerConsumption > 0f;
        if (_requiresPower) {
            // TODO: Подписаться на события ResourceManager для обновления _isCurrentlyPowered
            // Например: ResourceManager.Instance(OwnerPlayerId).OnPowerStatusChanged += UpdatePowerStatus;
            // И сразу запросить текущий статус
            // UpdatePowerStatus(ResourceManager.Instance(OwnerPlayerId).HasEnoughPower(...));
        }
        else {
            _isCurrentlyPowered = true;
        }
    }

    // вызывается из ResourceManager
    public virtual void UpdatePowerStatus(bool hasPower) {
        if (!_requiresPower) return;

        bool wasPowered = _isCurrentlyPowered;
        _isCurrentlyPowered = hasPower;

        if (wasPowered != _isCurrentlyPowered) {
            Debug.Log($"{gameObject.name} статус: {(_isCurrentlyPowered ? "Запитан" : "Незапитан")}");

            OnPowerChanged(_isCurrentlyPowered);
        }
    }

    protected virtual void OnPowerChanged(bool isPowered) { }

    public override void Select() {
        Debug.Log($"Здание {gameObject.name} выбрано");
        base.Select(); 
    }
    public override void Deselect() {
        Debug.Log($"Здание {gameObject.name} не выбрано");
        base.Deselect();
    }

    protected override void Die() {
        Debug.Log($"Здание {gameObject.name} уничтожено");
        // Эффекты разрушения здания

        // сообщить ResourceManager
        if (_requiresPower) {
            // ResourceManager.Instance(OwnerPlayerId).UnregisterBuilding(this);
        }

        base.Die();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (_requiresPower) {
            // TODO: Отписаться от событий ResourceManager
            // Например: if(ResourceManager.Instance != null) ResourceManager.Instance(OwnerPlayerId).OnPowerStatusChanged -= UpdatePowerStatus;
        }
    }
}
