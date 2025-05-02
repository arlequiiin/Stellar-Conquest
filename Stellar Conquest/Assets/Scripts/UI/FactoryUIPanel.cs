using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using System.Collections.Generic;
using System.Linq; 

public class FactoryUIPanel : MonoBehaviour {
    [Header("UI Elements")]
    [SerializeField] private GameObject _uiPanelGameObject;
    [SerializeField] private TextMeshProUGUI _buildingNameText;
    [SerializeField] private TextMeshProUGUI _hpText;

    [Header("Unit Buttons")]
    [SerializeField] private Button[] _unitButtons; // Массив ваших 4 кнопок
    [SerializeField] private TextMeshProUGUI[] _unitButtonTexts; // Массив текстов для кнопок (имя/стоимость)
    // Возможно, вам понадобятся ссылки на Image, если они не на самой кнопке, а внутри

    [Header("Production Queue")]
    [SerializeField] private Transform _queueContainer; // Контейнер для иконок в очереди
    [SerializeField] private GameObject _queueItemPrefab; // Префаб для одной иконки юнита в очереди

    [Header("Current Production")]
    [SerializeField] private Image _currentProductionIcon; // Иконка текущего юнита
    [SerializeField] private TextMeshProUGUI _productionProgressText; // Текст для прогресса (например, % или время) - ОПЦИОНАЛЬНО
    [SerializeField] private GameObject _currentProductionDisplay; // Объект, который показывает текущее производство (иконка + бар) - можно скрыть, если ничего не строится

    [Header("Messages")]
    [SerializeField] private TextMeshProUGUI _messageText; // Для отображения сообщений (нет ресурсов, очередь полна)
    [SerializeField] private float _messageDisplayTime = 3f; // Время показа сообщения

    private Factory _currentFactory; // Ссылка на фабрику, UI которой сейчас отображается
    private List<Image> _queueItemIcons = new List<Image>(); // Список созданных иконок в очереди
    private Coroutine _messageCoroutine; // Для управления временем показа сообщения

    private void Awake() {
        _uiPanelGameObject.SetActive(false);

        // Можно здесь добавить слушателей на кнопки, если не делаете это через инспектор
        // for (int i = 0; i < _unitButtons.Length; i++)
        // {
        //     int unitIndex = i; // Сохраняем индекс для замыкания
        //     _unitButtons[i].onClick.AddListener(() => OnUnitButtonClicked(unitIndex));
        // }
    }

    void Start() {
        // Подписываемся на события выделения/снятия выделения зданий (нужен менеджер выделения или аналогичный класс)
        // Этот шаг требует централизованного менеджера выделения или других зданий, которые оповещают о своем выделении.
        // Простой вариант: Найти все фабрики и подписаться. Неэффективно для большого кол-ва зданий.
        // Лучший вариант: Единый SelectionManager, который обрабатывает клики и вызывает Select/Deselect на объектах.
        // Допустим, у нас есть SelectionManager.Instance
        // SelectionManager.Instance.OnEntitySelected += OnEntitySelected;
        // SelectionManager.Instance.OnEntityDeselected += OnEntityDeselected;

        // Пока для примера, подпишемся на все фабрики в сцене (для тестирования)
        // Это **НЕ** рекомендуемый подход для финальной игры
        // FindObjectsOfType<Factory>().ToList().ForEach(factory =>
        // {
        //    factory.OnFactorySelected += Show;
        //    factory.OnFactoryDeselected += Hide;
        // });

        // Лучше всего, когда Factory сам вызывает не глобальный Show/Hide,
        // а оповещает SelectionManager, а SelectionManager решает, какой UI показать.

        // **Самый простой вариант для начала:** Сделать Show/Hide публичными и вызывать их из метода Select/Deselect в Factory,
        // передавая ссылку на себя. Для этого UI Panel должен быть легко доступен (например, найден по тегу или ссылке из SelectionManager).
    }

    // Вызывается, когда любую сущность выбрали (для SelectionManager)
    // private void OnEntitySelected(Entity entity)
    // {
    //     if (entity is Factory factory) // Если выбранная сущность - фабрика
    //     {
    //         Show(factory);
    //     }
    //     else
    //     {
    //         Hide(); // Скрыть UI фабрики, если выбрано что-то другое
    //     }
    // }

    // Вызывается, когда любую сущность сняли с выделения
    // private void OnEntityDeselected(Entity entity)
    // {
    //     if (entity == _currentFactory) // Если сняли с выделения текущую фабрику
    //     {
    //         Hide();
    //     }
    // }


    // !!! Публичный метод для показа панели !!!
    public void Show(Factory factory) {
        if (_currentFactory != null) {
            // Если уже показываем UI другой фабрики, сначала скрываем его
            // Это может случиться, если SelectionManager переключает выделение быстро
            Hide();
        }

        _currentFactory = factory;
        _uiPanelGameObject.SetActive(true);

        // Обновляем основную информацию о здании
        _buildingNameText.text = factory.gameObject.name;
        // Обновляем HP. Пока просто число. Можно добавить MaxHP если нужно
        _hpText.text = $"HP: {factory.CurrentHealthInt}";

        // Обновляем кнопки юнитов
        SetupUnitButtons(factory.ProducibleUnits);

        // Подписываемся на события конкретной выбранной фабрики
        factory.OnQueueChanged += UpdateQueueUI;
        factory.OnProductionProgressUpdated += UpdateProductionProgressUI;
        factory.OnProductionMessage += DisplayMessage;
        // Если нужно обновлять HP в реальном времени при получении урона,
        // понадобится событие OnHealthChanged в Entity или Building
        // factory.OnHealthChanged += UpdateBuildingStats; // Предполагаем такой метод и событие

        // Первичное обновление UI очереди и прогресса
        UpdateQueueUI(_currentFactory.ProductionQueue.ToList(), _currentFactory.CurrentProduction);
        UpdateProductionProgressUI(_currentFactory.CurrentProductionTimer / (_currentFactory.CurrentProduction?.ProductionTime ?? 1f)); // Обработка деления на ноль

        Debug.Log($"UI для фабрики {factory.name} показан.");
    }

