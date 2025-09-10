using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class WarehouseLayout
{
    public List<List<int>> shelves;
    public List<List<int>> machinery;
    public List<int> delivery_point;
    public List<List<int>> boxes;
    public List<List<int>> path_to_box;
    public List<List<int>> path_to_delivery;
}

public class WarehouseSimulation : MonoBehaviour
{
    [Header("JSON File")]
    public string fileName = "warehouse_layout.json";

    [Header("Prefabs")]
    public GameObject shelfPrefab;
    public GameObject machineryPrefab;
    public GameObject deliveryPointPrefab;
    public GameObject boxPrefab;
    public GameObject pathNodePrefab;
    public GameObject agentPrefab;

    [Header("Settings")]
    public float cellSize = 1.0f;
    public float animationSpeed = 0.5f;
    public bool showPaths = true;

    private WarehouseLayout layout;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    IEnumerator Start()
    {
        yield return LoadLayoutAsync();
        if (layout == null)
        {
            Debug.LogError("Failed to load warehouse layout");
            yield break;
        }

        BuildWarehouse();
        StartCoroutine(SimulateAgentMovement());
    }

    IEnumerator LoadLayoutAsync()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS
        if (!path.StartsWith("file://"))
            path = "file://" + path;
#endif

        using (UnityWebRequest req = UnityWebRequest.Get(path))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError("Error loading JSON: " + req.error);
                yield break;
            }

            string json = req.downloadHandler.text;
            layout = JsonUtility.FromJson<WarehouseLayout>(json);
        }
    }

    void BuildWarehouse()
    {
        ClearScene();

        // Create grid background
        CreateGridPlane();

        // Spawn shelves
        foreach (var shelfPos in layout.shelves)
        {
            SpawnObject(shelfPrefab, shelfPos[0], shelfPos[1], "Shelf", Color.gray);
        }

        // Spawn machinery
        foreach (var machinePos in layout.machinery)
        {
            SpawnObject(machineryPrefab, machinePos[0], machinePos[1], "Machine", Color.red);
        }

        // Spawn delivery point
        SpawnObject(deliveryPointPrefab, layout.delivery_point[0], layout.delivery_point[1],
                   "DeliveryPoint", Color.green);

        // Spawn boxes
        foreach (var boxPos in layout.boxes)
        {
            SpawnObject(boxPrefab, boxPos[0], boxPos[1], "Box", Color.yellow);
        }

        // Visualize paths (optional)
        if (showPaths)
        {
            VisualizePath(layout.path_to_box, "PathToBox", Color.blue);
            VisualizePath(layout.path_to_delivery, "PathToDelivery", Color.magenta);
        }
    }

    void CreateGridPlane()
    {
        // Estimate grid size based on maximum coordinates
        int maxX = 0, maxY = 0;
        FindMaxCoordinates(ref maxX, ref maxY);

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.position = new Vector3(maxX * cellSize / 2f, -0.1f, maxY * cellSize / 2f);
        plane.transform.localScale = new Vector3(maxX * cellSize / 10f, 1f, maxY * cellSize / 10f);
        plane.name = "GridPlane";

        // Create a simple material with transparency
        Material gridMaterial = new Material(Shader.Find("Standard"));
        gridMaterial.color = new Color(0.9f, 0.9f, 0.9f, 0.3f);
        plane.GetComponent<Renderer>().material = gridMaterial;

        spawnedObjects.Add(plane);
    }

    void FindMaxCoordinates(ref int maxX, ref int maxY)
    {
        maxX = Mathf.Max(maxX, layout.delivery_point[0]);
        maxY = Mathf.Max(maxY, layout.delivery_point[1]);

        foreach (var pos in layout.shelves)
        {
            maxX = Mathf.Max(maxX, pos[0]);
            maxY = Mathf.Max(maxY, pos[1]);
        }
        foreach (var pos in layout.machinery)
        {
            maxX = Mathf.Max(maxX, pos[0]);
            maxY = Mathf.Max(maxY, pos[1]);
        }
        foreach (var pos in layout.boxes)
        {
            maxX = Mathf.Max(maxX, pos[0]);
            maxY = Mathf.Max(maxY, pos[1]);
        }
    }

    void SpawnObject(GameObject prefab, int x, int y, string name, Color color)
    {
        Vector3 position = ToWorldPosition(x, y);
        GameObject obj;

        if (prefab != null)
        {
            obj = Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * (cellSize * 0.8f);
        }

        obj.name = $"{name}_{x}_{y}";

        // Set color if the object has a renderer
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        spawnedObjects.Add(obj);
    }

    void VisualizePath(List<List<int>> path, string pathName, Color color)
    {
        if (path == null || path.Count == 0) return;

        for (int i = 0; i < path.Count; i++)
        {
            var node = path[i];
            GameObject pathNode;

            if (pathNodePrefab != null)
            {
                pathNode = Instantiate(pathNodePrefab, ToWorldPosition(node[0], node[1]), Quaternion.identity);
            }
            else
            {
                pathNode = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pathNode.transform.position = ToWorldPosition(node[0], node[1]);
                pathNode.transform.localScale = Vector3.one * (cellSize * 0.2f);
            }

            pathNode.name = $"{pathName}_{i}";

            // Set color if the object has a renderer
            Renderer renderer = pathNode.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            spawnedObjects.Add(pathNode);
        }
    }

    IEnumerator SimulateAgentMovement()
    {
        if (agentPrefab == null || layout.path_to_box == null || layout.path_to_box.Count == 0)
        {
            Debug.LogWarning("Agent prefab or path to box is missing");
            yield break;
        }

        // Create agent at the start of the path to box
        GameObject agent = Instantiate(agentPrefab,
            ToWorldPosition(layout.path_to_box[0][0], layout.path_to_box[0][1]),
            Quaternion.identity);
        agent.name = "Agent";
        agent.transform.localScale = Vector3.one * (cellSize * 0.5f);

        spawnedObjects.Add(agent);

        // Follow path to box
        yield return StartCoroutine(FollowPath(agent, layout.path_to_box));

        // Pick up box (visual effect)
        agent.transform.localScale *= 1.2f;
        agent.GetComponent<Renderer>().material.color = Color.cyan; // Change color when carrying
        yield return new WaitForSeconds(0.5f);

        // Follow path to delivery (if available)
        if (layout.path_to_delivery != null && layout.path_to_delivery.Count > 0)
        {
            yield return StartCoroutine(FollowPath(agent, layout.path_to_delivery));

            // Deliver box (visual effect)
            agent.transform.localScale /= 1.2f;
            agent.GetComponent<Renderer>().material.color = Color.white; // Reset color
            yield return new WaitForSeconds(0.5f);

            Debug.Log("Delivery completed!");
        }
        else
        {
            Debug.Log("Reached box location - no delivery path specified");
        }
    }

    IEnumerator FollowPath(GameObject agent, List<List<int>> path)
    {
        if (path == null || path.Count <= 1) yield break;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 startPos = agent.transform.position;
            Vector3 targetPos = ToWorldPosition(path[i][0], path[i][1]);

            float journey = 0f;
            while (journey <= animationSpeed)
            {
                journey += Time.deltaTime;
                float percent = Mathf.Clamp01(journey / animationSpeed);

                // Smooth movement
                agent.transform.position = Vector3.Lerp(startPos, targetPos, percent);

                // Rotate towards movement direction
                if ((targetPos - startPos).magnitude > 0.1f)
                {
                    agent.transform.rotation = Quaternion.LookRotation(targetPos - startPos);
                }

                yield return null;
            }

            agent.transform.position = targetPos;
            yield return new WaitForSeconds(0.1f); // Brief pause at each node
        }
    }

    Vector3 ToWorldPosition(int gridX, int gridY)
    {
        return new Vector3(gridX * cellSize, 0f, gridY * cellSize);
    }

    void ClearScene()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    // Public method to restart the simulation
    public void RestartSimulation()
    {
        StopAllCoroutines();
        ClearScene();
        BuildWarehouse();
        StartCoroutine(SimulateAgentMovement());
    }

    // Optional: Draw debug grid in editor
    void OnDrawGizmos()
    {
        if (layout == null) return;

        Gizmos.color = Color.gray;
        for (int x = 0; x <= 15; x++)
        {
            Vector3 start = new Vector3(x * cellSize, 0, 0);
            Vector3 end = new Vector3(x * cellSize, 0, 10 * cellSize);
            Gizmos.DrawLine(start, end);
        }
        for (int y = 0; y <= 10; y++)
        {
            Vector3 start = new Vector3(0, 0, y * cellSize);
            Vector3 end = new Vector3(15 * cellSize, 0, y * cellSize);
            Gizmos.DrawLine(start, end);
        }
    }
}