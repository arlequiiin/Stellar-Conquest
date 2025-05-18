using UnityEngine;

public class CommandCenter : Buildings {
    [SerializeField] private bool _isEssential = true;
    [SerializeField] private bool _isBot;

    protected override void Start() {
        base.Start();
        Debug.Log($"���������� ����� {OwnerPlayerId} ���������. ����������: {_isEssential}");
        // ����� ������������ ������� �������?
    }

    protected override void Die() {
        base.Die();

        if (!_isBot) 
            Debug.Log($"���������� ����� ������ ���������");
        else            
            Debug.Log($"���������� ����� ���� ���������");

        if (_isEssential && !_isBot) {
            GameManager.Instance.PlayerLost();
        }
        if (_isBot) {
            GameManager.Instance.PlayerWin();
        }
    }
}