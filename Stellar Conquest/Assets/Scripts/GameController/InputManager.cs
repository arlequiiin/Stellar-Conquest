using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.EventSystems; 
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

public class InputManager : MonoBehaviour {
    public static InputManager Instance { get; private set; }

    public event Action<Vector3> OnLeftClick;
    public event Action<Vector3> OnShiftLeftClick;
    public event Action<Vector3> OnRightClick;
    public event Action OnCancelKeyPressed;
    public event Action<Vector2> OnDragSelectStart;
    public event Action<Vector2, Vector2> OnDragSelectUpdate;
    public event Action<Vector2, Vector2> OnDragSelectEnd;

    [SerializeField] private Camera _mainCamera;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private LayerMask _selectableLayerMask;
    [SerializeField] private LayerMask _interactableLayerMask;
    [SerializeField] private LayerMask _enemyLayerMask;

    private PlayerControls _controls;

    private bool isDragging = false;
    private Vector2 dragStartPosition;
    private const float DragThreshold = 5f;
    private const float MaxRayDistance = 1000f;
    private bool nextLeftButtonClick = false;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_mainCamera == null) _mainCamera = Camera.main;

        _controls = new PlayerControls();
        _controls.Enable();

        _controls.Player.Select.performed += ctx => HandleLeftClick();
        _controls.Player.RightClick.performed += ctx => HandleRightClick();
        _controls.Player.Cancel.performed += ctx => OnCancelKeyPressed?.Invoke();

        _controls.Player.Select.started += ctx => StartDrag(ctx);
        _controls.Player.Select.canceled += ctx => EndDrag(ctx);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        _mainCamera = Camera.main;
    }
    void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void Update() {
        if (isDragging) {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (Vector2.Distance(mousePos, dragStartPosition) > DragThreshold)
                OnDragSelectUpdate?.Invoke(mousePos, dragStartPosition);
        }
    }


    private void StartDrag(InputAction.CallbackContext ctx) {
        dragStartPosition = Mouse.current.position.ReadValue();
        isDragging = true;
        OnDragSelectStart?.Invoke(dragStartPosition);
    }

    private void EndDrag(InputAction.CallbackContext ctx) {
        if (!isDragging) return;

        isDragging = false;
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (Vector2.Distance(mousePos, dragStartPosition) > DragThreshold) {
            OnDragSelectEnd?.Invoke(mousePos, dragStartPosition);
            nextLeftButtonClick = true;
        }
        else {
            nextLeftButtonClick = true;
            if (IsPointerOverUI()) return;
            
            if (RaycastMouse(out RaycastHit2D hit, _selectableLayerMask | _groundLayerMask)) {
                if (_controls.Player.MultiSelect.ReadValue<float>() > 0)
                    OnShiftLeftClick?.Invoke(hit.point);
                else
                    OnLeftClick?.Invoke(hit.point);
            }
            else {
                OnLeftClick?.Invoke(Vector3.negativeInfinity);
            }
        }
    }

    private void HandleLeftClick() {
        if (nextLeftButtonClick) { 
            nextLeftButtonClick = false;
            return;
        }
        if (IsPointerOverUI()) {
            return;
        }

        if (RaycastMouse(out RaycastHit2D hit, _selectableLayerMask)) {
            OnLeftClick?.Invoke(hit.point);
        }
        else {
            OnLeftClick?.Invoke(Vector3.negativeInfinity);
        }
    }

    private void HandleRightClick() {
        if (IsPointerOverUI()) {
            Debug.Log("Нажато ПКМ по UI");
            return;
        }

        LayerMask rightClickMask = _groundLayerMask | _enemyLayerMask | _interactableLayerMask;

        if (RaycastMouse(out RaycastHit2D hit, rightClickMask)) {
            OnRightClick?.Invoke(hit.point);
        }
    }

    private bool RaycastMouse(out RaycastHit2D hitInfo, LayerMask layerMask) {
        hitInfo = default;

        if (_mainCamera == null) {
            Debug.LogWarning("Main camera is null!");
            return false;
        }

        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, MaxRayDistance, layerMask);

        if (hit.collider != null) {
            hitInfo = hit;
            return true; 
        }

        return false; 
    }

    public bool GetObjectUnderCursor<T>(out T component, LayerMask layerMask) where T : Component {
        component = null;
        if (RaycastMouse(out RaycastHit2D hit, layerMask)) {
            component = hit.collider.GetComponent<T>();
            return component != null;
        }
        return false;
    }

    public bool GetGroundPointUnderCursor(out Vector3 point) {
        point = Vector3.zero;
        if (RaycastMouse(out RaycastHit2D hit, _groundLayerMask)) {
            point = hit.point;
            return true;
        }
        return false;
    }

    private bool IsPointerOverUI() {
        if (EventSystem.current == null) {
            Debug.LogError("EventSystem отсутствует в сцене! UI события не будут работать корректно.");
            return false;
        }
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}
