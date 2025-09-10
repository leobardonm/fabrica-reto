using UnityEngine;

public class BSplineMovement : MonoBehaviour
{
    public Transform[] controlPoints; // Array of control points
    public float duration = 5f; // Duration to complete the spline path

    private float t = 0f;
    private int numControlPoints;
    private int degree = 3; // Degree of B-spline
    private float[] knotVector;

    private void Start()
    {
        numControlPoints = controlPoints.Length;
        knotVector = GenerateUniformKnotVector(numControlPoints, degree);
    }

    private void Update()
    {
        t += Time.deltaTime / duration;

        if (t >= 1f)
        {
            t = 1f; // Ensure t doesn't exceed 1
        }

        // Calculate the new position using B-spline interpolation
        Vector3 newPosition = GetBSplinePoint(t);

        // Move the object
        transform.position = newPosition;
    }

    private Vector3 GetBSplinePoint(float t)
    {
        Vector3 point = Vector3.zero;

        for (int i = 0; i < numControlPoints; i++)
        {
            float basis = BSplineBasis(i, degree, t);
            point += basis * controlPoints[i].position;
        }

        return point;
    }

    private float[] GenerateUniformKnotVector(int numControlPoints, int degree)
    {
        int knotCount = numControlPoints + degree + 1;
        float[] knotVector = new float[knotCount];

        for (int i = 0; i < knotCount; i++)
        {
            if (i <= degree)
                knotVector[i] = 0f;
            else if (i >= knotCount - degree - 1)
                knotVector[i] = 1f;
            else
                knotVector[i] = (float)(i - degree) / (numControlPoints - degree);
        }

        return knotVector;
    }

    private float BSplineBasis(int i, int p, float t)
    {
        // Base case for degree 0
        if (p == 0)
        {
            return (t >= knotVector[i] && t < knotVector[i + 1]) ? 1f : 0f;
        }

        // Recursive definition
        float left = (t - knotVector[i]) / (knotVector[i + p] - knotVector[i]);
        float right = (knotVector[i + p + 1] - t) / (knotVector[i + p + 1] - knotVector[i + 1]);

        float leftBasis = (knotVector[i + p] - knotVector[i]) == 0 ? 0 : left * BSplineBasis(i, p - 1, t);
        float rightBasis = (knotVector[i + p + 1] - knotVector[i + 1]) == 0 ? 0 : right * BSplineBasis(i + 1, p - 1, t);

        return leftBasis + rightBasis;
    }
}