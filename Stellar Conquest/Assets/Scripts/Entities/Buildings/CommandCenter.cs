using UnityEngine;

public class CommandCenter : Buildings {
    [SerializeField] private bool _isEssential = true;
    [SerializeField] private bool _isBot;

    protected override void Start() {
        base.Start();
        Debug.Log($"Коммандный центр {OwnerPlayerId} построена. Существует: {_isEssential}");
        // может генерировать немного энергии?
    }

    protected override void Die() {
        base.Die();

        if (!_isBot) 
            Debug.Log($"Коммандный центр игрока УНИЧТОЖЕН");
        else            
            Debug.Log($"Коммандный центр бота УНИЧТОЖЕН");

        if (_isEssential && !_isBot) {
            GameManager.Instance.PlayerLost();
        }
        if (_isBot) {
            GameManager.Instance.PlayerWin();
        }
    }
}