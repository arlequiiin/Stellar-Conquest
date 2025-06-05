using UnityEngine;

public class Generator : Buildings {
    [Header("Ресурсы")]
    [SerializeField] private ResourceType resourceType = ResourceType.Energy;
    [SerializeField] private float powerPerSecond = 10f;

    private float timer;

    protected override void Start() {
        base.Start();
        if (IsCompleted) {
            ResourceManager.Instance.AddProduction(resourceType, powerPerSecond);
        }
    }

    private void Update() {
        if (!IsCompleted) return;

        timer += Time.deltaTime;
        if (timer >= 1f) {
            timer -= 1f;
            ResourceManager.Instance.AddEnergy(powerPerSecond);
        }
    }

    protected override void FinishConstruction() {
        base.FinishConstruction();
        if (!underConstruction) {
            ResourceManager.Instance.AddProduction(resourceType, powerPerSecond);
        }
    }

    protected override void Die() {
        if (!underConstruction) {
            ResourceManager.Instance.RemoveProduction(resourceType, powerPerSecond);
        }

        base.Die();
    }
}
