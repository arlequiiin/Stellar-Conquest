using UnityEngine;

public abstract class Entity : MonoBehaviour {
    [SerializeField] public float _maxHealth;
    [SerializeField] private GameObject _selectionCircle;
    private float _currentHealth;

    public int OwnerPlayerId { get; protected set; }

    protected virtual void Awake() {
        _currentHealth = _maxHealth;
    }

    protected virtual void Start() {
        _selectionCircle?.SetActive(false);
    }

    public virtual void TakeDamage(float amount) {
        if (amount <= 0) return;

        _currentHealth -= amount;
        Debug.Log($"{gameObject.name} получил {amount} урона. “екущее здоровье: {_currentHealth}/{_maxHealth}");

        if (_currentHealth <= 0) Die();
    }

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
        //  логика проигрывани€ анимации смерти, звуков, эффектов

        // —ообщаем внешним системам (например, GameManager) о смерти
        // OnEntityDestroyed?.Invoke(this); 

        Destroy(gameObject);
    }
    protected virtual void OnDestroy() {
        if (_selectionCircle != null) {
            Destroy(_selectionCircle);
        }
    }
}
