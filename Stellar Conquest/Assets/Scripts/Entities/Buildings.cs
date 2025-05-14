using UnityEngine;
using UnityEngine.AI;
using NavMeshPlus.Components;

public abstract class Buildings : Entity {
    public bool isExtractor;
    public GameObject previewPrefab;
    private float buildProgress;

    private bool underConstruction = true;
    private NavMeshObstacle obstacle;
    public bool IsCompleted => !underConstruction;

    protected override void Start() {
        base.Start();
    }

    protected override void Die() {
        base.Die();
    }

    public void Build(float deltaTime) {
        if (!underConstruction) return;

        buildProgress += deltaTime;

        if (buildProgress >= entityData.buildTime) {
            FinishConstruction();
        }
    }

    public void CancelConstruction() {
        ResourceManager.Instance.RefundResources(
            entityData.uraniumCost * 0.6f,
            entityData.energyCost * 0.6f
        );

        Destroy(gameObject);
    }

    private void FinishConstruction() {
        underConstruction = false;

        var obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle != null) obstacle.enabled = true;

        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        foreach (var surface in surfaces) {
            surface.BuildNavMesh();
        }
    }
}
