using UnityEngine;
using UnityEngine.InputSystem;
using System;

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

    private bool _isDragging = false;
    private Vector2 _dragStartPosition;
    private const float DragThreshold = 5f;
    private const float MaxRayDistance = 1000f;

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
        //_controls.Player.RightClick.performed += ctx => HandleRightClick();
        //_controls.Player.Cancel.performed += ctx => OnCancelKeyPressed?.Invoke();
        //_controls.Player.Select.started += ctx => StartDrag(ctx);
        //_controls.Player.Select.canceled += ctx => EndDrag(ctx);
    }

    void Update() {
        //if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        //    return;

        //if (_isDragging) {
        //    Vector2 mousePos = Mouse.current.position.ReadValue();
        //    if (Vector2.Distance(mousePos, _dragStartPosition) > DragThreshold)
        //        OnDragSelectUpdate?.Invoke(mousePos, _dragStartPosition);
        //}
    }

    private void StartDrag(InputAction.CallbackContext ctx) {
        _dragStartPosition = Mouse.current.position.ReadValue();
        _isDragging = true;
        OnDragSelectStart?.Invoke(_dragStartPosition);
    }

    private void EndDrag(InputAction.CallbackContext ctx) {
        if (!_isDragging) return;

        _isDragging = false;
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (Vector2.Distance(mousePos, _dragStartPosition) > DragThreshold) {
            OnDragSelectEnd?.Invoke(mousePos, _dragStartPosition);
        }
        else {
            if (RaycastMouse(out RaycastHit hit, _selectableLayerMask | _groundLayerMask)) {
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
        if (RaycastMouse(out RaycastHit hit, _selectableLayerMask)) {
            OnLeftClick?.Invoke(hit.point);
            Debug.Log($"Рейкаст попал в объект (в HandleLeftClick): {hit.collider.gameObject.name}, Слой: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, Точка попадания: {hit.point}");
        }
        else {
            OnLeftClick?.Invoke(Vector3.negativeInfinity);
            Debug.Log("Рейкаст ничего не попал (в HandleLeftClick)");
        }
    }

    private void HandleRightClick() {
        if (RaycastMouse(out RaycastHit hit, _interactableLayerMask))
            OnRightClick?.Invoke(hit.point);
    }

    private bool RaycastMouse(out RaycastHit hitInfo, LayerMask layerMask) {
        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        return Physics.Raycast(ray, out hitInfo, MaxRayDistance, layerMask);
    }

    public bool GetObjectUnderCursor<T>(out T component, LayerMask layerMask) where T : Component {
        Debug.Log($"Выполняем RaycastMouse с маской: {layerMask.value}"); 
        component = null;
        if (RaycastMouse(out RaycastHit hit, layerMask)) {
            Debug.Log($"Raycast попал в объект: {hit.collider.gameObject.name}");
            component = hit.collider.GetComponent<T>();
            Debug.Log($"Получен компонент {typeof(T)}: {component != null}"); 
            return component != null;
        }
        return false;
    }

    public bool GetGroundPointUnderCursor(out Vector3 point) {
        point = Vector3.zero;
        if (RaycastMouse(out RaycastHit hit, _groundLayerMask)) {
            point = hit.point;
            return true;
        }
        return false;
    }
}
