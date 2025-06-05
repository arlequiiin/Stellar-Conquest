using UnityEngine;
using UnityEngine.AI; 

public class Scout : Units {
    protected override void Update() {
        switch (_currentState) {
            case UnitState.Idle:
                SetAnimator(false, false, false);
                FindTargetAndAttackIfNeeded();
                break;
            case UnitState.Moving:
                SetAnimator(true, false, false);
                if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance) {
                    if (!_navMeshAgent.hasPath || _navMeshAgent.velocity.sqrMagnitude <= 2f) {
                        Debug.Log($"{gameObject.name} достиг пункта назначени€");
                        _currentState = UnitState.Idle;
                    }
                }
                //FindTargetAndAttackIfNeeded(); // –азведчик может атаковать в движении
                break;
            case UnitState.Attacking:
                SetAnimator(false, true, false);
                PerformAttack();
                break;
            case UnitState.Death:
                break;
            case UnitState.Building:
                SetAnimator(false, false, true);
                // логика строительства
                break;
        }
    }
}