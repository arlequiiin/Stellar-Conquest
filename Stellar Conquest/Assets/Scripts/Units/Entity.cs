using UnityEngine;

public abstract class Entity : MonoBehaviour {
    [SerializeField] public float _maxHealth;
    private float _currentHealth;

    private GameObject _outlineRenderer;
    protected virtual void Awake() 
{
        _currentHealth = _maxHealth;
    }

    protected virtual void Start() {
        _outlineRenderer.SetActive(false);
    }


    public int OwnerPlayerId { get; protected set; }

    public virtual void TakeDamage(float amount) {
        if (amount <= 0) return;

        _currentHealth -= amount;
        Debug.Log($"{gameObject.name} получил {amount} урона. “екущее здоровье: {_currentHealth}/{_maxHealth}");

        if (_currentHealth <= 0) Die();
    }

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;

    public virtual void Select() {
        if (_outlineRenderer != null) {
            _outlineRenderer.SetActive(true);
        }
        Debug.Log($"{gameObject.name} выбран");
    }

    public virtual void Deselect() {
        if (_outlineRenderer != null) {
            _outlineRenderer.SetActive(false);
        }
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
        if (_outlineRenderer != null) {
            Destroy(_outlineRenderer);
        }
    }
}
