using UnityEngine;

public abstract class Entity : MonoBehaviour {
    [SerializeField] private float maxHealth;
    [SerializeField] private GameObject selectionCircle;
    public int OwnerPlayerId { get; protected set; }
    public EntityData entityData; 

    private float currentHealth;
    public string currentStatus;

    private bool isDie = false;
    public bool IsAlive => currentHealth > 0;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    protected virtual void Awake() {
        currentHealth = maxHealth;
        OwnerPlayerId = 1;
    }

    protected virtual void Start() {
        selectionCircle?.SetActive(false);
    }

    protected virtual void OnDestroy() {

    }

    public virtual void TakeDamage(float amount) {
        if (amount <= 0) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} ������� {amount} �����. ������� ��������: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) Die();
    }

    public virtual void Select() {
        if (selectionCircle != null)
            selectionCircle.SetActive(true);
        }

    public virtual void Deselect() {
        if (selectionCircle != null)
            selectionCircle.SetActive(false);
    }

    public virtual void SetOwner(int playerId) {
        OwnerPlayerId = playerId;
        // ��������� ����� ������
    }

    protected virtual void Die() {
        if (isDie) return;
            isDie = true;

        Debug.Log($"{gameObject.name} ��� ���������");
        //  ������ ������������ �������� ������, ������, ��������

        // OnEntityDestroyed?.Invoke(this); 

        Destroy(gameObject, 5f); 
    }
}
