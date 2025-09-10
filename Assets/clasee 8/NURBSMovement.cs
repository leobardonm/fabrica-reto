using UnityEngine;

public class NURBSMovement : MonoBehaviour
{
    public Transform[] controlPoints; // Array of control points
    public float[] weights; // Array of weights for control points
    public float duration = 5f; // Duration to complete the curve

    private float t = 0f;
    private int numControlPoints;
    private int degree = 3; // Degree of NURBS
    private float[] knotVector;

    private void Start()
    {
        numControlPoints = controlPoints.Length;
        knotVector = GenerateUniformKnotVector(numControlPoints, degree);

        if(weights == null || weights.Length != numControlPoints)
        {
            // If weights are not set, default them to 1 (i.e. uniform weights)
            weights = new float[numControlPoints];
            for (int i = 0; i < numControlPoints; i++)
            {
                weights[i] = 1f;
            }
        }
    }

    private void Update()
    {
        t += Time.deltaTime / duration;

        if (t >= 1f)
        {
            t = 1f; // Ensure t doesn't exceed 1
        }

        // Calculate the new position using NURBS interpolation
        Vector3 newPosition = GetNURBSPoint(t);

        // Move the object
        transform.position = newPosition;
    }

    private Vector3 GetNURBSPoint(float t)
    {
        Vector3 numerator = Vector3.zero;
        float denominator = 0f;

        for (int i = 0; i < numControlPoints; i++)
        {
            float basis = BSplineBasis(i, degree, t);
            float weightedBasis = basis * weights[i];

            numerator += weightedBasis * controlPoints[i].position;
            denominator += weightedBasis;
        }

        // Handle division by zero
        if (denominator == 0f) {
            return numerator;
        }

        return numerator / denominator;
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