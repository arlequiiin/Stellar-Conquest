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
            Debug.LogError("SelectionManager'� ��������� InputManager");
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
                    Debug.Log($"������ ���� ������: {clickedEntity.name}");
                }
                else {
                    Debug.Log($"���� �� ������/������������ �������: {clickedEntity.name}");
                }
            }
            else {
                Debug.Log("���� �� ����� ��� ������� ������������� �������");
            }
        }
        else {
            Debug.Log("���� � �������");
        }


        if (!clickedFriendlySelectable) {
            ClearSelection(); 
        }
        UpdateSelectionUI(); 
    }

    private void HandleShiftLeftClick(Vector3 clickPosition) {
        // �� ������� ���������!
        if (clickPosition != Vector3.negativeInfinity) {
            if (_inputManager.GetObjectUnderCursor<Entity>(out Entity clickedEntity, _selectableLayerMask)) {
                // ���������/������� ������ ������/������ ���������� ������
                if (clickedEntity.OwnerPlayerId == GameManager.Instance?.LocalPlayerId) {
                    if (_selectedEntities.Contains(clickedEntity)) {
                        DeselectEntity(clickedEntity); // ������� �� ���������
                    }
                    else {
                        SelectEntity(clickedEntity, false); // ��������� � ��������� (�� ������ ������)
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

        if (_inputManager.GetObjectUnderCursor<Entity>(out targetEntity, enemyMask)) // ���������� _enemyLayerMask
        {
            if (targetEntity.OwnerPlayerId != GameManager.Instance?.LocalPlayerId && targetEntity.OwnerPlayerId != 0) 
            {
                Debug.Log($"������ ���������: {targetEntity.name}");
                IssueAttackCommand(targetEntity);
                hitEnemy = true;
            }
        }

        if (_inputManager.GetGroundPointUnderCursor(out groundPoint)) {
            Debug.Log($"������ ��������� � �����: {groundPoint}");
            IssueMoveCommand(groundPoint); // ���� ����� �������� unit.MoveTo()
            hitGround = true;
        }

        if (!hitEnemy && !hitGround) {
            Debug.Log("���: ������������ ���� ��� �������.");
        }

        //���� �� ������ �������� ?
    }

    private void HandleCancelKey() {
        if (_selectedEntities.Count > 0) {
            Debug.Log("������� ���� ��� ������");
            foreach (var entity in _selectedEntities) {
                if (entity is Units unit) // �������� ������ ��� ������
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
        UpdateSelectionBox(screenPos); // �������������� ������/�������
    }

    private void HandleDragUpdate(Vector2 currentScreenPos, Vector2 startScreenPos) {
        if (_selectionBoxRect == null || !_selectionBoxRect.gameObject.activeSelf) return;
        UpdateSelectionBox(currentScreenPos);
    }

    private void HandleDragEnd(Vector2 endScreenPos, Vector2 startScreenPos) {
        if (_selectionBoxRect != null) _selectionBoxRect.gameObject.SetActive(false);

        ClearSelection(); // ������� ����� ���������� ������

        Rect selectionRect = GetSelectionRect(_startDragPos, endScreenPos);

        // ������� ��� ���������� ������� � �����
        // TODO: ��������������! �� ������ ��� ������� ������ ���. ������������ Quadtree ��� Physics.OverlapBox?
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
            entity.Deselect(); // ������� ��������� � �������
            _selectedEntities.Remove(entity);
            Debug.Log($"Deselected: {entity.gameObject.name}");
        }
    }

    private void ClearSelection() {
        List<Entity> entitiesToDeselect = new List<Entity>(_selectedEntities);

        foreach (var entity in entitiesToDeselect) {
            if (entity != null) // �������� �� ������, ���� ������ ��� ��������� ���� ��� �������
            {
                entity.Deselect();
            }
        }
        _selectedEntities.Clear();
        Debug.Log("��������� �������");
    }

    private void IssueMoveCommand(Vector3 destination) {
        // ���� ������ ���������� ���� � ���� �����
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
        // ������ �����?
    }

    private void UpdateSelectionUI() {
        if (_selectedEntities.Count == 1) {
            Entity selectedEntity = _selectedEntities[0];

            if (selectedEntity is Factory selectedFactory) 
            {
                if (_factoryUIPanel != null) {
                    _factoryUIPanel.Show(selectedFactory); 
                }
                // TODO: ������ ������ UI ������ (��������, ��� ������)
            }
            else {
                if (_factoryUIPanel != null) _factoryUIPanel.Hide();
                // TODO: �������� UI ��� ����� ��� ������� ���� ������
            }
        }
        else
        {
            if (_factoryUIPanel != null) _factoryUIPanel.Hide(); 
            // TODO: �������� UI ��� ������-��������� ��� ������ ��� UI ������
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
        // ����������� ����������, ����� xMin/yMin ������ ���� ������ xMax/yMax
        float xMin = Mathf.Min(startPos.x, endPos.x);
        float xMax = Mathf.Max(startPos.x, endPos.x);
        float yMin = Mathf.Min(startPos.y, endPos.y);
        float yMax = Mathf.Max(startPos.y, endPos.y);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

}