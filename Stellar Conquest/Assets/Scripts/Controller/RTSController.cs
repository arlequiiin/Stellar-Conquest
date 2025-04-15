using UnityEngine;
using UnityEngine.InputSystem;

public class RTSController : MonoBehaviour
{
    private PlayerControls _controls;

    private void Awake()
    {
        _controls = new PlayerControls();
    }
}
