using UnityEngine;
using UnityEngine.UIElements;

public class Extractor : Buildings {
    [SerializeField] private ResourceType _resourceType = ResourceType.Resource;
    [SerializeField] private float _extractionRate = 5f; 
    [SerializeField] private float _extractionInterval = 1f;

    private float _timer;
    private bool _isPlacedOnNode = false; 

    // нужна логика проверки, стоит ли над месторождением

    protected override void Start() {
        base.Start();
        // TODO: ѕроверить, находитс€ ли здание на ResourceNode. ≈сли нет, оно не должно работать.
        // Ќапример, через Physics.OverlapSphere или триггеры при постройке.
        // _isPlacedOnNode = CheckPlacement();

        _isPlacedOnNode = true; 

        if (!_isPlacedOnNode) Debug.LogError($"{gameObject.name} не размещено над месторождением");
        Debug.Log($"{gameObject.name} начало потребл€ть {_powerConsumption} энергии");
    }

    void Update() {
        if (IsPowered && _isPlacedOnNode) {
            _timer += Time.deltaTime;
            if (_timer >= _extractionInterval) {
                _timer -= _extractionInterval;
                float amountExtracted = _extractionRate * _extractionInterval;
                // ƒобавл€ем ресурсы игроку через ResourceManager
                // TODO: ResourceManager.Instance(OwnerPlayerId).AddResource(_resourceType, amountExtracted);

                Debug.Log($"{gameObject.name} добыл {amountExtracted} ресурса {_resourceType} дл€ игрока {OwnerPlayerId}");
            }
        }
        else {
            _timer = 0;
        }
    }

    protected override void OnPowerChanged(bool isPowered) {
        if (isPowered) {
            Debug.Log($"{gameObject.name} энерги€ по€вилась, начата добыча ресурсов");
            // анимации работы?
        }
        else {
            Debug.Log($"{gameObject.name} энергии нет, добыча ресурсов не происходит");
            _timer = 0;
            // анимации работы?
        }
    }
}

public enum ResourceType { Electricity, Resource } 