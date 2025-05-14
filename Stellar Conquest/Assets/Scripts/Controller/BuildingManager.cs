using UnityEngine;

public class BuildingManager : MonoBehaviour {
    public LayerMask placementLayer;
    private GameObject previewInstance;
    private EntityData selectedBuildingData;
    private bool placing = false;

    public void StartPlacingBuilding(EntityData data) {
        selectedBuildingData = data;
        previewInstance = Instantiate(data.prefab);
        MakeTransparent(previewInstance);
        placing = true;
    }

    void Update() {
        if (!placing) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementLayer)) {
            previewInstance.transform.position = hit.point;

            if (Input.GetMouseButtonDown(0)) {
                TryPlaceBuilding(hit.point);
            }
            else if (Input.GetMouseButtonDown(1)) {
                CancelPlacing();
            }
        }
    }

    void TryPlaceBuilding(Vector3 position) {
        if (!IsValidPlacement(position)) {
            CancelPlacing();
            return;
        }

        GameObject instance = Instantiate(selectedBuildingData.prefab, position, Quaternion.identity);
        var building = instance.GetComponent<Buildings>();
        var engineer = GetSelectedEngineer();
        if (engineer != null) {
            engineer.StartConstruction(building);
        }

        placing = false;
        Destroy(previewInstance);
    }

    bool IsValidPlacement(Vector3 position) {
        // Проверка на коллизии, землю, месторождения и т.п.
        var extractor = selectedBuildingData.prefab.GetComponent<Extractor>();


        //if (extractor != null) {
        //    return extractor.HasDepositUnderneath(); // нужен такой геттер в Extractor.cs
        //}

        Collider[] colliders = Physics.OverlapBox(position, Vector3.one * 1f);
        return colliders.Length == 0;
    }

    void CancelPlacing() {
        placing = false;
        if (previewInstance) Destroy(previewInstance);
    }

    void MakeTransparent(GameObject obj) {
        foreach (var r in obj.GetComponentsInChildren<Renderer>()) {
            foreach (var mat in r.materials) {
                mat.shader = Shader.Find("Transparent/Diffuse");
                var color = mat.color;
                color.a = 0.5f;
                mat.color = color;
            }
        }
    }

    Engineer GetSelectedEngineer() {
        // Возвращает выбранного инженера из системы выделения
        return FindObjectOfType<Engineer>(); // Лучше через ваш UnitSelectionManager
    }
}
