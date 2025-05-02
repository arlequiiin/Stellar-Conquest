using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SelectionManager : MonoBehaviour {
    private static SelectionManager _instance;
    public static SelectionManager Instance { get { return _instance; } }

    [SerializeField] private List<Entity> _selectedEntities = new List<Entity>();
    public IReadOnlyList<Entity> SelectedEntities => _selectedEntities; 

    private InputManager _inputManager;
    [SerializeField] private LayerMask _selectableLayerMask;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private LayerMask _enemyLayerMask; 

    [SerializeField] private RectTransform _selectionBoxRect; 
    private Vector2 _startDragPos;

    [SerializeField] private FactoryUIPanel _factoryUIPanel;

    void Awake() {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        // DontDestroyOnLoad(gameObject); 

        if (_selectionBoxRect != null) _selectionBoxRect.gameObject.SetActive(false);
    }

    void Start() {
        _inputManager = InputManager.Instance;
        if (_inputManager == null) {
            Debug.LogError("SelectionManager'у необходим InputManager");
            enabled = false; 
            return;
        }

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

    private void HandleLeftClick(Vector3 clickPosition) {
        bool clickedFriendlySelectable = false;
        Entity clickedEntity = null;

        if (clickPosition != Vector3.negativeInfinity) {

            if (_inputManager.GetObjectUnderCursor<Entity>(out clickedEntity, _selectableLayerMask)) {

                if (clickedEntity.OwnerPlayerId == GameManager.Instance?.LocalPlayerId) {
                    ClearSelection(); 
                    SelectEntity(clickedEntity);
                    clickedFriendlySelectable = true;
                    Debug.Log($"Выбран свой объект: {clickedEntity.name}");
                }
                else {
                    Debug.Log($"Клик по чужому/нейтральному объекту: {clickedEntity.name}");
                }
            }
            else {
                Debug.Log("Клик по земле или другому невыделяемому объекту");
            }
        }
        else {
            Debug.Log("Клик в пустоту");
        }


        if (!clickedFriendlySelectable) {
            ClearSelection(); 
        }
        UpdateSelectionUI(); 
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
        if (_selectedEntities.Count == 0) 
            return;

        Entity targetEntity = null;
        Vector3 groundPoint = Vector3.zero;
        bool hitEnemy = false;
        bool hitGround = false;
        LayerMask enemyMask = _enemyLayerMask; 

        if (_inputManager.GetObjectUnderCursor<Entity>(out targetEntity, enemyMask)) // Используем _enemyLayerMask
        {
            if (targetEntity.OwnerPlayerId != GameManager.Instance?.LocalPlayerId && targetEntity.OwnerPlayerId != 0) 
            {
                Debug.Log($"Приказ атаковать: {targetEntity.name}");
                IssueAttackCommand(targetEntity);
                hitEnemy = true;
            }
        }

        if (_inputManager.GetGroundPointUnderCursor(out groundPoint)) {
            Debug.Log($"Приказ двигаться в точку: {groundPoint}");
            IssueMoveCommand(groundPoint); // Этот метод вызывает unit.MoveTo()
            hitGround = true;
        }

        if (!hitEnemy && !hitGround) {
            Debug.Log("ПКМ: Недопустимая цель для команды.");
        }

        //клик по другим объектам ?
    }

    private void HandleCancelKey() {
        if (_selectedEntities.Count > 0) {
            Debug.Log("Команда стоп для юнитов");
            foreach (var entity in _selectedEntities) {
                if (entity is Units unit) // Отменяем только для юнитов
                {
                    unit.StopActions();
                }
            }
        }
        ClearSelection();
        UpdateSelectionUI();
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
        Vector3 worldStart = Camera.main.ScreenToWorldPoint(_startDragPos);
        Vector3 worldEnd = Camera.main.ScreenToWorldPoint(endScreenPos);
        Vector2 point1 = new Vector2(Mathf.Min(worldStart.x, worldEnd.x), Mathf.Min(worldStart.y, worldEnd.y));
        Vector2 point2 = new Vector2(Mathf.Max(worldStart.x, worldEnd.x), Mathf.Max(worldStart.y, worldEnd.y));

        Collider2D[] hitColliders = Physics2D.OverlapAreaAll(point1, point2, _selectableLayerMask); 

        foreach (var hitCollider in hitColliders) {
            Entity entity = hitCollider.GetComponent<Entity>();
            if (entity.OwnerPlayerId == GameManager.Instance?.LocalPlayerId) {
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(entity.transform.position);
                if (entity != null && entity.OwnerPlayerId == GameManager.Instance?.LocalPlayerId && entity.IsAlive) {
                    SelectEntity(entity, false);
                }
            }
        }
        Debug.Log($"Drag selection finished. Selected {_selectedEntities.Count} entities.");
        UpdateSelectionUI();
    }

    // --- Helper Methods ---

    private void SelectEntity(Entity entity, bool clearPrevious = true) {
        if (entity == null || !entity.IsAlive) return; 

        if (clearPrevious) {
            ClearSelection();
        }

        if (!_selectedEntities.Contains(entity)) {
            _selectedEntities.Add(entity);
            entity.Select(); 
            Debug.Log($"Selected: {entity.gameObject.name}");
        }
    }

    private void DeselectEntity(Entity entity) {
        if (entity == null) return;

        if (_selectedEntities.Contains(entity)) {
            entity.Deselect(); // Снимаем выделение у объекта
            _selectedEntities.Remove(entity);
            Debug.Log($"Deselected: {entity.gameObject.name}");
        }
    }

    private void ClearSelection() {
        List<Entity> entitiesToDeselect = new List<Entity>(_selectedEntities);

        foreach (var entity in entitiesToDeselect) {
            if (entity != null) // Проверка на случай, если объект был уничтожен пока был выделен
            {
                entity.Deselect();
            }
        }
        _selectedEntities.Clear();
        Debug.Log("Выделение очищено");
    }

    private void IssueMoveCommand(Vector3 destination) {
        // пока просто отправляем всех в одну точку
        foreach (var entity in _selectedEntities) {
            if (entity is Units unit && unit.IsAlive) {
                unit.MoveTo(destination);
            }
        }
    }

    private void IssueAttackCommand(Entity target) {
        if (target == null || !target.IsAlive) 
            return; 

        foreach (var entity in _selectedEntities) {
            if (entity is Units unit && unit.IsAlive) {
                unit.OrderAttackTarget(target);
            }
        }
        // маркер атаки?
    }

    private void UpdateSelectionUI() {
        if (_selectedEntities.Count == 1) {
            Entity selectedEntity = _selectedEntities[0];

            if (selectedEntity is Factory selectedFactory) 
            {
                if (_factoryUIPanel != null) {
                    _factoryUIPanel.Show(selectedFactory); 
                }
                // TODO: Скрыть другие UI панели (например, для юнитов)
            }
            else {
                if (_factoryUIPanel != null) _factoryUIPanel.Hide();
                // TODO: Показать UI для юнита или другого типа здания
            }
        }
        else
        {
            if (_factoryUIPanel != null) _factoryUIPanel.Hide(); 
            // TODO: Показать UI для мульти-выделения или скрыть все UI панели
        }
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