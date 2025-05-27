using UnityEngine;
using UnityEngine.AI;

public class Units : Entity {

    [SerializeField] private float _walkSpeed;
    [SerializeField] public float _range;
    [SerializeField] private float _attackDamage;
    [SerializeField] private float _attackCooldown;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private AudioClip shootSound;

    protected AudioSource audioSource;
    protected NavMeshAgent _navMeshAgent;

    protected enum UnitState { Idle, Moving, Attacking, Death, Building }
    protected UnitState _currentState = UnitState.Idle;
    protected Entity _currentTarget;
    protected float _lastAttackTime;
    protected Animator _animator;
    protected bool _isMoving;
    protected bool _isFiring;
    protected bool _isBuilding;
    protected bool _updateRotation = false;
    protected bool _updateUpAxis = false;
    // �������� ��������� ��� ��������!
    protected override void Awake() 
    {
        base.Awake(); 
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _navMeshAgent.speed = _walkSpeed;
        _navMeshAgent.updateRotation = _updateRotation;
        _navMeshAgent.updateUpAxis = _updateUpAxis;
        _animator = GetComponent<Animator>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    protected virtual void Update() {
        switch (_currentState) {
            case UnitState.Idle:
                SetAnimator(false, false, false);
                FindTargetAndAttackIfNeeded();
                break;
            case UnitState.Moving:
                SetAnimator(true, false, false);
                if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance) {
                    if (!_navMeshAgent.hasPath || _navMeshAgent.velocity.sqrMagnitude <= 2f) {
                        Debug.Log($"{gameObject.name} ������ ������ ����������");
                        _currentState = UnitState.Idle; 
                    }
                }
                // FindTargetAndAttackIfNeeded(); // ��������� � ��������
                break;
            case UnitState.Attacking:
                SetAnimator(false, true, false);
                PerformAttack();
                break;
            case UnitState.Death:
                break;
            case UnitState.Building:
                SetAnimator(false, false, true);
                // ������ ������������� 
                break;
        }
    }

    protected void SetAnimator(bool isMoving, bool isFiring, bool isBuilding) {
        if (_isMoving != isMoving) {
            _animator.SetBool("IsMoving", isMoving);
            _isMoving = isMoving;
        }

        if (_isFiring != isFiring) {
            _animator.SetBool("IsFiring", isFiring);
            _isFiring = isFiring;
        }

        if (_isBuilding != isBuilding) {
            _animator.SetBool("IsBuilding", isBuilding);
            _isBuilding = isBuilding;
        }
    }

    public virtual string GetCurrentAction() {
        switch (_currentState) {
            case UnitState.Idle: return "�������";
            case UnitState.Moving: return "� ��������";
            case UnitState.Attacking: return "�������";
            case UnitState.Death: return "���������";
            case UnitState.Building: return "������";
            default: return "";
        }
    }

    public void MoveTo(Vector3 destination) {
        FlipSprite(destination);
        if (_navMeshAgent.SetDestination(destination)) {
            _currentState = UnitState.Moving;
            _currentTarget = null; 
            Debug.Log($"{gameObject.name} ��������� � {destination}");
        }
        else {
            Debug.LogWarning($"{gameObject.name} �� ����� ������� ���� {destination}");
        }
    }

    public void OrderAttackTarget(Entity target) {
        FlipSprite(target.transform.position);
        if (target == null || target == this || target.OwnerPlayerId == this.OwnerPlayerId)
        {
            Debug.LogWarning($"{gameObject.name} �� ����� ��������� {target?.name}");
            StopActions();
            return;
        }

        _currentTarget = target;
        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

        if (distanceToTarget > _range) {
            Vector3 targetPosition = _currentTarget.transform.position;

            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            Vector3 destination = targetPosition - directionToTarget * (_range * 0.9f); 

            if (_navMeshAgent.SetDestination(destination)) {
                _currentState = UnitState.Moving; 
                Debug.Log($"{gameObject.name} �������� ��� ����� {target.name}");
            }
            else {
                Debug.LogWarning($"{gameObject.name} �� ����� ������� ���� {target.name}.");
                _currentTarget = null;
                _currentState = UnitState.Idle;
            }
        }
        else 
        {
            Debug.Log($"{gameObject.name} ���� {target.name} � ������� ������. �����");
            _currentState = UnitState.Attacking;
            _navMeshAgent.ResetPath();

            FlipSprite(_currentTarget.transform.position);
        }
    }

    public void StopActions() {
        _navMeshAgent.ResetPath();
        _currentState = UnitState.Idle;
        _currentTarget = null; 
        Debug.Log($"{gameObject.name} ������� ������");
    }

    protected void FindTargetAndAttackIfNeeded() {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _range); 
        Entity potentialTarget = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders) {
            Entity entity = hitCollider.GetComponent<Entity>();

            if (entity != null && entity != this && entity.OwnerPlayerId != 1) {
                float distance = Vector3.Distance(transform.position, entity.transform.position);
                if (distance < minDistance) {
                    minDistance = distance;
                    potentialTarget = entity;
                }
            }
        }

        if (potentialTarget != null) {
            OrderAttackTarget(potentialTarget);
        }
    }

    protected void PerformAttack() {
        if (_currentTarget == null) {
            Debug.Log("��� ����");
        }
        if (_currentTarget.GetCurrentHealth <= 0) {
            Debug.Log("��� �������� � ����");
        }
        if (_currentTarget == null || _currentTarget.GetCurrentHealth <= 0) {
            StopActions();
            return;
        }
        FlipSprite(_currentTarget.transform.position);

        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);
        if (distanceToTarget > _range) {
            OrderAttackTarget(_currentTarget);
            return;
        }

        if (Time.time >= _lastAttackTime + _attackCooldown) {
            ShootBullet(_currentTarget); // �������� � �����
            _lastAttackTime = Time.time;
        }
    }

    protected void ShootBullet(Entity target) {
        if (bulletPrefab == null) { Debug.LogWarning("bulletPrefab ����!"); return; }
        if (firePoint == null) { Debug.LogWarning("firePoint ����!"); return; }

        Debug.Log("����� ����"); // <- ������� �� �� ���� ���?

        var bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        var bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null) {
            bullet.Init(target, _attackDamage);
        }

        if (shootSound != null) {
            audioSource.PlayOneShot(shootSound);
        }
    }



    protected void FlipSprite(Vector3 targetPosition) {
        Vector3 scale = transform.localScale;

        if (targetPosition.x < transform.position.x && scale.x > 0)
        {
            scale.x *=-1 ;
        }
        else if (targetPosition.x > transform.position.x && scale.x < 0)
        {
            scale.x  *= -1;
        }

        transform.localScale = scale;
    }


    protected override void Die()
    {
        _animator.SetBool("IsMoving", false);
        _animator.SetBool("IsFiring", false);
        _animator.SetBool("IsBuilding", false); // ����������� ������� �� ���?

        _animator.SetTrigger("Die");
        _currentState = UnitState.Death;
        _navMeshAgent.enabled = false;

        base.Die();
    
     }
}
