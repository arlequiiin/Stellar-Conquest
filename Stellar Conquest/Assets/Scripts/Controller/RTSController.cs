using UnityEngine;
using UnityEngine.InputSystem;

public class RTSController : MonoBehaviour
{
    private PlayerControls _controls;

    private SoldierBlue _selectedSoldier;

    private void Awake()
    {
        _controls = new PlayerControls();
    }

    private void OnEnable()
    {
        _controls.Enable();

        // �������� �������� ������ ����� � ������
        _controls.Player.Select.performed += ctx => OnSelectUnit();

        Debug.Log("ENABLE");
    }

    private void OnDisable()
    {
        _controls.Disable();
    }

    // ����� ��� ������ �����
    private void OnSelectUnit()
    {
        // �������� ������� ����� �� ������
        Vector2 clickPosition = _controls.Player.MousePosition.ReadValue<Vector2>();
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(clickPosition);
        worldPosition.z = 0; // ���������� ��� Z

        Debug.Log($"Mouse Position: {_controls.Player.MousePosition.ReadValue<Vector2>()}");
        Debug.Log($"World Position: {worldPosition}");

        // �������� ��������� � ��������� �����
        Collider2D hitCollider = Physics2D.OverlapPoint(worldPosition);

        if (hitCollider != null)
        {
            SoldierBlue soldier = hitCollider.GetComponent<SoldierBlue>();
            if (soldier != null)
            {
                Debug.Log("Unit selected!");
                SelectUnit(soldier);
            }
            else {
                Debug.Log("Hit collider, but not a SoldierBlue");
            }
        }
        else {
            Debug.Log("No collider hit.");
        }
    }

    // ����� ��� ��������� �����
    private void SelectUnit(SoldierBlue soldier)
    {
        if (_selectedSoldier != null)
        {
            // ������� ��������� � ����������� �����
            _selectedSoldier.Deselect();
        }

        // �������� ������ �����
        _selectedSoldier = soldier;
        _selectedSoldier.Select();

        Debug.Log("New unit selected and outline enabled.");
    }
}
