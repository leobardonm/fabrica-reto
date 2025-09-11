using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f; // WASD speed
    public float lookSensitivity = 2f;

    [Header("Teleport Targets")]
    public List<Transform> points;

    [Header("Pause Settings")]
    public GameObject pauseCanvas; // Assign in Inspector (starts disabled)

    [Header("Follow Settings")]
    public Vector3 followOffset = new Vector3(0, 2, -5);

    private float rotationX;
    private float rotationY;

    private bool isPaused = false;
    private bool isMovingToTarget = false;
    private Transform moveTarget;
    private float moveTime;
    private float moveDuration = 2f;

    private bool isFollowingAgent = false;
    private Transform agentTarget;

    void Start()
    {
        Vector3 angles = transform.localEulerAngles;
        rotationX = angles.x;
        rotationY = angles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

    void Update()
    {
        // Toggle pause
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePause();
        }

        if (isPaused) return;

        // Follow agent mode
        if (isFollowingAgent && agentTarget != null)
        {
            FollowAgent();
            return; // Prevent manual control
        }

        // Smooth move to target
        if (isMovingToTarget && moveTarget != null)
        {
            MoveToTarget();
            return;
        }

        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        rotationY += mouseX;

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);

        // WASD movement
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

        transform.position += move * moveSpeed * Time.deltaTime;

        // ----------- SMOOTH MOVE TO POINTS -----------
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (points != null && i < points.Count && points[i] != null)
                {
                    StartMove(points[i]);
                }
            }
        }

        // Follow agent
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            GameObject agent = GameObject.FindGameObjectWithTag("Agent");
            if (agent != null)
            {
                agentTarget = agent.transform;
                isFollowingAgent = true;
                isMovingToTarget = false; // stop current move
            }
        }
    }

    private void StartMove(Transform target)
    {
        moveTarget = target;
        moveTime = 0f;
        isMovingToTarget = true;
        isFollowingAgent = false; // stop agent follow
    }

    private void MoveToTarget()
    {
        moveTime += Time.deltaTime / moveDuration;
        float t = Mathf.SmoothStep(0f, 1f, moveTime);

        transform.position = Vector3.Lerp(transform.position, moveTarget.position, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, moveTarget.rotation, t);

        if (moveTime >= 1f)
        {
            isMovingToTarget = false;
            // Sync rotation for mouse look
            Vector3 angles = transform.localEulerAngles;
            rotationX = angles.x;
            rotationY = angles.y;
        }
    }

    private void FollowAgent()
    {
        Vector3 targetPos = agentTarget.position + followOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 3f);

        Quaternion lookRot = Quaternion.LookRotation(agentTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 2f);
    }

    private void TogglePause()
    {
        isPaused = !isPaused;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(isPaused);

        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
/*
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 5f;       // Normal WASD speed
    public float lookSensitivity = 2f; // Sensibilidad del mouse

    //public Transform point1; // Asigna en el Inspector
    //public Transform point2;
    //public Transform point3;
    public List<Transform> points;

    [Header("Smooth Transition")]
    public float transitionDuration = 2f; // Time to move to a point
    public AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float rotationX;
    private float rotationY;
    private bool isTransitioning = false;

    void Start()
    {
        // Inicializar rotaciones con la rotación actual de la cámara
        Vector3 angles = transform.localEulerAngles;
        rotationX = angles.x;
        rotationY = angles.y;

        LockCursor(true);
    }

    void Update()
    {
        if (!isTransitioning)
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
        }

        // ----------- SMOOTH MOVE TO POINTS -----------
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (points != null && i < points.Count && points[i] != null)
                {
                    StartCoroutine(SmoothMoveTo(points[i]));
                }
            }
        }
    }

    IEnumerator SmoothMoveTo(Transform target)
    {
        isTransitioning = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            // Apply easing (EaseInOut by default)
            float easedT = easing.Evaluate(t);

            // Smooth position and rotation
            transform.position = Vector3.Lerp(startPos, endPos, easedT);
            transform.rotation = Quaternion.Slerp(startRot, endRot, easedT);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        // Update mouse rotation angles for consistency
        Vector3 angles = transform.localEulerAngles;
        rotationX = angles.x;
        rotationY = angles.y;

        isTransitioning = false;
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
*/