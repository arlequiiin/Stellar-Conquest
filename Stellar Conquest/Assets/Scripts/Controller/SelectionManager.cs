using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SelectionManager : MonoBehaviour {
    // --- Singleton ---
    private static SelectionManager _instance;
    public static SelectionManager Instance { get { /* ... Singleton Get ... */ return _instance; } }

    // --- State ---
    [SerializeField] private List<Entity> _selectedEntities = new List<Entity>();
    public IReadOnlyList<Entity> SelectedEntities => _selectedEntities; // Доступ только для чтения извне

    // --- Dependencies ---
    private InputManager _inputManager;
    [SerializeField] private LayerMask _selectableLayerMask;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private LayerMask _enemyLayerMask; // Слой для вражеских юнитов/зданий

    // --- Drag Selection UI ---
    [SerializeField] private RectTransform _selectionBoxRect; // Ссылка на UI Image/Panel для рамки
    private Vector2 _startDragPos;

    void Awake() {
        // Ensure Singleton
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        // DontDestroyOnLoad(gameObject); // Если нужно

        if (_selectionBoxRect != null) _selectionBoxRect.gameObject.SetActive(false);
    }

    void Start() {
        _inputManager = InputManager.Instance;
        if (_inputManager == null) {
            Debug.LogError("SelectionManager'у необходим InputManager");
            enabled = false; 
            return;
        }

        // Подписываемся на события InputManager
        _inputManager.OnLeftClick += HandleLeftClick;
        _inputManager.OnShiftLeftClick += HandleShiftLeftClick;
        _inputManager.OnRightClick += HandleRightClick;
        _inputManager.OnCancelKeyPressed += HandleCancelKey;

        _inputManager.OnDragSelectStart += HandleDragStart;
        _inputManager.OnDragSelectUpdate += HandleDragUpdate;
        _inputManager.OnDragSelectEnd += HandleDragEnd;
    }

    void OnDestroy()
    {
        if (_inputManager != null) {
            _inputManager.OnLeftClick -= HandleLeftClick;
            _inputManager.OnShiftLeftClick -= HandleShiftLeftClick;
            _inputManager.OnRightClick -= HandleRightClick;
            _inputManager.OnCancelKeyPressed -= HandleCancelKey;
            _inputManager.OnDragSelectStart -= HandleDragStart;
            _inputManager.OnDragSelectUpdate -= HandleDragUpdate;
            _inputManager.OnDragSelectEnd -= HandleDragEnd;
        }
    }

    // --- Event Handlers ---

    private void HandleLeftClick(Vector3 clickPosition) {
        ClearSelection(); // Сначала очищаем предыдущее выделение

        // Если клик был не в пустоту
        if (clickPosition != Vector3.negativeInfinity) {
            // Пытаемся найти объект под курсором
            if (_inputManager.GetObjectUnderCursor<Entity>(out Entity clickedEntity, _selectableLayerMask)) {
                // Выделяем только если это юнит/здание локального игрока
                if (clickedEntity.OwnerPlayerId == GameManager.Instance?.LocalPlayerId) {
                    SelectEntity(clickedEntity);
                }
                // нужно выделять и вражеские объекты?
                // else { SelectEntity(clickedEntity, allowCommands: false); }
            }
        }
        UpdateSelectionUI(); // Обновить UI в любом случае (показать инфо или очистить)
    }

    private void HandleShiftLeftClick(Vector3 clickPosition) {
        // Не очищаем выделение!
        if (clickPosition != Vector3.negativeInfinity) {
            if (_inputManager.GetObjectUnderCursor<Entity>(out Entity clickedEntity, _selectableLayerMask)) {
                // Добавляем/удаляем только юнитов/здания локального игрока
                if (clickedEntity.OwnerPlayerId == GameManager.Instance?.LocalPlayerId) {
                    if (_selectedEntities.Contains(clickedEntity)) {
                        DeselectEntity(clickedEntity); // Убираем из выделения
                    }
                    else {
                        SelectEntity(clickedEntity, false); // Добавляем к выделению (не очищая старое)
                    }
                    UpdateSelectionUI();
                }
            }
        }
    }

    private void HandleRightClick(Vector3 clickPosition) {
        if (_selectedEntities.Count == 0) return; // Нет выделенных юнитов

        // 1. Проверяем, кликнули ли по врагу
        if (_inputManager.GetObjectUnderCursor<Entity>(out Entity targetEntity, _enemyLayerMask)) // Используем _enemyLayerMask
        {
            // Проверяем, что цель действительно враг
            if (targetEntity.OwnerPlayerId != GameManager.Instance?.LocalPlayerId && targetEntity.OwnerPlayerId != 0) // 0 - нейтральный ID?
            {
                Debug.Log($"Ordering selected units to attack {targetEntity.name}");
                IssueAttackCommand(targetEntity);
                return; // Приоритет на атаку
            }
        }

        // 2. Если не по врагу, проверяем, кликнули ли по земле
        if (_inputManager.GetGroundPointUnderCursor(out Vector3 groundPoint)) {
            Debug.Log($"Ordering selected units to move to {groundPoint}");
            IssueMoveCommand(groundPoint);
            return;
        }

        // 3. TODO: Клик по другим объектам (ресурсы, дружественные здания для ремонта и т.д.)?
    }

    private void HandleCancelKey() {
        if (_selectedEntities.Count > 0) {
            Debug.Log("Issuing Stop command to selected units.");
            foreach (var entity in _selectedEntities) {
                if (entity is Units unit) // Отменяем только для юнитов
                {
                    unit.StopActions();
                }
            }
        }
    }

    // --- Drag Selection Logic ---

    private void HandleDragStart(Vector2 screenPos) {
        if (_selectionBoxRect == null) return;
        _startDragPos = screenPos;
        _selectionBoxRect.gameObject.SetActive(true);
        UpdateSelectionBox(screenPos); // Инициализируем размер/позицию
    }

    private void HandleDragUpdate(Vector2 currentScreenPos, Vector2 startScreenPos) {
        if (_selectionBoxRect == null || !_selectionBoxRect.gameObject.activeSelf) return;
        UpdateSelectionBox(currentScreenPos);
    }

    private void HandleDragEnd(Vector2 endScreenPos, Vector2 startScreenPos) {
        if (_selectionBoxRect != null) _selectionBoxRect.gameObject.SetActive(false);

        ClearSelection(); // Очищаем перед выделением рамкой

        Rect selectionRect = GetSelectionRect(_startDragPos, endScreenPos);

        // Находим все выбираемые объекты в сцене
        // TODO: Оптимизировать! Не искать все объекты каждый раз. Использовать Quadtree или Physics.OverlapBox?
        Entity[] allSelectables = FindObjectsOfType<Entity>();

        foreach (var entity in allSelectables) {
            // Проверяем, принадлежит ли локальному игроку и находится ли в рамке
            if (entity.OwnerPlayerId == GameManager.Instance?.LocalPlayerId) {
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(entity.transform.position);
                // Проверяем, что объект перед камерой и внутри прямоугольника
                if (screenPoint.z > 0 && selectionRect.Contains(screenPoint)) {
                    SelectEntity(entity, false); // Добавляем без очистки
                }
            }
        }
        Debug.Log($"Drag selection finished. Selected {_selectedEntities.Count} entities.");
        UpdateSelectionUI();
    }

    // --- Helper Methods ---

    private void SelectEntity(Entity entity, bool clearPrevious = true) {
        if (entity == null) return;

        if (clearPrevious) {
            ClearSelection();
        }

        if (!_selectedEntities.Contains(entity)) {
            _selectedEntities.Add(entity);
            entity.Select(); // Вызываем метод выделения у самого объекта
                             // Debug.Log($"Selected: {entity.gameObject.name}");
        }
    }

    private void DeselectEntity(Entity entity) {
        if (entity == null) return;

        if (_selectedEntities.Contains(entity)) {
            entity.Deselect(); // Снимаем выделение у объекта
            _selectedEntities.Remove(entity);
            // Debug.Log($"Deselected: {entity.gameObject.name}");
        }
    }

    private void ClearSelection() {
        // Снимаем выделение со всех ранее выделенных
        foreach (var entity in _selectedEntities) {
            if (entity != null) // Проверка на случай, если объект был уничтожен пока был выделен
            {
                entity.Deselect();
            }
        }
        _selectedEntities.Clear();
        // Debug.Log("Selection Cleared.");
    }

    private void IssueMoveCommand(Vector3 destination) {
        // TODO: Формации движения? Пока просто отправляем всех в одну точку
        foreach (var entity in _selectedEntities) {
            if (entity is Units unit) {
                unit.MoveTo(destination);
            }
        }
    }

    private void IssueAttackCommand(Entity target) {
        foreach (var entity in _selectedEntities) {
            if (entity is Units unit) {
                unit.OrderAttackTarget(target);
            }
        }
    }

    // Обновление UI, связанного с выделением (панель информации и т.д.)
    private void UpdateSelectionUI() {
        Debug.Log($"Selection updated. Count: {_selectedEntities.Count}");
        // TODO: Здесь код для обновления UI:
        // - Если выделен 1 объект: показать его панель информации (иконка, здоровье, кнопки действий)
        // - Если выделено несколько: показать сетку иконок, общее кол-во, возможно, панель только для общих действий
        // - Если выделение снято: скрыть панель информации
    }

    // --- UI Drag Box ---
    private void UpdateSelectionBox(Vector2 currentMousePos) {
        if (!_selectionBoxRect.gameObject.activeSelf) return;

        float width = currentMousePos.x - _startDragPos.x;
        float height = currentMousePos.y - _startDragPos.y;

        _selectionBoxRect.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        _selectionBoxRect.anchoredPosition = _startDragPos + new Vector2(width / 2, height / 2);
    }

    private Rect GetSelectionRect(Vector2 startPos, Vector2 endPos) {
        // Нормализуем координаты, чтобы xMin/yMin всегда были меньше xMax/yMax
        float xMin = Mathf.Min(startPos.x, endPos.x);
        float xMax = Mathf.Max(startPos.x, endPos.x);
        float yMin = Mathf.Min(startPos.y, endPos.y);
        float yMax = Mathf.Max(startPos.y, endPos.y);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

}