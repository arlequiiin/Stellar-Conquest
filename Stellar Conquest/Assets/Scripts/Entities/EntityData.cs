using UnityEngine;

[CreateAssetMenu(fileName = "Новый", menuName = "RTS")]

public class EntityData : ScriptableObject {
    public Sprite icon;
    public string entityName;
    [TextArea(2, 4)] public string description;

    public float maxHealth;
    public float buildTime;
    public float uraniumCost;
    public float energyCost;
    public GameObject prefab;
}