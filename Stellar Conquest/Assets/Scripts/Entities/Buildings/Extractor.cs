using UnityEngine;

public class Extractor : Buildings {
    [Header("Ресурсы")]
    [SerializeField] private ResourceType resourceType = ResourceType.Uranium;
    [SerializeField] private float uranuimPerSecond = 10f;

    private float timer;

    protected override void Start() {
        base.Start();
        if (IsCompleted) {
            ResourceManager.Instance.AddProduction(resourceType, uranuimPerSecond);
        }
    }

    private void Update() {
        if (!IsCompleted) return;

        timer += Time.deltaTime;
        if (timer >= 1f) {
            timer -= 1f;
            ResourceManager.Instance.AddUranium(uranuimPerSecond);
        }
    }

    protected override void FinishConstruction() {
        if (!underConstruction) {
            ResourceManager.Instance.AddProduction(resourceType, uranuimPerSecond);
        }
        base.FinishConstruction();
    }

    protected override void Die() {
        if (!underConstruction) {
            ResourceManager.Instance.RemoveProduction(resourceType, uranuimPerSecond);
        }
        base.Die();
    }
}
