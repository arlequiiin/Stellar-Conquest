using UnityEngine;
using UnityEngine.AI;

public class Units : Entity {

    [SerializeField] private float _walkSpeed;
    [SerializeField] public float _range;
    [SerializeField] private float _attackDamage;
    [SerializeField] private float _attackCooldown;


    private NavMeshAgent _navMeshAgent;

    private enum UnitState { Idle, Moving, Attacking, Death }
    private UnitState _currentState = UnitState.Idle;
    private Entity _currentTarget;
    private float _lastAttackTime;
    private Animator _animator;
    private bool _isMoving;
    private bool _isFiring;
    private bool _updateRotation = false;
    private bool _updateUpAxis = false;

    protected override void Awake() 
    {
        base.Awake(); 
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _navMeshAgent.speed = _walkSpeed;
        _navMeshAgent.updateRotation = _updateRotation;
        _navMeshAgent.updateUpAxis = _updateUpAxis;

        _animator = GetComponent<Animator>();
    }

    protected virtual void Update() {
        switch (_currentState) {
            case UnitState.Idle:
                SetAnimator(false, false);
                FindTargetAndAttackIfNeeded();
                break;
            case UnitState.Moving:
                SetAnimator(true, false);
                if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance) {
                    if (!_navMeshAgent.hasPath || _navMeshAgent.velocity.sqrMagnitude == 0f) {
                        Debug.Log($"{gameObject.name} достиг пункта назначения");
                        _currentState = UnitState.Idle; 
                    }
                }
                // FindTargetAndAttackIfNeeded(); // автоатака в движении
                break;
            case UnitState.Attacking:
                SetAnimator(false, true);
                PerformAttack();
                break;
            case UnitState.Death:
                break;
        }

        // #if UNITY_EDITOR
        // Debug.Log($"Юнит: {gameObject.name}, Состояние: {_currentState}");
        // #endif    
    }

    private void SetAnimator(bool isMoving, bool isFiring) {
        if (_isMoving != isMoving) {
            _animator.SetBool("IsMoving", isMoving);
            _isMoving = isMoving;
        }

        if (_isFiring != isFiring) {
            _animator.SetBool("IsFiring", isFiring);
            _isFiring = isFiring;
        }
    }


    public void MoveTo(Vector3 destination) {
        if (_navMeshAgent.SetDestination(destination)) {
            _currentState = UnitState.Moving;
            _currentTarget = null; 
            Debug.Log($"{gameObject.name} двигается в {destination}");
        }
        else {
            Debug.LogWarning($"{gameObject.name} не может достичь цели {destination}");
        }
    }

    public void OrderAttackTarget(Entity target) {
        if (target == null || target == this || target.OwnerPlayerId == this.OwnerPlayerId)
        {
            Debug.LogWarning($"{gameObject.name} не может атаковать {target?.name}");
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
                Debug.Log($"{gameObject.name} движется для атаки {target.name}");
            }
            else {
                Debug.LogWarning($"{gameObject.name} не может достичь цели {target.name}.");
                _currentTarget = null;
                _currentState = UnitState.Idle;
            }
        }
        else 
        {
            Debug.Log($"{gameObject.name} цель {target.name} в радиусе обзора. Атака");
            _currentState = UnitState.Attacking;
            _navMeshAgent.ResetPath(); 
            transform.LookAt(_currentTarget.transform); 
        }
    }

    public void StopActions() {
        _navMeshAgent.ResetPath();
        _currentState = UnitState.Idle;
        _currentTarget = null; 
        Debug.Log($"{gameObject.name} сброшен приказ");
    }

    private void FindTargetAndAttackIfNeeded() {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _range); 
        Entity potentialTarget = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders) {
            Entity entity = hitCollider.GetComponent<Entity>();

            if (entity != null && entity != this && entity.OwnerPlayerId != this.OwnerPlayerId) {
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

    private void PerformAttack() {
        if (_currentTarget == null || _currentTarget.CurrentHealth <= 0) {
            StopActions();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);
        if (distanceToTarget > _range) {
            Debug.Log($"{gameObject.name} цель {_currentTarget.name} за пределом радиуса обзора");
            // либо снова вызвать OrderAttackTarget(_currentTarget), либо StopActions()
            OrderAttackTarget(_currentTarget); // Попробуем догнать
            return;
        }

        transform.LookAt(_currentTarget.transform); // ?

        if (Time.time >= _lastAttackTime + _attackCooldown) {
            Debug.Log($"{gameObject.name} атакует {_currentTarget.name} нанеся {_attackDamage} урона");
            _currentTarget.TakeDamage(_attackDamage);

            // визуальные эффекты: выстрел, звук, отдача

            _lastAttackTime = Time.time; 
        }
    }

    protected override void Die() {
        Debug.Log($"Юнит {gameObject.name} уничтожен");
        _currentState = UnitState.Death;
        _navMeshAgent.enabled = false;
        _animator.SetBool("IsMoving", false);
        _animator.SetBool("IsFiring", false);
        _animator.SetTrigger("IsDead");

        base.Die();
    }
}
