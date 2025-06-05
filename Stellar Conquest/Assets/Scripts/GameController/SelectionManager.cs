using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectionManager : MonoBehaviour {
    private static SelectionManager _instance;
    public static SelectionManager Instance { get { return _instance; } }

    [SerializeField] private List<Entity> _selectedEntities = new List<Entity>();
    public IReadOnlyList<Entity> SelectedEntities => _selectedEntities; 

    [SerializeField] private LayerMask _selectableLayerMask;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private LayerMask _enemyLayerMask; 
    [SerializeField] private RectTransform _selectionBoxRect; 
    [SerializeField] private FactoryUIPanel factoryUIPanel;
    [SerializeField] private SelectionUIPanel selectionUIPanel;
    [SerializeField] private OrdersUIPanel ordersUIPanel;
    private Entity _currentEntity;
    private InputManager _inputManager;
    private Vector2 _startDragPos;
    public enum OrderMode {
        None,
        Move,
        Build
    }

    public OrderMode CurrentOrderMode { get; private set; } = OrderMode.None;

    void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start() {
        InitializeInputManager();
    }

    private void InitializeInputManager() {
        _inputManager = InputManager.Instance;
        if (_inputManager == null) {
            Debug.LogError("SelectionManager'у необходим InputManager");
            enabled = false;
            return;
        }

        // Отписываемся от старых событий (если были)
        UnsubscribeFromInputEvents();

        // Подписываемся на новые события
        _inputManager.OnLeftClick += HandleLeftClick;
        _inputManager.OnShiftLeftClick += HandleShiftLeftClick;
        _inputManager.OnRightClick += HandleRightClick;
        _inputManager.OnCancelKeyPressed += HandleCancelKey;

        _inputManager.OnDragSelectStart += HandleDragStart;
        _inputManager.OnDragSelectUpdate += HandleDragUpdate;
        _inputManager.OnDragSelectEnd += HandleDragEnd;
    }

    private void UnsubscribeFromInputEvents() {
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

    void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_instance == this) _instance = null;
        UnsubscribeFromInputEvents();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        ClearSelection();

        StartCoroutine(ReinitializeAfterFrame());
    }
    private System.Collections.IEnumerator ReinitializeAfterFrame() {
        yield return null;

        EnsureUIPanels();
        _selectionBoxRect = GameObject.FindWithTag("SelectionBox")?.GetComponent<RectTransform>();

        if (_selectionBoxRect != null) {
            _selectionBoxRect.gameObject.SetActive(false);
        }

        InitializeInputManager();
    }

    private void HandleLeftClick(Vector3 clickPosition) {
        bool clickedFriendlySelectable = false;
        Entity clickedEntity = null;

        if (clickPosition != Vector3.negativeInfinity) {

            if (_inputManager.GetObjectUnderCursor<Entity>(out clickedEntity, _selectableLayerMask)) {

                if (clickedEntity.OwnerPlayerId == GameManager.Instance.playerId) {
                    ClearSelection(); 
                    SelectEntity(clickedEntity);
                    clickedFriendlySelectable = true;
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
        Debug.Log("Shift");
        if (clickPosition != Vector3.negativeInfinity) {
            if (_inputManager.GetObjectUnderCursor<Entity>(out Entity clickedEntity, _selectableLayerMask)) {
                if (clickedEntity.OwnerPlayerId == GameManager.Instance.playerId) {
                    if (_selectedEntities.Contains(clickedEntity)) {
                        DeselectEntity(clickedEntity);
                    }
                    else {
                        SelectEntity(clickedEntity, false); 
                    }
                    UpdateSelectionUI();
                }
            }
        }
    }

    private void HandleRightClick(Vector3 clickPosition) {
        if (_selectedEntities.Count == 0) 
            return;

        if (CurrentOrderMode == OrderMode.Move) {
            IssueMoveCommand(clickPosition);
            ResetOrderMode();
            return;
        }

        Entity targetEntity = null;

        if (CurrentOrderMode == OrderMode.Build) {
            if (_inputManager.GetObjectUnderCursor<Entity>(out targetEntity, _selectableLayerMask)) {
                if (targetEntity is Buildings building && !building.IsCompleted) {
                    var engineers = _selectedEntities.OfType<Engineer>().ToList();
                    if (engineers.Count > 0) {
                        var nearest = engineers.OrderBy(e => Vector3.Distance(e.transform.position, building.transform.position)).First();
                        nearest.StartBuild(building);
                        ResetOrderMode();
                        return;
                    }
                }
            }
            ResetOrderMode();
            return;
        }

        Vector3 groundPoint = Vector3.zero;
        bool hitEnemy = false;
        bool hitGround = false;
        LayerMask enemyMask = _enemyLayerMask; 

        if (_inputManager.GetObjectUnderCursor<Entity>(out targetEntity, enemyMask))
        {
            if (targetEntity.OwnerPlayerId != GameManager.Instance.playerId && targetEntity.OwnerPlayerId != 0) 
            {
                Debug.Log($"Приказ атаковать: {targetEntity.name}");
                IssueAttackCommand(targetEntity);
                hitEnemy = true;
            }
        }

        if (_inputManager.GetGroundPointUnderCursor(out groundPoint)) {
            Debug.Log($"Приказ двигаться в точку: {groundPoint}");
            IssueMoveCommand(groundPoint);
            hitGround = true;
        }

        if (!hitEnemy && !hitGround) {
            Debug.Log("ПКМ: Недопустимая цель для команды.");
        }

        // клик по другим объектам ?
    }

    private void HandleCancelKey() {
        if (_selectedEntities.Count > 0) {
            Debug.Log("Команда стоп для юнитов");
            foreach (var entity in _selectedEntities) {
                if (entity is Units unit)
                {
                    unit.StopActions();
                }
            }
        }
        ClearSelection();
        UpdateSelectionUI();
    }


    private void HandleDragStart(Vector2 screenPos) {
        if (_selectionBoxRect == null) return;
        _startDragPos = screenPos;
        _selectionBoxRect.gameObject.SetActive(true);
        UpdateSelectionBox(screenPos);
    }

    private void HandleDragUpdate(Vector2 currentScreenPos, Vector2 startScreenPos) {
        if (_selectionBoxRect == null || !_selectionBoxRect.gameObject.activeSelf) return;
        UpdateSelectionBox(currentScreenPos);
    }

    private void HandleDragEnd(Vector2 endScreenPos, Vector2 startScreenPos) {
        if (_selectionBoxRect != null) _selectionBoxRect.gameObject.SetActive(false);

        ClearSelection();

        Rect selectionRect = GetSelectionRect(_startDragPos, endScreenPos);

        Vector3 worldStart = Camera.main.ScreenToWorldPoint(_startDragPos);
        Vector3 worldEnd = Camera.main.ScreenToWorldPoint(endScreenPos);
        Vector2 point1 = new Vector2(Mathf.Min(worldStart.x, worldEnd.x), Mathf.Min(worldStart.y, worldEnd.y));
        Vector2 point2 = new Vector2(Mathf.Max(worldStart.x, worldEnd.x), Mathf.Max(worldStart.y, worldEnd.y));

        Collider2D[] hitColliders = Physics2D.OverlapAreaAll(point1, point2, _selectableLayerMask); 

        foreach (var hitCollider in hitColliders) {
            Entity entity = hitCollider.GetComponent<Entity>();
            if (entity.OwnerPlayerId == GameManager.Instance.playerId) {
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(entity.transform.position);
                if (entity != null && entity.OwnerPlayerId == GameManager.Instance.playerId && entity.IsAlive) {
                    SelectEntity(entity, false);
                }
            }
        }
        Debug.Log($"Перетаскиванием выделено {_selectedEntities.Count} существ");
        UpdateSelectionUI();
    }

    private void SelectEntity(Entity entity, bool clearPrevious = true) {
        if (entity == null || !entity.IsAlive) return;

        if (clearPrevious) {
            if (_currentEntity != null)
                _currentEntity.OnHealthChanged -= OnEntityHealthChanged;
            ClearSelection();
        }

        if (!_selectedEntities.Contains(entity)) {
            _selectedEntities.Add(entity);
            entity.Select();
            _currentEntity = entity;

            // Подписываемся на событие изменения ХП
            entity.OnHealthChanged += OnEntityHealthChanged;

            selectionUIPanel.UpdateEntityInfo(entity); // или UpdateEntityInfo(entity)
        }
    }
    private void OnEntityHealthChanged(float current, float max) {
        if (selectionUIPanel != null) 
            selectionUIPanel.UpdateHealth(current, max);
    }

    private void DeselectEntity(Entity entity) {
        if (entity == null) return;

        if (_selectedEntities.Contains(entity)) {
            entity.Deselect();
            _selectedEntities.Remove(entity);
        }
    }

    private void ClearSelection() {
        List<Entity> entitiesToDeselect = new List<Entity>(_selectedEntities);

        foreach (var entity in entitiesToDeselect) {
            if (entity != null) 
            {
                entity.Deselect();
            }
        }
        _selectedEntities.Clear();
        if (selectionUIPanel != null)   
            selectionUIPanel.Hide();
    }

    private void IssueMoveCommand(Vector3 destination) {
        int count = _selectedEntities.Count;
        float radius = 0.5f + count * 0.1f; // подобрать по размеру юнитов

        for (int i = 0; i < count; i++) {
            if (_selectedEntities[i] is Units unit && unit.IsAlive) {
                float angle = 2 * Mathf.PI * i / count;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                Vector3 unitTarget = destination + offset;

                unit.MoveTo(unitTarget);
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
        if (!EnsureUIPanels()) return;

        if (_selectedEntities.Count == 1) {
            Entity selectedEntity = _selectedEntities[0];
            selectionUIPanel.UpdateEntityInfo(selectedEntity);
            ordersUIPanel.Show(_selectedEntities[0]);

            if (selectedEntity is Factory selectedFactory) 
            {
                if (factoryUIPanel != null) { 
                    factoryUIPanel.Show(selectedFactory); 
                }
                // Скрыть другие UI панели
            }
            else {
                if (factoryUIPanel != null) factoryUIPanel.Hide();           
                // показать UI для юнита или другого типа здания
            }
        }
        else if (_selectedEntities.Count > 1) {
            if (selectionUIPanel != null) selectionUIPanel.Hide();
            if (factoryUIPanel != null) factoryUIPanel.Hide();

            if (ordersUIPanel)  
                ordersUIPanel.Show();
        }
        else
        {
            if (factoryUIPanel != null) factoryUIPanel.Hide();
            if (selectionUIPanel != null) selectionUIPanel.Hide();
            if (ordersUIPanel != null) ordersUIPanel.Hide();
            // скрыть все UI панели
        }
    }
    private bool EnsureUIPanels() {
        if (selectionUIPanel == null)
            selectionUIPanel = Object.FindFirstObjectByType<SelectionUIPanel>(FindObjectsInactive.Include);
        if (ordersUIPanel == null)
            ordersUIPanel = Object.FindFirstObjectByType<OrdersUIPanel>(FindObjectsInactive.Include);
        if (factoryUIPanel == null)
            factoryUIPanel = Object.FindFirstObjectByType<FactoryUIPanel>(FindObjectsInactive.Include);
        if (_selectionBoxRect == null)
            _selectionBoxRect = GameObject.FindWithTag("SelectionBox")?.GetComponent<RectTransform>();
        return selectionUIPanel != null;
    }

    public void SetOrderMode(OrderMode mode) {
        CurrentOrderMode = mode;
        //  показать подсказку в UI?
        if (ordersUIPanel != null)
            ordersUIPanel.SetMessage(mode == OrderMode.Move ? "Укажите точку для перемещения" : "");
    }

    public void ResetOrderMode() {
        CurrentOrderMode = OrderMode.None;
        if (ordersUIPanel != null) 
            ordersUIPanel.ClearMessage();
    }


    // прямоугольник выделения
    private void UpdateSelectionBox(Vector2 currentMousePos) {
        if (!_selectionBoxRect.gameObject.activeSelf) return;

        Canvas canvas = _selectionBoxRect.GetComponentInParent<Canvas>();

        RectTransform canvasRectTransform = _selectionBoxRect.GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, _startDragPos, canvas.worldCamera, out Vector2 localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, currentMousePos, canvas.worldCamera, out Vector2 localEnd);

        Vector2 size = new Vector2(Mathf.Abs(localEnd.x - localStart.x), Mathf.Abs(localEnd.y - localStart.y));
        Vector2 center = (localStart + localEnd) / 2f;

        _selectionBoxRect.sizeDelta = size;
        _selectionBoxRect.anchoredPosition = center;
    }


    private Rect GetSelectionRect(Vector2 startPos, Vector2 endPos) {
        float xMin = Mathf.Min(startPos.x, endPos.x);
        float xMax = Mathf.Max(startPos.x, endPos.x);
        float yMin = Mathf.Min(startPos.y, endPos.y);
        float yMax = Mathf.Max(startPos.y, endPos.y);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

}