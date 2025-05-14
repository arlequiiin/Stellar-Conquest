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

    [SerializeField] private FactoryUIPanel factoryUIPanel;
    [SerializeField] private SelectionUIPanel selectionUIPanel;
    [SerializeField] private OrdersUIPanel orderUIPanel;
    [SerializeField] private BuildingsUIPanel buildingsUIPanel;

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
                // Добавляем/удаляем только юнитов/здания локального игрока
                if (clickedEntity.OwnerPlayerId == GameManager.Instance.playerId) {
                    if (_selectedEntities.Contains(clickedEntity)) {
                        DeselectEntity(clickedEntity);
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

        //клик по другим объектам ?
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
            ClearSelection();
        }

        if (!_selectedEntities.Contains(entity)) {
            _selectedEntities.Add(entity);
            entity.Select(); 
        }
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
        selectionUIPanel.Clear();
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
            selectionUIPanel.UpdateEntityInfo(selectedEntity);

            if (selectedEntity is Factory selectedFactory) 
            {
                if (factoryUIPanel != null) { 
                    factoryUIPanel.Show(selectedFactory); 
                }
                // Скрыть другие UI панели
            }
            else if (selectedEntity is Engineer selectedEngineer) {
                 buildingsUIPanel.Open();
            }
            else {
                if (factoryUIPanel != null) factoryUIPanel.Hide();
                // оказать UI для юнита или другого типа здания
            }
        }
        else
        {
            selectionUIPanel.Clear();
            if (factoryUIPanel != null) factoryUIPanel.Hide(); 
            // скрыть все UI панели
        }
    }

    private void UpdateSelectionBox(Vector2 currentMousePos) {
        if (!_selectionBoxRect.gameObject.activeSelf) return;

        float width = currentMousePos.x - _startDragPos.x;
        float height = currentMousePos.y - _startDragPos.y;

        _selectionBoxRect.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        _selectionBoxRect.anchoredPosition = _startDragPos + new Vector2(width / 2, height / 2);
    }

    private Rect GetSelectionRect(Vector2 startPos, Vector2 endPos) {
        float xMin = Mathf.Min(startPos.x, endPos.x);
        float xMax = Mathf.Max(startPos.x, endPos.x);
        float yMin = Mathf.Min(startPos.y, endPos.y);
        float yMax = Mathf.Max(startPos.y, endPos.y);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

}