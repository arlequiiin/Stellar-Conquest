using UnityEngine;

[CreateAssetMenu(fileName = "Новый юнит", menuName = "RTS")]

public class EntityData : ScriptableObject {
    public Sprite icon;
    public string entityName;
    public string description;
}