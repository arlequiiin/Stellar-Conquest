using UnityEngine;

public class Extractor : Buildings {
    [SerializeField] private ResourceType _resourceType = ResourceType.Uranium;
    [SerializeField] private float extractionRate = 5f;
    [SerializeField] private float extractionInterval = 1f;
    [SerializeField] private float checkRadius = 1.5f;

    private float timer;
    private bool isOnResourceNode;
    private Uranuim claimedNode;

    protected override void Start() {
        base.Start();

        isOnResourceNode = CheckPlacement();
        if (!isOnResourceNode) {
            Debug.LogError($"{gameObject.name} не размещено над месторождением и не будет работать");
        }
    }

    private void Update() {
        if (isOnResourceNode) {
            timer += Time.deltaTime;
            if (timer >= extractionInterval) {
                timer -= extractionInterval;
                float amount = extractionRate * extractionInterval;

                ResourceManager.Instance.AddUranium(amount);
                Debug.Log($"{gameObject.name} добыл {amount} {_resourceType}");
            }
        }
        else {
            timer = 0;
        }
    }

    private bool CheckPlacement() {
        Collider[] hits = Physics.OverlapSphere(transform.position, checkRadius);
        foreach (var hit in hits) {
            if (hit.TryGetComponent<Uranuim>(out var node)) {
                if (node.ResourceType == _resourceType && node.TryClaim()) {
                    claimedNode = node;
                    return true;
                }
            }
        }
        return false;
    }

    protected override void Die() {
        if (claimedNode != null) {
            claimedNode.Release();
        }
        base.Die();
    }

    protected override void OnDestroy() {
        if (claimedNode != null) {
            claimedNode.Release();
        }
        base.OnDestroy();
    }
}
