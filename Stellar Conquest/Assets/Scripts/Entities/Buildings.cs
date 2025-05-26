// Buildings.cs
using UnityEngine;

public abstract class Buildings : Entity {
    [Header("Постройка")]
    [SerializeField] protected bool underConstruction = false;
    [SerializeField] protected bool isBuildingInProgress = false;
    public bool IsCompleted => !underConstruction;

    public bool IsBuildingInProgress {
        get => isBuildingInProgress;
        set => isBuildingInProgress = value;
    }


    private float buildProgress = 0f;
    private float currentBuildTime = 0f;

    protected override void Start() {
        base.Start();
        // Например, делаем здание полупрозрачным, если не построено
        if (underConstruction)
            SetInactiveVisual();
    }

    private void Update() {
        // if (!underConstruction) return;
        // Здесь можно обновлять визуал постройки (например, альфа-прозрачность)
    }

    public void Build(float deltaTime) {
        if (!underConstruction) return;
        currentBuildTime += deltaTime;
        buildProgress = Mathf.Clamp01(currentBuildTime / entityData.buildTime);

        UpdateBuildingVisual(buildProgress);
        Debug.Log(buildProgress);

        if (buildProgress >= 1f) {
            FinishConstruction();
        }
    }

    public void CancelConstruction() {
        ResourceManager.Instance.RefundResources(entityData.uraniumCost * 0.6f, entityData.energyCost * 0.6f);

        underConstruction = true;
        isBuildingInProgress = false;
        buildProgress = 0f;
        currentBuildTime = 0f;
        SetInactiveVisual();
    }

    protected virtual void FinishConstruction() {
        underConstruction = false;
        isBuildingInProgress = false;
        buildProgress = 1f;
        currentBuildTime = 0f;
        SetActiveVisual();
    }

    protected override void Die() {
        underConstruction = true;
        isBuildingInProgress = false;
        buildProgress = 0f;
        currentBuildTime = 0f;
        SetInactiveVisual();
    }

    private void SetInactiveVisual() {
        var rend = GetComponent<SpriteRenderer>();
        if (rend != null) {
            Color c = rend.color;
            c.a = 0.5f;
            rend.color = c;
        }
    }

    private void UpdateBuildingVisual(float progress) {
        var rend = GetComponent<SpriteRenderer>();
        if (rend != null) {
            Color c = rend.color;
            c.a = 0.5f + 0.5f * progress;
            rend.color = c;
        }
    }

    private void SetActiveVisual() {
        var rend = GetComponent<SpriteRenderer>();
        if (rend != null) {
            Color c = rend.color;
            c.a = 1f;
            rend.color = c;
        }
    }
}
