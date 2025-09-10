using UnityEngine;

public class HermiteSpin : MonoBehaviour
{
    public Transform[] controlPoints; // Array to store control points
    public float duration = 5f; // Duration to complete the spline path

    private float t = 0f;
    private int segmentCount;
    private float[] segmentDurations;

    private void Start()
    {
        segmentCount = controlPoints.Length - 1;
        segmentDurations = new float[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            segmentDurations[i] = 1f / segmentCount;
        }
    }

    private void Update()
    {
        t += Time.deltaTime / duration;

        if (t >= 1f)
        {
            t = 1f; // Ensure t doesn't exceed 1
        }

        // Get the segment index based on t
        int segmentIndex = Mathf.Min(Mathf.FloorToInt(t * segmentCount), segmentCount - 1);

        // Get the local t value for the segment
        float localT = (t - segmentIndex * segmentDurations[segmentIndex]) / segmentDurations[segmentIndex];

        // Calculate the new position using the cubic spline interpolation
        Vector3 newPosition = GetCubicSplinePoint(segmentIndex, localT);

        // Move the object
        transform.position = newPosition;

        // Spin the object while moving
        transform.Rotate(Vector3.up * 180f * Time.deltaTime, Space.World);

    }

    private Vector3 GetCubicSplinePoint(int segmentIndex, float t)
    {
        Vector3 p0 = controlPoints[segmentIndex].position;
        Vector3 p1 = controlPoints[segmentIndex + 1].position;

        // Ensure segmentIndex is within bounds
        Vector3 m0 = segmentIndex == 0 ? (p1 - p0) : (controlPoints[segmentIndex + 1].position - controlPoints[segmentIndex - 1].position) / 2f;
        Vector3 m1 = segmentIndex == segmentCount - 1 ? (p1 - p0) : (controlPoints[segmentIndex + 2].position - controlPoints[segmentIndex].position) / 2f;

        // Cubic Hermite spline formula
        float t2 = t * t;
        float t3 = t2 * t;
        float h00 = 2f * t3 - 3f * t2 + 1f;
        float h10 = t3 - 2f * t2 + t;
        float h01 = -2f * t3 + 3f * t2;
        float h11 = t3 - t2;

        return h00 * p0 + h10 * m0 + h01 * p1 + h11 * m1;
    }
}
