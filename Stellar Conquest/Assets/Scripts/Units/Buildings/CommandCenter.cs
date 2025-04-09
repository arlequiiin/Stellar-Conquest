using UnityEngine;

public class CommandCenter : Buildings {
    [SerializeField] private bool _isEssential = true; 

    protected override void Start() {
        base.Start();
        Debug.Log($"���������� ����� ������ {OwnerPlayerId} ���������. ����������: {_isEssential}");
        // ����� ������������ ������� �������?
    }

    protected override void Die() {
        Debug.Log($"���������� ����� ������ {OwnerPlayerId} ���������");
        if (_isEssential) {
            // ������� GameManager � ��������� ������
            // GameManager.Instance.PlayerLost(OwnerPlayerId);
            Debug.LogError($"����� {OwnerPlayerId} ��������");
        }
        base.Die();
    }
}