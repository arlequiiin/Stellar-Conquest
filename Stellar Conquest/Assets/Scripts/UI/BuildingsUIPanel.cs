using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuildingsUIPanel : MonoBehaviour
{
    [System.Serializable]
    public struct BuildingData
    {
        public string name;
        public int cost;
        public Sprite icon;
        public KeyCode hotkey;
        public GameObject prefab;
    }

    [Header("Main UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _statusText;

    [Header("Building Slots")]
    [SerializeField] private Button[] _buttons = new Button[6];
    [SerializeField] private Image[] _icons = new Image[6];
    [SerializeField] private TextMeshProUGUI[] _costTexts = new TextMeshProUGUI[6];

    [Header("Buildings Configuration")]
    [SerializeField] private BuildingData[] _buildings = new BuildingData[6];

    private Engineer _selectedEngineer;
    private ResourceManager _resourceManager;

    private void Awake()
    {
#pragma warning disable CS0618 // Тип или член устарел
        _resourceManager = FindObjectOfType<ResourceManager>();
#pragma warning restore CS0618 // Тип или член устарел
        InitializeUI();
        Hide();
    }

    private void InitializeUI()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            int index = i;
            _buttons[i].onClick.AddListener(() => TryBuild(index));
            _icons[i].sprite = _buildings[index].icon;
            _costTexts[i].text = _buildings[index].cost.ToString();
        }
    }

    public void Show(Engineer engineer)
    {
        _selectedEngineer = engineer;
        _panel.SetActive(true);
        UpdateButtonsState();
    }

    public void Hide()
    {
        _panel.SetActive(false);
        _selectedEngineer = null;
    }

    private void UpdateButtonsState()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            bool canAfford = _resourceManager.CanAfford(_buildings[i].cost);
            _buttons[i].interactable = canAfford;
            _costTexts[i].color = canAfford ? Color.white : Color.red;
        }
    }

    private void TryBuild(int index)
    {
        if (_selectedEngineer == null) return;

        BuildingData building = _buildings[index];

        if (_resourceManager.TrySpendResources(building.cost))
        {
            _selectedEngineer.StartConstruction(building.prefab);
            Hide();
        }
        else
        {
            ShowMessage("Недостаточно ресурсов!", 2f);
        }
    }

    private void ShowMessage(string text, float duration)
    {
        _statusText.text = text;
        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), duration);
    }

    private void ClearMessage()
    {
        _statusText.text = "";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Hide();

        // Обработка горячих клавиш
        for (int i = 0; i < _buildings.Length; i++)
        {
            if (Input.GetKeyDown(_buildings[i].hotkey))
            {
                TryBuild(i);
                break;
            }
        }
    }
}