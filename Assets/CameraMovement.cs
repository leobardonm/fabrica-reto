using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 5f;       // Velocidad de movimiento
    public float lookSensitivity = 2f; // Sensibilidad del mouse

    public Transform point1; // Asigna en el Inspector
    public Transform point2;
    public Transform point3;

    float rotationX;
    float rotationY;

    void Start()
    {
        // Inicializar rotaciones con la rotación actual de la cámara
        Vector3 angles = transform.localEulerAngles;
        rotationX = angles.x;
        rotationY = angles.y;

        // Bloquear y ocultar cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // ----------- MOUSE LOOK -----------
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        rotationY += mouseX;

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);

        // ----------- MOVIMIENTO -----------
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

        transform.position += move * moveSpeed * Time.deltaTime;

        // ----------- TELEPORT A PUNTOS -----------
        if (Input.GetKeyDown(KeyCode.Alpha1) && point1 != null)
            TeleportTo(point1);

        if (Input.GetKeyDown(KeyCode.Alpha2) && point2 != null)
            TeleportTo(point2);

        if (Input.GetKeyDown(KeyCode.Alpha3) && point3 != null)
            TeleportTo(point3);
    }

    void TeleportTo(Transform target)
    {
        transform.position = target.position;
        transform.rotation = target.rotation;

        // Actualizar los ángulos de rotación para que el mouse siga correcto
        Vector3 angles = transform.localEulerAngles;
        rotationX = angles.x;
        rotationY = angles.y;
    }
}
