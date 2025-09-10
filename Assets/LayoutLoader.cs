using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class LayoutDTO
{
    public List<List<int>> shelves;
    public List<List<int>> machinery;
    public List<int> delivery_point;
    public List<List<int>> boxes;
    public List<List<int>> path_to_box;
    public List<List<int>> path_to_delivery;
}

public class LayoutLoader : MonoBehaviour
{
    [Header("JSON Layout (en StreamingAssets)")]
    public string fileName = "layout.json";

    [Header("Prefabs")]
    public GameObject shelfPrefab;
    public GameObject machinePrefab;
    public GameObject deliveryPrefab;
    public GameObject boxPrefab;
    public GameObject pathPrefab;

    [Header("Grid Settings")]
    public float cellSize = 1f;

    private LayoutDTO layout;

    IEnumerator Start()
    {
        yield return LoadLayoutAsync();
        if (layout == null)
        {
            Debug.LogError("Layout vacío o inválido.");
            yield break;
        }

        BuildScene();
    }

    IEnumerator LoadLayoutAsync()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

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
                Debug.LogError("Error al leer JSON: " + req.error + "\nPath: " + path);
                yield break;
            }

            string json = req.downloadHandler.text;
            layout = JsonUtility.FromJson<LayoutDTO>(json);
        }
    }

    void BuildScene()
    {
        // Spawn shelves
        foreach (var pos in layout.shelves)
        {
            Vector3 wpos = ToWorld(pos[0], pos[1]);
            InstantiatePrefab(shelfPrefab, wpos, "Shelf");
        }

        // Spawn machinery
        foreach (var pos in layout.machinery)
        {
            Vector3 wpos = ToWorld(pos[0], pos[1]);
            InstantiatePrefab(machinePrefab, wpos, "Machine");
        }

        // Delivery point
        if (layout.delivery_point != null && layout.delivery_point.Count == 2)
        {
            Vector3 wpos = ToWorld(layout.delivery_point[0], layout.delivery_point[1]);
            InstantiatePrefab(deliveryPrefab, wpos, "DeliveryPoint");
        }

        // Boxes
        foreach (var pos in layout.boxes)
        {
            Vector3 wpos = ToWorld(pos[0], pos[1]);
            InstantiatePrefab(boxPrefab, wpos, "Box");
        }

        // Path to box
        foreach (var pos in layout.path_to_box)
        {
            Vector3 wpos = ToWorld(pos[0], pos[1]);
            InstantiatePrefab(pathPrefab, wpos, "PathToBox");
        }

        // Path to delivery
        foreach (var pos in layout.path_to_delivery)
        {
            Vector3 wpos = ToWorld(pos[0], pos[1]);
            InstantiatePrefab(pathPrefab, wpos, "PathToDelivery");
        }
    }

    GameObject InstantiatePrefab(GameObject prefab, Vector3 position, string fallbackName)
    {
        GameObject go;
        if (prefab != null)
            go = Instantiate(prefab, position, Quaternion.identity);
        else
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);

        go.transform.localScale = Vector3.one * (cellSize * 0.8f);
        go.name = fallbackName;
        return go;
    }

    Vector3 ToWorld(int gx, int gy)
    {
        return new Vector3(gx * cellSize, 0f, gy * cellSize);
    }
}
