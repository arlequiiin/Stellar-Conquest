using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SelectionUIPanel: MonoBehaviour {
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI healthText;

    public void UpdateEntityInfo(Entity entity) {
        if (entity == null) {
            Clear();
            return;
        }

        gameObject.SetActive(true);

        nameText.text = entity.GetEntityName;
        descriptionText.text = entity.GetDescription;

        if (iconImage != null && entity.GetIcon != null)
            iconImage.sprite = entity.GetIcon;

        healthText.text = $"HP: {entity.GetCurrentHealth} / {entity.GetMaxHealth}";
    }

    public void Clear() {
        gameObject.SetActive(false);
        nameText.text = "";
        descriptionText.text = "";
        iconImage.sprite = null;
        healthText.text = "";
    }
}
