using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OrdersUIPanel : MonoBehaviour {
    [Header("Элементы UI")]
    public GameObject _uiPanel;
    public TextMeshProUGUI _entityNameText;
    public TextMeshProUGUI _messageText;
    public GameObject _buttonContainer;

    [Header("Кнопки приказов")]
    public Button moveButton;
    public Button attackButton;
    public Button patrolButton;
    public Button repairButton;
    public Button attackMoveButton;
    public Button cancelButton;

    public void Show(Entity entity) {
        _uiPanel.SetActive(true);
        _entityNameText.text = entity.entityData.entityName;
        _messageText.text = "";
        _buttonContainer.SetActive(true);
    }

    public void Show() {
        _uiPanel.SetActive(true);
        _entityNameText.text = "Несколько юнитов";
        _messageText.text = "";
        _buttonContainer.SetActive(true);
    }

    public void Hide() {
        _uiPanel.SetActive(false);
    }

    public void SetMessage(string msg) {
        _messageText.text = msg;
    }

    public void ClearMessage() {
        _messageText.text = "";
    }
}
