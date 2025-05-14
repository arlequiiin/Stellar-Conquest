using UnityEngine;

public class Uranuim : MonoBehaviour {
    public ResourceType ResourceType = ResourceType.Uranium;
    public bool isOccupied { get; private set; } = false;

    public bool TryClaim() {
        if (isOccupied) 
            return false;
        isOccupied = true;
        return true;
    }

    public void Release() {
        isOccupied = false;
    }
}
