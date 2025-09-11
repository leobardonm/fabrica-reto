using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking; // <-- para UnityWebRequest

[System.Serializable] public class GridCell { public int r; public int c; }
[System.Serializable] public class GridRect { public int rowMin, rowMax, colMin, colMax; }
[System.Serializable] public class GridPos { public int row; public int col; }
[System.Serializable] public class GridSize { public int w; public int h; }

[System.Serializable]
public class ObjectData
{
    public string name;
    public Vector2 position;     // mundo XZ (centro transform)
    public Vector2 size;         // mundo XZ (bounds)
    public GridPos gridPos;      // celda del centro
    public GridSize gridSize;    // tamaño en celdas (ancho x alto)
    public GridRect gridRect;    // rectángulo en celdas (inclusivo)
    public List<GridCell> gridCells; // celdas ocupadas
}

[System.Serializable]
public class GridData
{
    public int rows, cols;
    public float cellSize;
    public float fraction;
    public Vector2 origin;  // esquina min del piso (x,z)
}

[System.Serializable]
public class FactoryData
{
    public GridData grid;
    public ObjectData floor;
    public ObjectData[] shelves;
    public ObjectData[] machines;
}

public class FactorySetup : MonoBehaviour
{
    [Range(0.05f, 1f)] public float gridFraction = 1f / 3f;
    public GameObject floor;
    public GameObject[] shelves;
    public GameObject[] machines;

    float cellSize;
    int rows, cols;
    Vector2 originXZ;
    float width, height;

    void Start()
    {
        // ---- medir piso y definir grid ----
        var fr = floor.GetComponent<Renderer>();
        var fb = fr.bounds;
        width = fb.size.x;
        height = fb.size.z;
        originXZ = new Vector2(fb.min.x, fb.min.z);

        int targetCols = Mathf.Max(1, Mathf.RoundToInt(width * gridFraction));
        int targetRows = Mathf.Max(1, Mathf.RoundToInt(height * gridFraction));
        float cellX = width / targetCols;
        float cellZ = height / targetRows;
        cellSize = Mathf.Min(cellX, cellZ);
        cols = Mathf.Max(1, Mathf.FloorToInt(width / cellSize));
        rows = Mathf.Max(1, Mathf.FloorToInt(height / cellSize));

        var data = new FactoryData
        {
            grid = new GridData { rows = rows, cols = cols, cellSize = cellSize, fraction = gridFraction, origin = originXZ },
            floor = MakeObjData(floor),
            shelves = MakeObjsData(shelves),
            machines = MakeObjsData(machines)
        };

        string json = JsonUtility.ToJson(data, true);
        StartCoroutine(SendFactoryData(json));  // <<--- enviar JSON al backend
    }

    IEnumerator SendFactoryData(string json)
    {
        string url = "http://localhost:8000/upload_factory"; // Cambia si usas otro host/puerto
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError("Error al enviar datos: " + request.error);
        else
            Debug.Log("FactoryData enviado correctamente: " + request.downloadHandler.text);
    }

    ObjectData[] MakeObjsData(GameObject[] gos)
    {
        var list = new List<ObjectData>();
        foreach (var go in gos) if (go) list.Add(MakeObjData(go));
        return list.ToArray();
    }

    ObjectData MakeObjData(GameObject go)
    {
        var od = new ObjectData();
        od.name = go.name;
        od.position = new Vector2(go.transform.position.x, go.transform.position.z);

        Bounds bb;
        bool ok = GetWorldBounds(go, out bb);

        Vector2 worldSize = ok ? new Vector2(bb.size.x, bb.size.z) : new Vector2(cellSize, cellSize);
        if (worldSize.x <= 1e-5f && worldSize.y <= 1e-5f)
        {
            worldSize = new Vector2(cellSize, cellSize);
        }
        od.size = worldSize;

        float eps = 1e-4f;
        int cMin = WorldToCol(bb.min.x);
        int cMax = WorldToCol((ok ? bb.max.x : (od.position.x + cellSize * 0.5f)) - eps);
        int rMin = WorldToRow(bb.min.z);
        int rMax = WorldToRow((ok ? bb.max.z : (od.position.y + cellSize * 0.5f)) - eps);
        ClampRect(ref rMin, ref rMax, ref cMin, ref cMax);

        int wCells = Mathf.Max(1, cMax - cMin + 1);
        int hCells = Mathf.Max(1, rMax - rMin + 1);
        od.gridSize = new GridSize { w = wCells, h = hCells };

        Vector2 centerXZ = ok ? new Vector2(bb.center.x, bb.center.z) : od.position;
        int cCenter = WorldToCol(centerXZ.x);
        int rCenter = WorldToRow(centerXZ.y);
        cCenter = Mathf.Clamp(cCenter, 0, cols - 1);
        rCenter = Mathf.Clamp(rCenter, 0, rows - 1);
        od.gridPos = new GridPos { row = rCenter, col = cCenter };

        od.gridRect = new GridRect { rowMin = rMin, rowMax = rMax, colMin = cMin, colMax = cMax };
        od.gridCells = new List<GridCell>();
        for (int r = rMin; r <= rMax; r++)
            for (int c = cMin; c <= cMax; c++)
                od.gridCells.Add(new GridCell { r = r, c = c });

        return od;
    }

    bool GetWorldBounds(GameObject go, out Bounds outBounds)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        bool has = false;
        outBounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in renderers)
        {
            if (!has) { outBounds = r.bounds; has = true; }
            else outBounds.Encapsulate(r.bounds);
        }
        if (!has)
        {
            var colls = go.GetComponentsInChildren<Collider>(true);
            foreach (var c in colls)
            {
                if (!has) { outBounds = c.bounds; has = true; }
                else outBounds.Encapsulate(c.bounds);
            }
        }
        return has;
    }

    int WorldToCol(float x)
    {
        return Mathf.FloorToInt((x - originXZ.x) / cellSize);
    }
    int WorldToRow(float z)
    {
        return Mathf.FloorToInt((z - originXZ.y) / cellSize);
    }
    void ClampRect(ref int rMin, ref int rMax, ref int cMin, ref int cMax)
    {
        rMin = Mathf.Clamp(rMin, 0, rows - 1);
        rMax = Mathf.Clamp(rMax, 0, rows - 1);
        cMin = Mathf.Clamp(cMin, 0, cols - 1);
        cMax = Mathf.Clamp(cMax, 0, cols - 1);
        if (rMax < rMin) rMax = rMin;
        if (cMax < cMin) cMax = cMin;
    }
}
