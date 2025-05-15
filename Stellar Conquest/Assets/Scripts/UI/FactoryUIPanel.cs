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

        // SetupUnitButtons(factory.ProducibleUnits);

        factory.OnQueueChanged += UpdateQueueUI;
        factory.OnProductionProgressUpdated += UpdateProductionProgressUI;
        factory.OnProductionMessage += DisplayMessage;
        // factory.OnHealthChanged += UpdateBuildingStats; 

        UpdateQueueUI(_currentFactory.ProductionQueue.ToList(), _currentFactory.CurrentProduction);
        UpdateProductionProgressUI(_currentFactory.CurrentProductionTimer / (_currentFactory.CurrentProduction?.ProductionTime ?? 1f)); // Обработка деления на ноль

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

    private void UpdateQueueUI(List<Factory.UnitProductionInfo> queueList, Factory.UnitProductionInfo currentProduction) {
        ClearQueueUI(); 

        if (currentProduction != null) {
            if (_currentProductionIcon != null && currentProduction.UnitIcon != null) {
                _currentProductionIcon.sprite = currentProduction.UnitIcon;
                _currentProductionIcon.enabled = true;
                _currentProductionDisplay.SetActive(true);
            }
            else if (_currentProductionIcon != null) {
                _currentProductionIcon.enabled = false; // Скрыть иконку если нет спрайта
                _currentProductionDisplay.SetActive(true); // Но бар и текст могут быть видны
            }
            // Прогресс бар и текст будут обновляться через UpdateProductionProgressUI
        }
        else {
            if (_currentProductionIcon != null) _currentProductionIcon.enabled = false;
            _productionProgressText.text = "";
            _currentProductionDisplay.SetActive(false); // Скрыть блок текущего производства
        }

        // Добавляем иконки юнитов из очереди
        foreach (var unitInfo in queueList) {
            if (_queueItemPrefab != null && _queueContainer != null) {
                GameObject queueItemGO = Instantiate(_queueItemPrefab, _queueContainer);
                Image iconImage = queueItemGO.GetComponent<Image>(); // Префаб должен иметь Image компонент

                if (iconImage != null && unitInfo.UnitIcon != null) {
                    iconImage.sprite = unitInfo.UnitIcon;
                }
                else if (iconImage != null) {
                    iconImage.enabled = false; // Скрыть Image если нет спрайта
                }
                _queueItemIcons.Add(iconImage); // Сохраняем ссылку на созданную иконку

                // TODO: Возможность отмены по клику на иконку в очереди
                // Для этого Prefab элемента очереди должен быть кнопкой или иметь скрипт с кнопкой/обработчиком клика
                // и передавать свой индекс в очереди для отмены.
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

    private void UpdateProductionProgressUI(float progress) // progress от 0 до 1
    {
        if (_productionProgressText != null) {
            // Можно отобразить процент или оставшееся время
            if (_currentFactory != null && _currentFactory.CurrentProduction != null) {
                float remainingTime = _currentFactory.CurrentProduction.ProductionTime - _currentFactory.CurrentProductionTimer;
                _productionProgressText.text = $"{Mathf.CeilToInt(remainingTime)}s"; // Например, округленное оставшееся время
                                                                                     // _productionProgressText.text = $"{Mathf.RoundToInt(progress * 100)}%"; // Или процент
            }
            else {
                _productionProgressText.text = ""; // Очистить текст если ничего не строится
            }
        }

        // Показываем/скрываем блок текущего производства в зависимости от того, строится ли что-то
        if (_currentProductionDisplay != null) {
            _currentProductionDisplay.SetActive(_currentFactory != null && _currentFactory.CurrentProduction != null);
        }
    }

    // Метод для отображения сообщений (например, "Нет ресурсов")
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

    // TODO: Метод для кнопки отмены текущего производства (вызывает _currentFactory.CancelProduction(0))
    // TODO: Метод для кнопки установки точки сбора (_currentFactory._rallyPoint = ...)

    void OnDestroy() {
        // Отписываемся от событий, если скрипт UI уничтожается раньше фабрики
        if (_currentFactory != null) {
            _currentFactory.OnQueueChanged -= UpdateQueueUI;
            _currentFactory.OnProductionProgressUpdated -= UpdateProductionProgressUI;
            _currentFactory.OnProductionMessage -= DisplayMessage;
            // if (_currentFactory is Buildings building) building.OnHealthChanged -= UpdateBuildingStats;
        }
    }
}