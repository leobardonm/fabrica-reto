using UnityEngine;

public class ParametricMovement : MonoBehaviour
{
    public float aX, bX, cX, dX;
    public float aY, bY, cY, dY;
    public float aZ, bZ, cZ, dZ;
    public float duration = 5f;

    private float t = 0f;

    private void Start()
    {
        ResetMovement();
    }

    private void Update()
    {
        t += Time.deltaTime / duration;

        // Calculate the new position using the parametric functions
        float x = aX * Mathf.Pow(t, 3) + bX * Mathf.Pow(t, 2) + cX * t + dX;
        float y = aY * Mathf.Pow(t, 3) + bY * Mathf.Pow(t, 2) + cY * t + dY;
        float z = aZ * Mathf.Pow(t, 3) + bZ * Mathf.Pow(t, 2) + cZ * t + dZ;

        // Set the object's position
        transform.position = new Vector3(x, y, z);

        // Reset the movement after the duration is reached
        if (t >= 1f)
        {
            ResetMovement();
        }
    }

    private void ResetMovement()
    {
        t = 0f;
    }
}