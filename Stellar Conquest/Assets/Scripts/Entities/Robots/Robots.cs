using UnityEngine;
using UnityEngine.AI;

public class Robots : Entity
{
    [SerializeField] private float _walkSpeed;
    [SerializeField] private float _range;
    [SerializeField] private float _attackDamage;
    [SerializeField] private float _attackCooldown;

    protected NavMeshAgent _navMeshAgent;

    protected enum UnitState { Idle, Moving, Attacking, Death }
    protected UnitState _currentState = UnitState.Idle;
    private Entity _currentTarget;
    private float _lastAttackTime;
    private Animator _animator;
    private bool _isMoving;
    private bool _isFiring;
    private bool _updateRotation = false;
    private bool _updateUpAxis = false;

    protected override void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _navMeshAgent.speed = _walkSpeed;
        _navMeshAgent.updateRotation = _updateRotation;
        _navMeshAgent.updateUpAxis = _updateUpAxis;
        _navMeshAgent.SetAreaCost(0, 1);
        _animator = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        switch (_currentState)
        {
            case UnitState.Idle:
                SetAnimator(false, false);
                FindTargetAndAttackIfNeeded();
                break;
            case UnitState.Moving:
                SetAnimator(true, false);
                if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
                {
                    if (!_navMeshAgent.hasPath || _navMeshAgent.velocity.sqrMagnitude <= 2f)
                    {
                        _currentState = UnitState.Idle;
                    }
                }
                break;
            case UnitState.Attacking:
                SetAnimator(false, true);
                PerformAttack();
                break;
            case UnitState.Death:
                break;

        }
    }

    private void SetAnimator(bool isMoving, bool isFiring)
    {
        if (_isMoving != isMoving)
        {
            _isMoving = isMoving; 
            _animator.SetBool("IsMoving", _isMoving);
        }

        if (_isFiring != isFiring)
        {
            _isFiring = isFiring;
            _animator.SetBool("IsFiring", _isFiring);
        }
    }

    public void MoveTo(Vector3 destination)
    {
        FlipSprite(destination);
        if (_navMeshAgent.SetDestination(destination))
        {
            _currentState = UnitState.Moving;
            _currentTarget = null;
        }
    }

    private void FindTargetAndAttackIfNeeded()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _range);
        Entity potentialTarget = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            Entity entity = hitCollider.GetComponent<Entity>();

            if (entity != null && entity != this && entity.OwnerPlayerId == 1)
            {
                float distance = Vector3.Distance(transform.position, entity.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    potentialTarget = entity;
                }
            }
        }

        if (potentialTarget != null)
        {
            OrderAttackTarget(potentialTarget);
        }
    }

    private void OrderAttackTarget(Entity target)
    {
        if (target == null || target == this) return;

        _currentTarget = target;
        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

        if (distanceToTarget > _range)
        {
            Vector3 targetPosition = _currentTarget.transform.position;
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            Vector3 destination = targetPosition - directionToTarget * (_range * 0.9f);

            if (_navMeshAgent.SetDestination(destination))
            {
                _currentState = UnitState.Moving;
            }
            else
            {
                _currentTarget = null;
                _currentState = UnitState.Idle;
            }
        }
        else
        {
            _currentState = UnitState.Attacking;
            _navMeshAgent.ResetPath();
            FlipSprite(_currentTarget.transform.position);
        }
    }

    private void PerformAttack()
    {
        if (_currentTarget == null || _currentTarget.GetCurrentHealth <= 0)
        {
            StopActions();
            return;
        }
        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);


        FlipSprite(_currentTarget.transform.position);
        if (distanceToTarget > _range)
        {
            OrderAttackTarget(_currentTarget);
            return;
        }

        if (Time.time >= _lastAttackTime + _attackCooldown)
        {
            _currentTarget.TakeDamage(_attackDamage);
            _lastAttackTime = Time.time;
        }
    }

    private void FlipSprite(Vector3 targetPosition)
    {
        Vector3 scale = transform.localScale;

        if (targetPosition.x < transform.position.x && scale.x > 0)
        {
            scale.x = -1;
        }
        else if (targetPosition.x > transform.position.x && scale.x < 0)
        {
            scale.x = -1;
        }

        transform.localScale = scale;
    }

    public void StopActions()
    {
        _navMeshAgent.ResetPath();
        _currentState = UnitState.Idle;
        _currentTarget = null;
    }

    protected override void Die()
    {
        _animator.SetBool("IsMoving", false);
        _animator.SetBool("IsFiring", false);

        _animator.SetTrigger("Die");
        _currentState = UnitState.Death;
        _navMeshAgent.enabled = false;

        base.Die();
    }
}