using UnityEngine;
using UnityEngine.AI;

public class Engineer : Units {
    private Buildings targetBuilding;
    private float buildDistance = 3f;
    private bool isBuilding = false;

    protected override void Update() {
        base.Update();
        if (isBuilding && targetBuilding != null) {
            float dist = Vector3.Distance(transform.position, targetBuilding.transform.position);
            if (dist <= buildDistance) {
                _currentState = UnitState.Building;

                Debug.Log("Начинаем строить: " + targetBuilding.name);
                _navMeshAgent.ResetPath(); 
                targetBuilding.Build(Time.deltaTime);
                // Анимация строительства, звук и т.д.
                if (targetBuilding.IsCompleted) {
                    isBuilding = false;
                    targetBuilding.IsBuildingInProgress = false;
                    targetBuilding = null;

                    _currentState = UnitState.Idle;
                }
            }
            else {
                _currentState = UnitState.Moving;
                MoveTo(targetBuilding.transform.position);
            }
        }
        // Обработка других приказов
    }
    public void StartBuild(Buildings building) {
        if (building == null || building.IsCompleted) return;
        // убедимся что строительство ещё не идёт
        if (building.IsBuildingInProgress) return;

        if (!ResourceManager.Instance.CanAfford(building.entityData.uraniumCost, building.entityData.energyCost)) {
            Debug.Log("Недостаточно ресурсов для строительства");
            return;
        }
        ResourceManager.Instance.SpendResources(building.entityData.uraniumCost, building.entityData.energyCost);

        targetBuilding = building;
        isBuilding = true;
        building.IsBuildingInProgress = true;
        _currentState = UnitState.Moving; 
    }

    // Вызов при отмене/смерти
    public void CancelBuild() {
        if (isBuilding && targetBuilding != null) {
            targetBuilding.CancelConstruction();
            targetBuilding.IsBuildingInProgress = false;
        }
        isBuilding = false;
        targetBuilding = null;
        _currentState = UnitState.Idle;
    }

    protected override void Die() {
        CancelBuild();
        _animator.SetBool("IsBuilding", false); 
        base.Die();
    }
}
