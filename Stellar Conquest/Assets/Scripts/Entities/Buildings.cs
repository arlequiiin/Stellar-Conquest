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
            // TODO: ����������� �� ������� ResourceManager ��� ���������� _isCurrentlyPowered
            // ��������: ResourceManager.Instance(OwnerPlayerId).OnPowerStatusChanged += UpdatePowerStatus;
            // � ����� ��������� ������� ������
            // UpdatePowerStatus(ResourceManager.Instance(OwnerPlayerId).HasEnoughPower(...));
        }
        else {
            _isCurrentlyPowered = true;
        }
    }

    // ���������� �� ResourceManager
    public virtual void UpdatePowerStatus(bool hasPower) {
        if (!_requiresPower) return;

        bool wasPowered = _isCurrentlyPowered;
        _isCurrentlyPowered = hasPower;

        if (wasPowered != _isCurrentlyPowered) {
            Debug.Log($"{gameObject.name} ������: {(_isCurrentlyPowered ? "�������" : "���������")}");

            OnPowerChanged(_isCurrentlyPowered);
        }
    }

    protected virtual void OnPowerChanged(bool isPowered) { }

    public override void Select() {
        Debug.Log($"������ {gameObject.name} �������");
        base.Select(); 
    }
    public override void Deselect() {
        Debug.Log($"������ {gameObject.name} �� �������");
        base.Deselect();
    }

    protected override void Die() {
        Debug.Log($"������ {gameObject.name} ����������");
        // ������� ���������� ������

        // �������� ResourceManager
        if (_requiresPower) {
            // ResourceManager.Instance(OwnerPlayerId).UnregisterBuilding(this);
        }

        base.Die();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (_requiresPower) {
            // TODO: ���������� �� ������� ResourceManager
            // ��������: if(ResourceManager.Instance != null) ResourceManager.Instance(OwnerPlayerId).OnPowerStatusChanged -= UpdatePowerStatus;
        }
    }
}
