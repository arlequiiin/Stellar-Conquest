using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SelectionManager : MonoBehaviour {
    // --- Singleton ---
    private static SelectionManager _instance;
    public static SelectionManager Instance { get { /* ... Singleton Get ... */ return _instance; } }

    // --- State ---
    [SerializeField] private List<Entity> _selectedEntities = new List<Entity>();
    public IReadOnlyList<Entity> SelectedEntities => _selectedEntities; // ������ ������ ��� ������ �����

    // --- Dependencies ---
    private InputManager _inputManager;
    [SerializeField] private LayerMask _selectableLayerMask;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private LayerMask _enemyLayerMask; // ���� ��� ��������� ������/������

    // --- Drag Selection UI ---
    [SerializeField] private RectTransform _selectionBoxRect; // ������ �� UI Image/Panel ��� �����
    private Vector2 _startDragPos;

    void Awake() {
        // Ensure Singleton
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        // DontDestroyOnLoad(gameObject); // ���� �����

        if (_selectionBoxRect != null) _selectionBoxRect.gameObject.SetActive(false);
    }

    void Start() {
        _inputManager = InputManager.Instance;
        if (_inputManager == null) {
            Debug.LogError("SelectionManager'� ��������� InputManager");
            enabled = false; 
            return;
        }

        // ������������� �� ������� InputManager
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
        ClearSelection(); // ������� ������� ���������� ���������

        // ���� ���� ��� �� � �������
        if (clickPosition != Vector3.negativeInfinity) {
            // �������� ����� ������ ��� ��������
            if (_inputManager.GetObjectUnderCursor<Entity>(out Entity clickedEntity, _selectableLayerMask)) {
                // �������� ������ ���� ��� ����/������ ���������� ������
                if (clickedEntity.OwnerPlayerId == GameManager.Instance?.LocalPlayerId) {
                    SelectEntity(clickedEntity);
                }
                // ����� �������� � ��������� �������?
                // else { SelectEntity(clickedEntity, allowCommands: false); }
            }
        }
        UpdateSelectionUI(); // �������� UI � ����� ������ (�������� ���� ��� ��������)
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
        if (_selectedEntities.Count == 0) return; // ��� ���������� ������

        // 1. ���������, �������� �� �� �����
        if (_inputManager.GetObjectUnderCursor<Entity>(out Entity targetEntity, _enemyLayerMask)) // ���������� _enemyLayerMask
        {
            // ���������, ��� ���� ������������� ����
            if (targetEntity.OwnerPlayerId != GameManager.Instance?.LocalPlayerId && targetEntity.OwnerPlayerId != 0) // 0 - ����������� ID?
            {
                Debug.Log($"Ordering selected units to attack {targetEntity.name}");
                IssueAttackCommand(targetEntity);
                return; // ��������� �� �����
            }
        }

        // 2. ���� �� �� �����, ���������, �������� �� �� �����
        if (_inputManager.GetGroundPointUnderCursor(out Vector3 groundPoint)) {
            Debug.Log($"Ordering selected units to move to {groundPoint}");
            IssueMoveCommand(groundPoint);
            return;
        }

        // 3. TODO: ���� �� ������ �������� (�������, ������������� ������ ��� ������� � �.�.)?
    }

    private void HandleCancelKey() {
        if (_selectedEntities.Count > 0) {
            Debug.Log("Issuing Stop command to selected units.");
            foreach (var entity in _selectedEntities) {
                if (entity is Units unit) // �������� ������ ��� ������
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
        Entity[] allSelectables = FindObjectsOfType<Entity>();

        foreach (var entity in allSelectables) {
            // ���������, ����������� �� ���������� ������ � ��������� �� � �����
            if (entity.OwnerPlayerId == GameManager.Instance?.LocalPlayerId) {
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(entity.transform.position);
                // ���������, ��� ������ ����� ������� � ������ ��������������
                if (screenPoint.z > 0 && selectionRect.Contains(screenPoint)) {
                    SelectEntity(entity, false); // ��������� ��� �������
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
            entity.Select(); // �������� ����� ��������� � ������ �������
                             // Debug.Log($"Selected: {entity.gameObject.name}");
        }
    }

    private void DeselectEntity(Entity entity) {
        if (entity == null) return;

        if (_selectedEntities.Contains(entity)) {
            entity.Deselect(); // ������� ��������� � �������
            _selectedEntities.Remove(entity);
            // Debug.Log($"Deselected: {entity.gameObject.name}");
        }
    }

    private void ClearSelection() {
        // ������� ��������� �� ���� ����� ����������
        foreach (var entity in _selectedEntities) {
            if (entity != null) // �������� �� ������, ���� ������ ��� ��������� ���� ��� �������
            {
                entity.Deselect();
            }
        }
        _selectedEntities.Clear();
        // Debug.Log("Selection Cleared.");
    }

    private void IssueMoveCommand(Vector3 destination) {
        // TODO: �������� ��������? ���� ������ ���������� ���� � ���� �����
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

    // ���������� UI, ���������� � ���������� (������ ���������� � �.�.)
    private void UpdateSelectionUI() {
        Debug.Log($"Selection updated. Count: {_selectedEntities.Count}");
        // TODO: ����� ��� ��� ���������� UI:
        // - ���� ������� 1 ������: �������� ��� ������ ���������� (������, ��������, ������ ��������)
        // - ���� �������� ���������: �������� ����� ������, ����� ���-��, ��������, ������ ������ ��� ����� ��������
        // - ���� ��������� �����: ������ ������ ����������
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