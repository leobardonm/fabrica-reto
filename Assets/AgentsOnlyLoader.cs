using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking; // UnityWebRequest

// === Tipos básicos de grid ===
[System.Serializable] public class GridCell { public int r; public int c; }
[System.Serializable] public class GridRect { public int rowMin, rowMax, colMin, colMax; }
[System.Serializable] public class GridPos  { public int row; public int col; }
[System.Serializable] public class GridSize { public int w; public int h; }

// === Payload EXACTO como lo espera mmm.py ===
[System.Serializable]
public class PointData  // punto/region genérica (1 celda en nuestro caso)
{
    public string name;
    public Vector2 position;     // mundo XZ (centro)
    public GridPos gridPos;      // celda del centro
    public GridRect gridRect;    // rect (1x1)
    public List<GridCell> gridCells; // [{r,c}] (1 celda)
}

[System.Serializable]
public class PalletEntry  // cada pallet con su pickup y drop
{
    public PointData pickup;
    public PointData drop;
}

[System.Serializable]
public class AgentEntry   // cada agente con su start
{
    public string id;      // opcional
    public PointData start;
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
public class ObjectData    // para shelves/machines/floor (huella en celdas)
{
    public string name;
    public Vector2 position;  // mundo XZ (centro)
    public Vector2 size;      // mundo XZ (ancho x alto de huella)
    public GridPos gridPos;   // celda del centro
    public GridSize gridSize; // tamaño en celdas
    public GridRect gridRect; // rect celdas
    public List<GridCell> gridCells; // todas las celdas ocupadas
}

[System.Serializable]
public class FactoryData
{
    public GridData grid;
    public ObjectData floor;
    public ObjectData[] shelves;
    public ObjectData[] machines;

    // === claves que mmm.py busca ===
    public AgentEntry[] agents;   // starts de agentes
    public PalletEntry[] pallets; // cada pallet con pickup+drop
}

public class FactorySetup_MMM : MonoBehaviour
{
    [Header("Grid")]
    [Range(0.05f, 1f)] public float gridFraction = 1f / 3f;

    [Header("Referencias en escena")]
    public GameObject floor;           // piso principal
    public GameObject[] shelves;       // estantes/obstáculos
    public GameObject[] machines;      // maquinaria/obstáculos

    [Header("Marcadores discretos")]
    public GameObject[] palletMarkers;   // N pickups
    public GameObject[] deliveryMarkers; // N drops (1:1 con pallets)
    public GameObject[] agentMarkers;    // M agentes (starts)

    [Header("Red/envío")]
    public string uploadUrl = "http://localhost:8000/upload_factory"; // FastAPI

    // cache grid
    float cellSize; int rows, cols; Vector2 originXZ; float width, height;

    void Start()
    {
        // 1) medir piso y deducir grid
        var fr = floor.GetComponent<Renderer>();
        var fb = fr.bounds;
        width   = fb.size.x;
        height  = fb.size.z;
        originXZ = new Vector2(fb.min.x, fb.min.z);

        int targetCols = Mathf.Max(1, Mathf.RoundToInt(width  * gridFraction));
        int targetRows = Mathf.Max(1, Mathf.RoundToInt(height * gridFraction));
        float cellX = width  / targetCols;
        float cellZ = height / targetRows;
        cellSize = Mathf.Min(cellX, cellZ);
        cols = Mathf.Max(1, Mathf.FloorToInt(width  / cellSize));
        rows = Mathf.Max(1, Mathf.FloorToInt(height / cellSize));

        // 2) construir payload EXACTO
        var data = new FactoryData
        {
            grid = new GridData { rows = rows, cols = cols, cellSize = cellSize, fraction = gridFraction, origin = originXZ },
            floor = MakeObjData(floor),
            shelves = MakeObjsData(shelves),
            machines = MakeObjsData(machines),
            agents = MakeAgents(agentMarkers),                 // "agents": [{start:{...}}]
            pallets = MakePallets(palletMarkers, deliveryMarkers) // "pallets": [{pickup:{...}, drop:{...}}]
        };

        // 3) validación rápida
        if (data.pallets == null || data.pallets.Length == 0)
            Debug.LogWarning("[FactorySetup_MMM] No hay pallets (pickup+drop). mmm.py generará aleatorios.");
        if (data.agents == null || data.agents.Length == 0)
            Debug.LogWarning("[FactorySetup_MMM] No hay agents/start definidos. mmm.py usará AGENTS env.");

        // 4) enviar
        string json = JsonUtility.ToJson(data, true);
        Debug.Log("[FactorySetup_MMM] JSON listo para mmm.py =>\n" + json);
        StartCoroutine(SendFactoryData(json));
    }

    IEnumerator SendFactoryData(string json)
    {
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
        UnityWebRequest request = new UnityWebRequest(uploadUrl, "POST");
        request.uploadHandler   = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError("[FactorySetup_MMM] Error POST: " + request.error);
        else
            Debug.Log("[FactorySetup_MMM] Enviado OK → " + request.downloadHandler.text);
    }

