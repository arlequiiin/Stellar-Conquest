using UnityEngine;

public class CameraController : MonoBehaviour {
    public float moveSpeed = 10f;
    public float edgeScrollSize = 10f; 
    public float zoomSpeed = 5f;
    public float minZoom = 5f; 
    public float maxZoom = 15f;

    private Camera cam;

    void Start() {
        cam = Camera.main;
    }

    void Update() {
        HandleKeyboardMovement();
        // HandleMouseEdgeScrolling();
        HandleZoom();
    }

    void HandleKeyboardMovement() {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical"); 

        Vector3 move = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        transform.position += move;
    }

    void HandleMouseEdgeScrolling() {
        Vector3 pos = transform.position;

        if (Input.mousePosition.x >= Screen.width - edgeScrollSize)
            pos.x += moveSpeed * Time.deltaTime;
        if (Input.mousePosition.x <= edgeScrollSize)
            pos.x -= moveSpeed * Time.deltaTime;
        if (Input.mousePosition.y >= Screen.height - edgeScrollSize)
            pos.y += moveSpeed * Time.deltaTime;
        if (Input.mousePosition.y <= edgeScrollSize)
            pos.y -= moveSpeed * Time.deltaTime;

        transform.position = pos;
    }

    void HandleZoom() {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        cam.orthographicSize -= scroll * zoomSpeed;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }
}
