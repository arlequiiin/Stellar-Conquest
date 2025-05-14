using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuildingsUIPanel : MonoBehaviour {
    public GameObject panel;

    public void Open() => panel.SetActive(true);
    public void Close() => panel.SetActive(false);
}
