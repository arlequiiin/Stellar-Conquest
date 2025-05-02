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
        Debug.Log($"{gameObject.name} �������� ���������");
    }

    public virtual void TakeDamage(float amount) {
        if (amount <= 0) return;

        _currentHealth -= amount;
        Debug.Log($"{gameObject.name} ������� {amount} �����. ������� ��������: {_currentHealth}/{_maxHealth}");

        if (_currentHealth <= 0) Die();
    }

    public bool IsAlive => _currentHealth > 0;
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;

    public virtual void Select() {
        if (_selectionCircle != null)
            _selectionCircle.SetActive(true);
    
        Debug.Log($"{gameObject.name} ������");
    }

    public virtual void Deselect() {
        if (_selectionCircle != null)
            _selectionCircle.SetActive(false);

        Debug.Log($"{gameObject.name} �� ������");
    }

    public virtual void SetOwner(int playerId) {
        OwnerPlayerId = playerId;
        // ��������� ����� ������
    }

    protected virtual void Die() {
        Debug.Log($"{gameObject.name} ��� ���������");
        //  ������ ������������ �������� ������, ������, ��������

        // �������� ������� �������� (��������, GameManager) � ������
        // OnEntityDestroyed?.Invoke(this); 

        Destroy(gameObject, 5f); // ������ ����� 5 ���
    }
    protected virtual void OnDestroy() {
    
    }
}