    // =============================
    // Builders
    // =============================
    ObjectData[] MakeObjsData(GameObject[] gos)
    {
        if (gos == null) return new ObjectData[0];
        var list = new List<ObjectData>();
        foreach (var go in gos) if (go) list.Add(MakeObjData(go));
        return list.ToArray();
    }

    AgentEntry[] MakeAgents(GameObject[] gos)
    {
        if (gos == null) return new AgentEntry[0];
        var list = new List<AgentEntry>();
        for (int i = 0; i < gos.Length; i++)
        {
            var go = gos[i]; if (!go) continue;
            var a = new AgentEntry();
            a.id = $"A{i}"; // opcional
            a.start = MakePoint(go);
            list.Add(a);
        }
        return list.ToArray();
    }

    PalletEntry[] MakePallets(GameObject[] pickups, GameObject[] drops)
    {
        int nP = pickups != null ? pickups.Length : 0;
        int nD = drops   != null ? drops.Length   : 0;
        int n = Mathf.Min(nP, nD);
        var list = new List<PalletEntry>(n);

        for (int i = 0; i < n; i++)
        {
            var pk = pickups[i]; var dr = drops[i];
            if (!pk || !dr) continue;
            list.Add(new PalletEntry { pickup = MakePoint(pk), drop = MakePoint(dr) });
        }
        if (nP != nD)
        {
            Debug.LogWarning($"[FactorySetup_MMM] pickups({nP}) != drops({nD}). Se exportan {n} pares pickup+drop.");
        }
        return list.ToArray();
    }

    // --- Objetos de huella (shelves/machines/floor) ---
    ObjectData MakeObjData(GameObject go)
    {
        // Bounds mundo del renderer (AABB), suficiente para cubrir footprint en celdas
        var rs = go.GetComponentsInChildren<Renderer>(true);
        Bounds bb;
        if (rs != null && rs.Length > 0)
        {
            bb = new Bounds(rs[0].bounds.center, Vector3.zero);
            for (int i = 0; i < rs.Length; i++) bb.Encapsulate(rs[i].bounds);
        }
        else
        {
            // Sin renderers → tratar como punto
            var pt = MakePoint(go);
            return new ObjectData {
                name = go.name,
                position = pt.position,
                gridPos  = pt.gridPos,
                gridRect = pt.gridRect,
                gridCells= pt.gridCells,
                size = new Vector2(cellSize, cellSize),
                gridSize = new GridSize{ w=1, h=1 }
            };
        }

        var od = new ObjectData();
        od.name     = go.name;
        od.position = new Vector2(bb.center.x, bb.center.z);
        od.size     = new Vector2(bb.size.x, bb.size.z);

        // Rect en celdas
        int cMin = WorldToCol(bb.min.x);
        int cMax = WorldToCol(bb.max.x - 1e-6f);
        int rMin = WorldToRow(bb.min.z);
        int rMax = WorldToRow(bb.max.z - 1e-6f);
        ClampRect(ref rMin, ref rMax, ref cMin, ref cMax);

        int cCenter = WorldToCol(bb.center.x);
        int rCenter = WorldToRow(bb.center.z);
        cCenter = Mathf.Clamp(cCenter, 0, cols-1);
        rCenter = Mathf.Clamp(rCenter, 0, rows-1);
        od.gridPos = new GridPos{ row = rCenter, col = cCenter };

        od.gridRect = new GridRect{ rowMin = rMin, rowMax = rMax, colMin = cMin, colMax = cMax };
        od.gridCells = new List<GridCell>();
        for (int r = rMin; r <= rMax; r++)
            for (int c = cMin; c <= cMax; c++)
                od.gridCells.Add(new GridCell{ r=r, c=c });

        od.gridSize = new GridSize{ w = (cMax-cMin+1), h = (rMax-rMin+1)};
        return od;
    }

    // --- Punto (1 celda) para pickup/drop/start ---
    PointData MakePoint(GameObject go)
    {
        var pd = new PointData();
        pd.name = go.name;
        pd.position = new Vector2(go.transform.position.x, go.transform.position.z);

        int c = WorldToCol(go.transform.position.x);
        int r = WorldToRow(go.transform.position.z);
        c = Mathf.Clamp(c, 0, cols-1);
        r = Mathf.Clamp(r, 0, rows-1);
        pd.gridPos = new GridPos{ row=r, col=c };
        pd.gridRect = new GridRect{ rowMin=r, rowMax=r, colMin=c, colMax=c };
        pd.gridCells = new List<GridCell>{ new GridCell{ r=r, c=c } };
        return pd;
    }

    // --- Grid helpers ---
    int WorldToCol(float x) { return Mathf.FloorToInt((x - originXZ.x) / cellSize); }
    int WorldToRow(float z) { return Mathf.FloorToInt((z - originXZ.y) / cellSize); }
    void ClampRect(ref int rMin, ref int rMax, ref int cMin, ref int cMax)
    {
        rMin = Mathf.Clamp(rMin, 0, rows-1);
        rMax = Mathf.Clamp(rMax, 0, rows-1);
        cMin = Mathf.Clamp(cMin, 0, cols-1);
        cMax = Mathf.Clamp(cMax, 0, cols-1);
        if (rMax < rMin) rMax = rMin;
        if (cMax < cMin) cMax = cMin;
    }
}
