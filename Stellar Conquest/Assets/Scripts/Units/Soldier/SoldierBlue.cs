using UnityEngine;
using UnityEngine.AI;

public class SoldierBlue : MonoBehaviour {
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] float _health = 100f;
    [SerializeField] public float _maxHealth = 100f;
    [SerializeField] public float _range = 10f;
    [SerializeField] public int _ammoInClip = 10;
    [SerializeField] public float _walkSpeed = 3f;
    [SerializeField] public float _recoilAmount = 1f;

    private NavMeshAgent _agent;    
    private Rigidbody2D _rb;
    private Collider2D _unitCollider;

    private Vector2 _targetPosition;
    private GameObject _outlineRenderer;

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _unitCollider = GetComponent<Collider2D>();
        _outlineRenderer = transform.Find("SoliderBlueVisual/OutlineRenderer").gameObject;

        _targetPosition = transform.position;
    }

    private void Start() {
        _outlineRenderer.SetActive(false);
    }

    void Update() {

    }
    public void Select() {
        // Включаем выделение
        _outlineRenderer.SetActive(true);
    }

    public void Deselect() {
        // Выключаем выделение
        _outlineRenderer.SetActive(false);
    }

    public void MoveTo() {

    }
}
