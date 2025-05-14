using UnityEngine;
using UnityEngine.AI;

public class Engineer : Units {
    private Buildings buildingTarget;
    private float buildRange = 2f;

    public void StartConstruction(Buildings target) {
        if (target == null) return;

        buildingTarget = target;
        MoveTo(target.transform.position);
        _currentState = UnitState.Moving;
    }

    protected override void Update() {
        base.Update();

        switch (_currentState) {
            case UnitState.Moving:
                HandleMoveToBuilding();
                break;
            case UnitState.Building:
                HandleBuilding();
                break;
        }
    }

    private void HandleMoveToBuilding() {
        if (buildingTarget == null) {
            _currentState = UnitState.Idle;
            return;
        }

        float distance = Vector3.Distance(transform.position, buildingTarget.transform.position);

        if (distance <= buildRange) {
            StopMoving();
            _currentState = UnitState.Building;
        }
    }

    private void HandleBuilding() {
        if (buildingTarget == null || buildingTarget.IsCompleted) {
            _currentState = UnitState.Idle;
            return;
        }

        buildingTarget.Build(Time.deltaTime);

        if (buildingTarget.IsCompleted) {
            buildingTarget = null;
            _currentState = UnitState.Idle;
        }
    }

    protected override void Die() {
        base.Die();

        if (buildingTarget != null && !buildingTarget.IsCompleted) {
            buildingTarget.CancelConstruction();
        }
    }

    private void StopMoving() {
        if (_navMeshAgent != null && _navMeshAgent.enabled) {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.ResetPath();
        }
    }
}
