using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;



public class Engineer : Units
{
    [Header("Buildings Settings")]
    public GameObject buildingPreview;
    public AudioClip constructionStartSound;
    public AudioClip constructionCompleteSound;
    public AudioClip errorSound;

    [Header("Animation")]
    public Animator engineerAnimator;
    public string constructionAnimTrigger = "StartBuilding";

    [Header("Events")]
    public UnityEvent<string> onMessage; // Для UI-уведомлений
    public UnityEvent onConstructionCancel;

    private Buildings selectedBuilding;
    private bool isBuilding = false;
    private bool isProcessing = false; // Защита от многократных кликов
    private AudioSource audioSource;

    private void Update() {
        if (!isBuilding || isProcessing) return;

        // Отмена строительства по правой кнопке
        if (Input.GetMouseButtonDown(1)) {
            CancelConstruction();
            return;
        }

        // Основная логика строительства
        if (Input.GetMouseButtonDown(0)) {
            isProcessing = true;

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //if (Physics.Raycast(ray, out hit)) {
            //    if (selectedBuilding.CanPlace(hit.point)) {
            //        CompleteConstruction(hit.point);
            //    }
            //    else {
            //        onMessage?.Invoke("Нельзя построить здесь!");

            //    }
            //}

            isProcessing = false;
        }
    }

    public void StartConstruction(GameObject buildingPrefab)
    {
        if (buildingPrefab == null) return;

        if (CanPlaceBuilding(out Vector3 position))
        {
            Instantiate(buildingPrefab, position, Quaternion.identity);
            // PlayConstructionAnimation();
        }
    }

    private bool CanPlaceBuilding(out Vector3 position)
    {
        position = transform.position + transform.forward * 5f;
        return true;
    }

    public void StartConstruction(Buildings building)
    {
        //if (!HasEnoughResources(building.cost))
        //{
        //    onMessage?.Invoke("Недостаточно ресурсов!");
        //    return;
        //}

        selectedBuilding = building;
        buildingPreview.SetActive(true);
        isBuilding = true;
        engineerAnimator.SetTrigger(constructionAnimTrigger);
        
    }


    private void CompleteConstruction(Vector3 position)
    {
        // ResourceCost(selectedBuilding.cost);
        Instantiate(selectedBuilding, position, Quaternion.identity);
        buildingPreview.SetActive(false);
        isBuilding = false;
        
    }

    private void CancelConstruction()
    {
        buildingPreview.SetActive(false);
        isBuilding = false;
        onConstructionCancel?.Invoke();
        
    }
}