    // !!! Публичный метод для скрытия панели !!!
    public void Hide() {
        if (_currentFactory != null) {
            // Отписываемся от событий текущей фабрики перед скрытием
            _currentFactory.OnQueueChanged -= UpdateQueueUI;
            _currentFactory.OnProductionProgressUpdated -= UpdateProductionProgressUI;
            _currentFactory.OnProductionMessage -= DisplayMessage;
            // if (_currentFactory is Buildings building) building.OnHealthChanged -= UpdateBuildingStats;

            _currentFactory = null;
        }

        _uiPanelGameObject.SetActive(false);
        // Очищаем UI (очередь, прогресс)
        ClearQueueUI();
        _currentProductionIcon.sprite = null; // Сбросить иконку
        _currentProductionIcon.enabled = false; // Скрыть иконку
        _productionProgressText.text = "";
        _currentProductionDisplay.SetActive(false); // Скрыть блок текущего производства
        _messageText.text = ""; // Очистить сообщения

        Debug.Log("UI фабрики скрыт.");
    }

    // Метод для обновления отображения HP и других статов здания
    // private void UpdateBuildingStats()
    // {
    //      if (_currentFactory != null)
    //      {
    //           _hpText.text = $"HP: {_currentFactory.CurrentHealthInt}";
    //           // Обновить другие статы, если есть
    //      }
    // }


    // Настраиваем кнопки юнитов на основе данных из фабрики
    private void SetupUnitButtons(List<Factory.UnitProductionInfo> producibleUnits) {
        // Предполагаем, что у вас есть ровно 4 кнопки и ровно 4 производимых юнита
        // или юниты в списке _producibleUnits соответствуют порядку кнопок
        if (_unitButtons.Length != producibleUnits.Count) {
            Debug.LogError("Количество кнопок юнитов не совпадает с количеством производимых юнитов в фабрике!");
            // Здесь можно как-то обработать или отключить UI
        }

        for (int i = 0; i < _unitButtons.Length; i++) {
            if (i < producibleUnits.Count) {
                Factory.UnitProductionInfo unitInfo = producibleUnits[i];

                // Настраиваем визуал кнопки
                if (_unitButtons[i].image != null && unitInfo.UnitIcon != null) {
                    _unitButtons[i].image.sprite = unitInfo.UnitIcon;
                    _unitButtons[i].image.enabled = true;
                }
                else if (_unitButtons[i].image != null) {
                    _unitButtons[i].image.enabled = false; // Скрыть Image если нет иконки
                }


                if (_unitButtonTexts.Length > i && _unitButtonTexts[i] != null) {
                    // Отображаем название и стоимость
                    _unitButtonTexts[i].text = $"{unitInfo.UnitName}\nCost: {unitInfo.ResourceCost}";
                    // TODO: Использовать иконки ресурсов вместо текста "Cost:"
                }

                // Привязываем клик кнопки к методу производства в текущей фабрике
                // Сначала очищаем все предыдущие слушатели, чтобы избежать двойных вызовов
                _unitButtons[i].onClick.RemoveAllListeners();
                int unitIndex = i; // Сохраняем индекс для передачи в замыкание
                _unitButtons[i].onClick.AddListener(() => OnUnitButtonClicked(unitIndex));

                _unitButtons[i].interactable = true; // Изначально кнопки активны (проверка ресурсов будет позже)

            }
            else {
                // Если кнопок больше, чем юнитов, скрываем или отключаем лишние кнопки
                _unitButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // Метод, вызываемый при клике на любую из кнопок юнитов
    public void OnUnitButtonClicked(int unitIndex) {
        if (_currentFactory != null) {
            // Пытаемся поставить юнита в очередь через фабрику
            bool success = _currentFactory.TryQueueUnitByIndex(unitIndex);
            // Фабрика сама вызовет OnQueueChanged и OnProductionMessage
        }
    }

    // Метод для обновления визуального отображения очереди
    private void UpdateQueueUI(List<Factory.UnitProductionInfo> queueList, Factory.UnitProductionInfo currentProduction) {
        ClearQueueUI(); // Очищаем текущие иконки

        // Добавляем иконку текущего производимого юнита, если он есть
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
            // Ничего не строится
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

    // Очищает визуальное отображение очереди
    private void ClearQueueUI() {
        foreach (var icon in _queueItemIcons) {
            if (icon != null && icon.gameObject != null) {
                Destroy(icon.gameObject);
            }
        }
        _queueItemIcons.Clear();
    }

    // Метод для обновления прогресс бара
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

    // TODO: Метод для обработки клика по иконке в очереди для отмены (потребует изменения Prefab очереди)
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