using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using System.Collections.Generic;
using System.Linq; 

public class FactoryUIPanel : MonoBehaviour {
    [Header("�������� UI")]
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private TextMeshProUGUI _buildingNameText;
    [SerializeField] private TextMeshProUGUI _hpText;

    [Header("������ ������")]
    [SerializeField] private Button[] _unitButtons;
    [SerializeField] private TextMeshProUGUI[] _unitButtonTexts; 

    [Header("������� ������������")]
    [SerializeField] private Transform _queueContainer;
    [SerializeField] private GameObject _queueItemPrefab; 

    [Header("������� ������������")]
    [SerializeField] private Image _currentProductionIcon;
    [SerializeField] private TextMeshProUGUI _productionProgressText;
    [SerializeField] private GameObject _currentProductionDisplay;

    [Header("���������")]
    [SerializeField] private TextMeshProUGUI _messageText; 
    [SerializeField] private float _messageDisplayTime = 3f; 

    private Factory _currentFactory; 
    private List<Image> _queueItemIcons = new List<Image>();
    private Coroutine _messageCoroutine;

    private void Awake() {
        _uiPanel.SetActive(false);
    }

    // ����������, ����� ����� �������� ������� (��� SelectionManager)
    // private void OnEntitySelected(Entity entity)
    // {
    //     if (entity is Factory factory) // ���� ��������� �������� - �������
    //     {
    //         Show(factory);
    //     }
    //     else
    //     {
    //         Hide(); // ������ UI �������, ���� ������� ���-�� ������
    //     }
    // }

    // ����������, ����� ����� �������� ����� � ���������
    // private void OnEntityDeselected(Entity entity)
    // {
    //     if (entity == _currentFactory) // ���� ����� � ��������� ������� �������
    //     {
    //         Hide();
    //     }
    // }

    public void Show(Factory factory) {
        if (_currentFactory != null) {
            Hide();
        }

        _currentFactory = factory;
        _uiPanel.SetActive(true);

        _buildingNameText.text = factory.gameObject.name;
        _hpText.text = $"HP: {factory.CurrentHealthInt}";

        // SetupUnitButtons(factory.ProducibleUnits);

        factory.OnQueueChanged += UpdateQueueUI;
        factory.OnProductionProgressUpdated += UpdateProductionProgressUI;
        factory.OnProductionMessage += DisplayMessage;
        // factory.OnHealthChanged += UpdateBuildingStats; 

        UpdateQueueUI(_currentFactory.ProductionQueue.ToList(), _currentFactory.CurrentProduction);
        UpdateProductionProgressUI(_currentFactory.CurrentProductionTimer / (_currentFactory.CurrentProduction?.ProductionTime ?? 1f)); // ��������� ������� �� ����

        Debug.Log($"UI ��� ������� {factory.name} �������.");
    }

    public void Hide() {
        if (_currentFactory != null) {
            _currentFactory.OnQueueChanged -= UpdateQueueUI;
            _currentFactory.OnProductionProgressUpdated -= UpdateProductionProgressUI;
            _currentFactory.OnProductionMessage -= DisplayMessage;
            // if (_currentFactory is Buildings building) building.OnHealthChanged -= UpdateBuildingStats;

            _currentFactory = null;
        }

        _uiPanel.SetActive(false);
        // ������� UI (�������, ��������)
        ClearQueueUI();
        _currentProductionIcon.sprite = null; 
        _currentProductionIcon.enabled = false; 
        _productionProgressText.text = "";
        _currentProductionDisplay.SetActive(false);
        _messageText.text = "";
    }

    // ����� ��� ���������� ����������� HP � ������ ������ ������
    // private void UpdateBuildingStats()
    // {
    //      if (_currentFactory != null)
    //      {
    //           _hpText.text = $"HP: {_currentFactory.CurrentHealthInt}";
    //           // �������� ������ �����, ���� ����
    //      }
    // }


