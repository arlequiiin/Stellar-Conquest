using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SelectionManager;

public class OrdersUIPanel : MonoBehaviour {
    [Header("�������� UI")]
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private TextMeshProUGUI _entityNameText;
    [SerializeField] private TextMeshProUGUI _messageText;

    private void Awake() {
        _uiPanel.SetActive(false);
    }

    public void Show(Entity entity) {
        _uiPanel.SetActive(true);
        _entityNameText.text = entity.entityData.entityName;
        _messageText.text = "";
    }

    public void Show() {
        _uiPanel.SetActive(true);
        _entityNameText.text = "��������� ������";
        _messageText.text = "";
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

    public void OnMoveOrderPressed() {
        SelectionManager.Instance.SetOrderMode(OrderMode.Move);
    }

    public void OnBuildOrderPressed() {
        SelectionManager.Instance.SetOrderMode(SelectionManager.OrderMode.Build);
        SetMessage("�������� �� ���������� ������ ��� ������� �������� �� �������");
    }
}
