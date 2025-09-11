using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking; // UnityWebRequest
using DoubleWay.MMM;         // <- usamos nuestros DTOs únicos

// ======================
// Script principal
// ======================
public class FactorySetup : MonoBehaviour
{
    [Header("Grid")]
    [Range(0.05f, 1f)] public float gridFraction = 1f / 3f;

    [Header("Referencias en escena")]
    public GameObject floor;
    public GameObject[] shelves;
    public GameObject[] machines;

    [Header("Marcadores discretos (en orden)")]
    public GameObject[] palletMarkers;   // pickups
    public GameObject[] deliveryMarkers; // drops (mismo orden que pickups)
    public GameObject[] agentMarkers;    // starts de agentes

    [Header("Red/envío")]
    public string uploadUrl = "http://localhost:8000/upload_factory";

    // cache grid
    float cellSize; int rows, cols; Vector2 originXZ; float width, height;

    void Start()
    {
        // medir piso y deducir grid
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

        var data = new MmmFactory {
            grid     = new MmmGrid { rows = rows, cols = cols, cellSize = cellSize, fraction = gridFraction, origin = originXZ },
            floor    = MakeObjData(floor),
            shelves  = MakeObjsData(shelves),
            machines = MakeObjsData(machines),
            agents   = MakeAgents(agentMarkers),                        // "agents": [{start:{...}}]
            pallets  = MakePallets(palletMarkers, deliveryMarkers)      // "pallets": [{pickup:{...}, drop:{...}}]
        };

        string json = JsonUtility.ToJson(data, true);
        Debug.Log("[FactorySetup] JSON generado =>\n" + json);
        StartCoroutine(SendFactoryData(json));
    }

