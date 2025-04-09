using UnityEngine;
using UnityEngine.AI;

public class Units : Entity {

    [SerializeField] private float _walkSpeed;
    [SerializeField] public float _range;
    [SerializeField] public int _ammoInClip;
    [SerializeField] public float _recoilAmount;
    [SerializeField] public float _reloadTime;
    [SerializeField] private float _attackDamage;
    [SerializeField] private float _attackCooldown;

    private NavMeshAgent _navMeshAgent;

    private enum UnitState { Idle, Moving, Attacking, Reloading }
    private UnitState _currentState = UnitState.Idle;
    private Entity _currentTarget;
    private float _lastAttackTime;
    private int _currentAmmo;

    protected override void Awake() 
    {
        base.Awake(); 
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _navMeshAgent.speed = _walkSpeed;
        _currentAmmo = _ammoInClip; 
    }

    protected virtual void Update() {
        switch (_currentState) {
            case UnitState.Idle:
                FindTargetAndAttackIfNeeded();
                break;

            case UnitState.Moving:
                if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance) {
                    if (!_navMeshAgent.hasPath || _navMeshAgent.velocity.sqrMagnitude == 0f) {
                        Debug.Log($"{gameObject.name} достиг пункта назначения");
                        _currentState = UnitState.Idle; 
                    }
                }
                // FindTargetAndAttackIfNeeded(); // автоатака в движении
                break;

            case UnitState.Attacking:
                PerformAttack();
                break;

            case UnitState.Reloading:
                // Логика перезарядки 
                break;
        }

        Debug.Log($"Юнит: {gameObject.name}, Состояние: {_currentState}");
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
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _range);
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
            Debug.Log($"{gameObject.name} найдена цель {potentialTarget.name}. Атака");
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

        transform.LookAt(_currentTarget.transform);

        if (Time.time >= _lastAttackTime + _attackCooldown) {
            if (_ammoInClip > 0 && _currentAmmo <= 0) {
                StartReloading(); 
                return; 
            }

            Debug.Log($"{gameObject.name} атакует {_currentTarget.name} нанеся {_attackDamage} урона");
            _currentTarget.TakeDamage(_attackDamage);

            // визуальные эффекты: выстрел, звук, отдача
            ApplyRecoil();

            _lastAttackTime = Time.time; 

            if (_ammoInClip > 0) {
                _currentAmmo--;
                Debug.Log($"{gameObject.name} патронов осталось: {_currentAmmo}/{_ammoInClip}");
            }
        }
    }

    private void ApplyRecoil() {
        transform.position -= transform.forward * _recoilAmount;
        // Или отдача для оружия, если оно отдельный объект
        // weaponTransform.localRotation *= Quaternion.Euler(-_recoilAmount * 10, 0, 0);
        // Debug.Log("Applying recoil effect (visual).");
    }

    private void StartReloading() {
        if (_currentState == UnitState.Reloading) return;

        Debug.Log($"{gameObject.name} начата перезарядка");
        _currentState = UnitState.Reloading;

        StartCoroutine(ReloadCoroutine());
    }

    private System.Collections.IEnumerator ReloadCoroutine() {
        yield return new WaitForSeconds(_reloadTime);
        _currentAmmo = _ammoInClip;
        _currentState = UnitState.Idle; 
        Debug.Log($"{gameObject.name} перезарядился");

        FindTargetAndAttackIfNeeded();
    }

    protected override void Die() {
        Debug.Log($"Юнит {gameObject.name} уничтожен");
        //эффекты смерти юнита (анимация, звук)

        base.Die(); 
    }
}
