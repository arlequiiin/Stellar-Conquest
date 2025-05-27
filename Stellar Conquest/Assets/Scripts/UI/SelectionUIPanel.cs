using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class SelectionUIPanel: MonoBehaviour {
    [Header("Ёлементы UI")]
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI stateText;
    private Entity _currentEntity;


    private void Awake() {
        _uiPanel.SetActive(false);
    }

    void LateUpdate() {
        if (_currentEntity != null) {
            healthText.text = $"{_currentEntity.GetCurrentHealth} / {_currentEntity.GetMaxHealth}";
            if (_currentEntity is Units u)
                stateText.text = u.GetCurrentAction();  
        }
    }

    public void UpdateEntityInfo(Entity entity) {
        if (gameObject == null) return;

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
