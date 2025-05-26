using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SelectionUIPanel: MonoBehaviour {
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI stateText;
    private Entity _currentEntity;


    void LateUpdate() {
        if (_currentEntity != null) {
            healthText.text = $"{_currentEntity.GetCurrentHealth} / {_currentEntity.GetMaxHealth}";
            if (_currentEntity is Units u)
                stateText.text = u.GetCurrentAction();  
        }
    }

    public void UpdateEntityInfo(Entity entity) {
        if (entity == null) {
            Hide();
            return;
        }
        _currentEntity = entity;
        gameObject.SetActive(true);

        nameText.text = entity.GetEntityName;
        descriptionText.text = entity.GetDescription;

        if (iconImage != null && entity.GetIcon != null)
            iconImage.sprite = entity.GetIcon;

        healthText.text = $"{entity.GetCurrentHealth} / {entity.GetMaxHealth}";
    }

    public void UpdateHealth(float current, float max) {
        healthText.text = $"{current} / {max}";
    }


    public void Hide() {
        gameObject.SetActive(false);
        nameText.text = "";
        descriptionText.text = "";
        iconImage.sprite = null;
        healthText.text = "";
    }
}