    // ����������� ������ ������ �� ������ ������ �� �������
    private void SetupUnitButtons(List<Factory.UnitProductionInfo> producibleUnits) {
        if (_unitButtons.Length != producibleUnits.Count) {
            Debug.LogError("���������� ������ ������ �� ��������� � ����������� ������������ ������ � �������!");
        }

        for (int i = 0; i < _unitButtons.Length; i++) {
            if (i < producibleUnits.Count) {
                Factory.UnitProductionInfo unitInfo = producibleUnits[i];

                // ����������� ������ ������
                if (_unitButtons[i].image != null && unitInfo.UnitIcon != null) {
                    _unitButtons[i].image.sprite = unitInfo.UnitIcon;
                    _unitButtons[i].image.enabled = true;
                }
                else if (_unitButtons[i].image != null) {
                    _unitButtons[i].image.enabled = false; // ������ Image ���� ��� ������
                }


                if (_unitButtonTexts.Length > i && _unitButtonTexts[i] != null) {
                    // ���������� �������� � ���������
                    _unitButtonTexts[i].text = $"{unitInfo.UnitName}\nCost: {unitInfo.UranuimCost}";
                    // TODO: ������������ ������ �������� ������ ������ "Cost:"
                }

                // ����������� ���� ������ � ������ ������������ � ������� �������
                // ������� ������� ��� ���������� ���������, ����� �������� ������� �������
                _unitButtons[i].onClick.RemoveAllListeners();
                int unitIndex = i; // ��������� ������ ��� �������� � ���������
                _unitButtons[i].onClick.AddListener(() => OnUnitButtonClicked(unitIndex));

                _unitButtons[i].interactable = true; // ���������� ������ ������� (�������� �������� ����� �����)

            }
            else {
                // ���� ������ ������, ��� ������, �������� ��� ��������� ������ ������
                _unitButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnUnitButtonClicked(int unitIndex) {
        if (_currentFactory != null) {
            bool success = _currentFactory.TryQueueUnitByIndex(unitIndex);
        }
    }

    // ���������� ����������� ����������� �������
    private void UpdateQueueUI(List<Factory.UnitProductionInfo> queueList, Factory.UnitProductionInfo currentProduction) {
        ClearQueueUI(); 

        if (currentProduction != null) {
            if (_currentProductionIcon != null && currentProduction.UnitIcon != null) {
                _currentProductionIcon.sprite = currentProduction.UnitIcon;
                _currentProductionIcon.enabled = true;
                _currentProductionDisplay.SetActive(true);
            }
            else if (_currentProductionIcon != null) {
                _currentProductionIcon.enabled = false; // ������ ������ ���� ��� �������
                _currentProductionDisplay.SetActive(true); // �� ��� � ����� ����� ���� �����
            }
            // �������� ��� � ����� ����� ����������� ����� UpdateProductionProgressUI
        }
        else {
            if (_currentProductionIcon != null) _currentProductionIcon.enabled = false;
            _productionProgressText.text = "";
            _currentProductionDisplay.SetActive(false); // ������ ���� �������� ������������
        }

        // ��������� ������ ������ �� �������
        foreach (var unitInfo in queueList) {
            if (_queueItemPrefab != null && _queueContainer != null) {
                GameObject queueItemGO = Instantiate(_queueItemPrefab, _queueContainer);
                Image iconImage = queueItemGO.GetComponent<Image>(); // ������ ������ ����� Image ���������

                if (iconImage != null && unitInfo.UnitIcon != null) {
                    iconImage.sprite = unitInfo.UnitIcon;
                }
                else if (iconImage != null) {
                    iconImage.enabled = false; // ������ Image ���� ��� �������
                }
                _queueItemIcons.Add(iconImage); // ��������� ������ �� ��������� ������

                // TODO: ����������� ������ �� ����� �� ������ � �������
                // ��� ����� Prefab �������� ������� ������ ���� ������� ��� ����� ������ � �������/������������ �����
                // � ���������� ���� ������ � ������� ��� ������.
            }
        }
    }

    // ������� ���������� ����������� �������
    private void ClearQueueUI() {
        foreach (var icon in _queueItemIcons) {
            if (icon != null && icon.gameObject != null) {
                Destroy(icon.gameObject);
            }
        }
        _queueItemIcons.Clear();
    }

    // ����� ��� ���������� �������� ����
    private void UpdateProductionProgressUI(float progress) // progress �� 0 �� 1
    {
        if (_productionProgressText != null) {
            // ����� ���������� ������� ��� ���������� �����
            if (_currentFactory != null && _currentFactory.CurrentProduction != null) {
                float remainingTime = _currentFactory.CurrentProduction.ProductionTime - _currentFactory.CurrentProductionTimer;
                _productionProgressText.text = $"{Mathf.CeilToInt(remainingTime)}s"; // ��������, ����������� ���������� �����
                                                                                     // _productionProgressText.text = $"{Mathf.RoundToInt(progress * 100)}%"; // ��� �������
            }
            else {
                _productionProgressText.text = ""; // �������� ����� ���� ������ �� ��������
            }
        }

        // ����������/�������� ���� �������� ������������ � ����������� �� ����, �������� �� ���-��
        if (_currentProductionDisplay != null) {
            _currentProductionDisplay.SetActive(_currentFactory != null && _currentFactory.CurrentProduction != null);
        }
    }

    // ����� ��� ����������� ��������� (��������, "��� ��������")
    private void DisplayMessage(string message) {
        if (_messageText != null) {
            _messageText.text = message;
            if (_messageCoroutine != null) {
                StopCoroutine(_messageCoroutine);
            }
            _messageCoroutine = StartCoroutine(ClearMessageAfterDelay(_messageDisplayTime));
        }
    }

    private System.Collections.IEnumerator ClearMessageAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        if (_messageText != null) {
            _messageText.text = "";
        }
        _messageCoroutine = null;
    }

    // TODO: ����� ��� ��������� ����� �� ������ � ������� ��� ������ (��������� ��������� Prefab �������)
    // TODO: ����� ��� ������ ������ �������� ������������ (�������� _currentFactory.CancelProduction(0))
    // TODO: ����� ��� ������ ��������� ����� ����� (_currentFactory._rallyPoint = ...)

    void OnDestroy() {
        // ������������ �� �������, ���� ������ UI ������������ ������ �������
        if (_currentFactory != null) {
            _currentFactory.OnQueueChanged -= UpdateQueueUI;
            _currentFactory.OnProductionProgressUpdated -= UpdateProductionProgressUI;
            _currentFactory.OnProductionMessage -= DisplayMessage;
            // if (_currentFactory is Buildings building) building.OnHealthChanged -= UpdateBuildingStats;
        }
    }
}