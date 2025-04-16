using UnityEngine;

public class CommandCenter : Buildings {
    [SerializeField] private bool _isEssential = true; 

    protected override void Start() {
        base.Start();
        Debug.Log($"Коммандный центр игрока {OwnerPlayerId} построена. Существует: {_isEssential}");
        // может генерировать немного энергии?
    }

    protected override void Die() {
        Debug.Log($"Коммандный центр игрока {OwnerPlayerId} УНИЧТОЖЕН");
        if (_isEssential) {
            // сообщим GameManager о поражении игрока
            // GameManager.Instance.PlayerLost(OwnerPlayerId);
            Debug.LogError($"Игрок {OwnerPlayerId} проиграл");
        }
        base.Die();
    }
}