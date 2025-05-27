using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class LevelData {
    public string sceneName;          
    public Sprite preview;
    [TextArea] public string description;
}

public class SelectLevelUI : MonoBehaviour { 
    [SerializeField] private LevelData[] levels;          // заполняем в инспекторе
    [SerializeField] private GameObject cardPrefab;       // наш LevelCard
    [SerializeField] private Transform container;         // HorizontalLayoutGroup

    private void Start() {
        foreach (var lvl in levels) {
            var card = Instantiate(cardPrefab, container);
            card.GetComponentInChildren<Image>().sprite = lvl.preview;
            card.GetComponentInChildren<TMP_Text>().text = lvl.description;

            var btn = card.GetComponent<Button>();
            string scene = lvl.sceneName;
            btn.onClick.AddListener(() => SceneManager.LoadScene(scene));
        }
    }

    public void OnBack() => SceneManager.LoadScene("MainMenu");
}
