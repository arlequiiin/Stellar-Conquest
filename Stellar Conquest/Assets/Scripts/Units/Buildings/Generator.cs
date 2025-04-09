using UnityEngine;
using UnityEngine.UIElements;

public class Generator : Buildings {
    [SerializeField] private float _powerGenerated = 50f; 

    public float PowerGenerated => _powerGenerated;

    protected override void Start() {
        base.Start();
        _requiresPower = false; 
        _isCurrentlyPowered = true; 
        // сообщаем ResourceManager 
        // TODO: ResourceManager.Instance(OwnerPlayerId).AddPowerSource(this);
        Debug.Log($"{gameObject.name} построен и генерирует {_powerGenerated} энергии для игрока: {OwnerPlayerId}.");
    }

    protected override void Die() {
        Debug.Log($"{gameObject.name} перестал давать энергию");
        // Сообщаем ResourceManager 
        // TODO: ResourceManager.Instance(OwnerPlayerId).RemovePowerSource(this);
        base.Die();
    }

    public override void UpdatePowerStatus(bool hasPower) { }
    protected override void OnPowerChanged(bool isPowered) { }
}