    IEnumerator SendFactoryData(string json)
    {
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
        using (var req = new UnityWebRequest(uploadUrl, "POST")) {
            req.uploadHandler   = new UploadHandlerRaw(jsonBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogError("[FactorySetup] Error POST: " + req.error);
            else
                Debug.Log("[FactorySetup] Enviado OK → " + req.downloadHandler.text);
        }
    }

    // =============================
    // Builders
    // =============================
    MmmObject[] MakeObjsData(GameObject[] gos)
    {
        if (gos == null) return new MmmObject[0];
        var list = new List<MmmObject>();
        foreach (var go in gos) if (go) list.Add(MakeObjData(go));
        return list.ToArray();
    }

    MmmAgent[] MakeAgents(GameObject[] gos)
    {
        if (gos == null) return new MmmAgent[0];
        var list = new List<MmmAgent>();
        for (int i = 0; i < gos.Length; i++) {
            var go = gos[i]; if (!go) continue;
            list.Add(new MmmAgent { id = $"A{i}", start = MakePoint(go) });
        }
        return list.ToArray();
    }

    MmmPallet[] MakePallets(GameObject[] pickups, GameObject[] drops)
    {
        int n = Mathf.Min(pickups?.Length ?? 0, drops?.Length ?? 0);
        var list = new List<MmmPallet>(n);
        for (int i = 0; i < n; i++) {
            var pk = pickups[i]; var dr = drops[i];
            if (!pk || !dr) continue;
            list.Add(new MmmPallet { pickup = MakePoint(pk), drop = MakePoint(dr) });
        }
        if ((pickups?.Length ?? 0) != (drops?.Length ?? 0))
            Debug.LogWarning($"[FactorySetup] pickups({pickups?.Length ?? 0}) != drops({drops?.Length ?? 0}). Se exportan {n} pares pickup+drop.");
        return list.ToArray();
    }

    MmmObject MakeObjData(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        Bounds bb;
        if (rs != null && rs.Length > 0) {
            bb = new Bounds(rs[0].bounds.center, Vector3.zero);
            foreach (var r in rs) bb.Encapsulate(r.bounds);
        } else {
            var pt = MakePoint(go);
            return new MmmObject {
                name     = go.name,
                position = pt.position,
                gridPos  = pt.gridPos,
                gridRect = pt.gridRect,
                gridCells= pt.gridCells,
                size     = new Vector2(cellSize, cellSize),
                gridSize = new MmmGridSize{ w=1, h=1 }
            };
        }

        var od = new MmmObject();
        od.name     = go.name;
        od.position = new Vector2(bb.center.x, bb.center.z);
        od.size     = new Vector2(bb.size.x, bb.size.z);

        int cMin = WorldToCol(bb.min.x);
        int cMax = WorldToCol(bb.max.x - 1e-6f);
        int rMin = WorldToRow(bb.min.z);
        int rMax = WorldToRow(bb.max.z - 1e-6f);
        ClampRect(ref rMin, ref rMax, ref cMin, ref cMax);

        od.gridPos  = new MmmGridPos { row = Mathf.Clamp(WorldToRow(bb.center.z), 0, rows-1),
                                       col = Mathf.Clamp(WorldToCol(bb.center.x), 0, cols-1) };
        od.gridRect = new MmmGridRect{ rowMin=rMin, rowMax=rMax, colMin=cMin, colMax=cMax };
        od.gridCells= new List<MmmGridCell>();
        for (int r=rMin; r<=rMax; r++)
            for (int c=cMin; c<=cMax; c++)
                od.gridCells.Add(new MmmGridCell{ r=r, c=c });
        od.gridSize = new MmmGridSize{ w=(cMax-cMin+1), h=(rMax-rMin+1) };
        return od;
    }

    MmmPoint MakePoint(GameObject go)
    {
        int c = WorldToCol(go.transform.position.x);
        int r = WorldToRow(go.transform.position.z);
        c = Mathf.Clamp(c, 0, cols-1);
        r = Mathf.Clamp(r, 0, rows-1);
        return new MmmPoint {
            name     = go.name,
            position = new Vector2(go.transform.position.x, go.transform.position.z),
            gridPos  = new MmmGridPos { row=r, col=c },
            gridRect = new MmmGridRect { rowMin=r, rowMax=r, colMin=c, colMax=c },
            gridCells= new List<MmmGridCell> { new MmmGridCell{ r=r, c=c } }
        };
    }

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

// ======================
// DTOs ÚNICOS (namespace propio)
// ======================
namespace DoubleWay.MMM
{
    [System.Serializable] public class MmmGridCell { public int r; public int c; }
    [System.Serializable] public class MmmGridRect { public int rowMin, rowMax, colMin, colMax; }
    [System.Serializable] public class MmmGridPos  { public int row; public int col; }
    [System.Serializable] public class MmmGridSize { public int w; public int h; }

    [System.Serializable]
    public class MmmPoint {
        public string name;
        public Vector2 position;
        public MmmGridPos  gridPos;
        public MmmGridRect gridRect;
        public List<MmmGridCell> gridCells;
    }

    [System.Serializable]
    public class MmmPallet {
        public MmmPoint pickup;
        public MmmPoint drop;
    }

    [System.Serializable]
    public class MmmAgent {
        public string id;
        public MmmPoint start;
    }

    [System.Serializable]
    public class MmmGrid {
        public int rows, cols;
        public float cellSize;
        public float fraction;
        public Vector2 origin;
    }

    [System.Serializable]
    public class MmmObject {
        public string name;
        public Vector2 position;
        public Vector2 size;
        public MmmGridPos  gridPos;
        public MmmGridSize gridSize;
        public MmmGridRect gridRect;
        public List<MmmGridCell> gridCells;
    }

    [System.Serializable]
    public class MmmFactory {
        public MmmGrid   grid;
        public MmmObject floor;
        public MmmObject[] shelves;
        public MmmObject[] machines;
        public MmmAgent[]  agents;   // <- clave que mmm.py espera
        public MmmPallet[] pallets;  // <- pickup + drop por pallet
    }
}
