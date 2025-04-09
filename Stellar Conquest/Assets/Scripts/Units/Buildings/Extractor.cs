using UnityEngine;
using UnityEngine.UIElements;

public class Extractor : Buildings {
    [SerializeField] private ResourceType _resourceType = ResourceType.Resource;
    [SerializeField] private float _extractionRate = 5f; 
    [SerializeField] private float _extractionInterval = 1f;

    private float _timer;
    private bool _isPlacedOnNode = false; 

    // ����� ������ ��������, ����� �� ��� ��������������

    protected override void Start() {
        base.Start();
        // TODO: ���������, ��������� �� ������ �� ResourceNode. ���� ���, ��� �� ������ ��������.
        // ��������, ����� Physics.OverlapSphere ��� �������� ��� ���������.
        // _isPlacedOnNode = CheckPlacement();

        _isPlacedOnNode = true; 

        if (!_isPlacedOnNode) Debug.LogError($"{gameObject.name} �� ��������� ��� ��������������");
        Debug.Log($"{gameObject.name} ������ ���������� {_powerConsumption} �������");
    }

    void Update() {
        if (IsPowered && _isPlacedOnNode) {
            _timer += Time.deltaTime;
            if (_timer >= _extractionInterval) {
                _timer -= _extractionInterval;
                float amountExtracted = _extractionRate * _extractionInterval;
                // ��������� ������� ������ ����� ResourceManager
                // TODO: ResourceManager.Instance(OwnerPlayerId).AddResource(_resourceType, amountExtracted);

                Debug.Log($"{gameObject.name} ����� {amountExtracted} ������� {_resourceType} ��� ������ {OwnerPlayerId}");
            }
        }
        else {
            _timer = 0;
        }
    }

    protected override void OnPowerChanged(bool isPowered) {
        if (isPowered) {
            Debug.Log($"{gameObject.name} ������� ���������, ������ ������ ��������");
            // �������� ������?
        }
        else {
            Debug.Log($"{gameObject.name} ������� ���, ������ �������� �� ����������");
            _timer = 0;
            // �������� ������?
        }
    }
}

public enum ResourceType { Electricity, Resource } 