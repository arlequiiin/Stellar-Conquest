using TMPro;
using UnityEngine;

public class ResourcesUI : MonoBehaviour {
    [Header("Текущие ресурсы")]
    [SerializeField] private TextMeshProUGUI currentUraniumText;
    [SerializeField] private TextMeshProUGUI currentEnergyText;

    [Header("Производство")]
    [SerializeField] private TextMeshProUGUI uraniumProductionText;
    [SerializeField] private TextMeshProUGUI energyProductionText;

    [Header("Частота обновления UI")]
    [SerializeField] private float productionUpdateInterval = 2f;

    private void Start() {
        ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;

        HandleResourceChanged(ResourceType.Uranium, ResourceManager.Instance.GetUranium());
        HandleResourceChanged(ResourceType.Energy, ResourceManager.Instance.GetEnergy());

        InvokeRepeating(nameof(UpdateProductionRates), 0f, productionUpdateInterval);
    }

    private void OnDestroy() {
        if (ResourceManager.Instance != null) {
            ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
        }
        CancelInvoke();
    }

    private void HandleResourceChanged(ResourceType type, float amount) {
        switch (type) {
            case ResourceType.Uranium:
                currentUraniumText.text = $"{amount:F0}";
                break;
            case ResourceType.Energy:
                currentEnergyText.text = $"{amount:F0}";
                break;
        }
    }

    private void UpdateProductionRates() {
        uraniumProductionText.text = $"+{ResourceManager.Instance.UraniumProductionRate:F1}/с";
        energyProductionText.text = $"+{ResourceManager.Instance.EnergyProductionRate:F1}/с";
    }
}
