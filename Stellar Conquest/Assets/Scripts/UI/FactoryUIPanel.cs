using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using System.Collections.Generic;
using System.Linq; 

public class FactoryUIPanel : MonoBehaviour {
    [Header("Элементы UI")]
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private TextMeshProUGUI _buildingNameText;
    [SerializeField] private TextMeshProUGUI _hpText;

    [Header("Кнопки юнитов")]
    [SerializeField] private Button[] _unitButtons;
    [SerializeField] private TextMeshProUGUI[] _unitButtonTexts; 

    [Header("Очередь производства")]
    [SerializeField] private Transform _queueContainer;
    [SerializeField] private GameObject _queueItemPrefab; 

    [Header("Текущее производство")]
    [SerializeField] private Image _currentProductionIcon;
    [SerializeField] private TextMeshProUGUI _productionProgressText;
    [SerializeField] private GameObject _currentProductionDisplay;

    [Header("Сообщения")]
    [SerializeField] private TextMeshProUGUI _messageText; 
    [SerializeField] private float _messageDisplayTime = 3f; 

    private Factory _currentFactory; 
    private List<Image> _queueItemIcons = new List<Image>();
    private Coroutine _messageCoroutine;

    private void Awake() {
        _uiPanel.SetActive(false);
    }

    public void Show(Factory factory) {
        if (_currentFactory != null) {
            Hide();
        }

        _currentFactory = factory;
        _uiPanel.SetActive(true);

        _buildingNameText.text = factory.gameObject.name;
        _hpText.text = $"HP: {factory.CurrentHealthInt}";

        factory.OnQueueChanged += UpdateQueueUI;
        factory.OnProductionProgressUpdated += UpdateProductionProgressUI;
        factory.OnProductionMessage += DisplayMessage;
        // factory.OnHealthChanged += UpdateBuildingStats; 

        UpdateQueueUI(_currentFactory.ProductionQueue.ToList(), _currentFactory.CurrentProduction);
        UpdateProductionProgressUI(_currentFactory.CurrentProductionTimer / (_currentFactory.CurrentProduction?.buildTime ?? 1f));

        Debug.Log($"UI для фабрики {factory.name} показан");
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

        ClearQueueUI();
        _currentProductionIcon.sprite = null; 
        _currentProductionIcon.enabled = false; 
        _productionProgressText.text = "";
        _currentProductionDisplay.SetActive(false);
        _messageText.text = "";
    }

    // обновления отображения HP
    // private void UpdateBuildingStats()
    // {
    //      if (_currentFactory != null)
    //      {
    //           _hpText.text = $"HP: {_currentFactory.CurrentHealthInt}";
    //      }
    // }

    public void OnUnitButtonClicked(int unitIndex) {
        if (_currentFactory != null) {
            bool success = _currentFactory.TryQueueUnitByIndex(unitIndex);
        }
    }

    public void OnCancelCurrentProduction() {
        if (_currentFactory != null)
            _currentFactory.CancelCurrentProduction();
    }

    private void UpdateQueueUI(List<EntityData> queueList, EntityData currentProduction) {
        ClearQueueUI();

        if (currentProduction != null && _currentProductionIcon != null) {
            if (currentProduction.icon != null) {
                _currentProductionIcon.sprite = currentProduction.icon;
                _currentProductionIcon.enabled = true;
            }
            else {
                _currentProductionIcon.enabled = false;
            }
            _currentProductionDisplay.SetActive(true);
        }
        else {
            if (_currentProductionIcon != null) _currentProductionIcon.enabled = false;
            _productionProgressText.text = "";
            _currentProductionDisplay.SetActive(false);
        }

        foreach (var unitInfo in queueList) {
            if (_queueItemPrefab != null && _queueContainer != null) {
                GameObject queueItemGO = Instantiate(_queueItemPrefab, _queueContainer);
                Image iconImage = queueItemGO.GetComponent<Image>();

                if (iconImage != null && unitInfo.icon != null) {
                    iconImage.sprite = unitInfo.icon;
                }
                else if (iconImage != null) {
                    iconImage.enabled = false;
                }
                _queueItemIcons.Add(iconImage);
            }
        }
    }

    private void ClearQueueUI() {
        foreach (var icon in _queueItemIcons) {
            if (icon != null && icon.gameObject != null) {
                Destroy(icon.gameObject);
            }
        }
        _queueItemIcons.Clear();
    }

    private void UpdateProductionProgressUI(float progress)
    {
        if (_productionProgressText != null) {
            if (_currentFactory != null && _currentFactory.CurrentProduction != null) {
                float remainingTime = _currentFactory.CurrentProduction.buildTime - _currentFactory.CurrentProductionTimer;
                _productionProgressText.text = $"{Mathf.CeilToInt(remainingTime)}s"; 
            }
            else {
                _productionProgressText.text = ""; 
            }
        }

        if (_currentProductionDisplay != null) {
            _currentProductionDisplay.SetActive(_currentFactory != null && _currentFactory.CurrentProduction != null);
        }
    }

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


    void OnDestroy() {
        if (_currentFactory != null) {
            _currentFactory.OnQueueChanged -= UpdateQueueUI;
            _currentFactory.OnProductionProgressUpdated -= UpdateProductionProgressUI;
            _currentFactory.OnProductionMessage -= DisplayMessage;
            // if (_currentFactory is Buildings building) building.OnHealthChanged -= UpdateBuildingStats;
        }
    }
}