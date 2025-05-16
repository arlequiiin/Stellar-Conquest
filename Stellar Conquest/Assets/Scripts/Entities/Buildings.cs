using UnityEngine;
using UnityEngine.AI;
using NavMeshPlus.Components;
using Unity.VisualScripting;

public abstract class Buildings : Entity {
    [Header("Постройка")]
    [SerializeField] protected bool underConstruction = true;

    public bool IsCompleted => !underConstruction;

    protected override void Start() {
        base.Start();
    }

    private void Update() {
        if (!underConstruction) return;

    }

    public void Build(float deltaTime) {
        if (!underConstruction) return;
        //будет строится entityData.buildTime времени
        FinishConstruction();

        ResourceManager.Instance.SpendResources(entityData.uraniumCost, entityData.energyCost);
    }

    public void CancelConstruction() {
        ResourceManager.Instance.RefundResources(entityData.uraniumCost * 0.6f, entityData.energyCost * 0.6f);
        // потом станет неактивным
    }

    protected virtual void FinishConstruction() {
        underConstruction = false;
    }

    protected override void Die() {
        base.Die();
    }
}
