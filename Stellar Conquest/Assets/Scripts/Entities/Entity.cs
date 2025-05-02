using UnityEngine;

public abstract class Entity : MonoBehaviour {
    [SerializeField] private float _maxHealth;
    [SerializeField] private GameObject _selectionCircle;
    private float _currentHealth;

    [SerializeField] public int OwnerPlayerId { get; protected set; }

    protected virtual void Awake() {
        _currentHealth = _maxHealth;
        OwnerPlayerId = 1;
    }

    protected virtual void Start() {
        _selectionCircle?.SetActive(false);
        Debug.Log($"{gameObject.name} выключил выделение");
    }

    public virtual void TakeDamage(float amount) {
        if (amount <= 0) return;

        _currentHealth -= amount;
        Debug.Log($"{gameObject.name} получил {amount} урона. Текущее здоровье: {_currentHealth}/{_maxHealth}");

        if (_currentHealth <= 0) Die();
    }

    public bool IsAlive => _currentHealth > 0;
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;

    public virtual void Select() {
        if (_selectionCircle != null)
            _selectionCircle.SetActive(true);
    
        Debug.Log($"{gameObject.name} выбран");
    }

    public virtual void Deselect() {
        if (_selectionCircle != null)
            _selectionCircle.SetActive(false);

        Debug.Log($"{gameObject.name} не выбран");
    }

    public virtual void SetOwner(int playerId) {
        OwnerPlayerId = playerId;
        // применить цвета игрока
    }

    protected virtual void Die() {
        Debug.Log($"{gameObject.name} был уничтожен");
        //  логика проигрывания анимации смерти, звуков, эффектов

        // Сообщаем внешним системам (например, GameManager) о смерти
        // OnEntityDestroyed?.Invoke(this); 

        Destroy(gameObject, 5f); // удалим через 5 сек
    }
    protected virtual void OnDestroy() {
    
    }
}
