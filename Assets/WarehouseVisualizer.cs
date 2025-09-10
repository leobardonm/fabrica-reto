using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class WarehouseVisualizer : MonoBehaviour
{
    [System.Serializable]
    public class WarehouseData
    {
        public List<int[]> shelves;
        public List<int[]> machinery;
        public int[] delivery_point;
        public List<int[]> boxes;
        public List<int[]> path_to_box;
        public List<int[]> path_to_delivery;
    }

    public GameObject shelfPrefab;
    public GameObject machineryPrefab;
    public GameObject boxPrefab;
    public GameObject deliveryPointPrefab;
    public GameObject movingObject;
    public float moveSpeed = 2f;
    public float yOffset = 0.5f; // Para elevar los objetos del suelo

    private WarehouseData warehouseData;
    private string jsonFilePath = "output.json";

    void Start()
    {
        LoadJsonData();
        VisualizeWarehouse();
        StartCoroutine(AnimatePaths());
    }

    void LoadJsonData()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, jsonFilePath);

        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            warehouseData = JsonUtility.FromJson<WarehouseData>(jsonData);
            Debug.Log("JSON cargado correctamente");
        }
        else
        {
            Debug.LogError("No se pudo encontrar el archivo JSON en: " + filePath);
        }
    }

    void VisualizeWarehouse()
    {
        Debug.Log("Visualizing Warehouse!");
        // Colocar estantes
        Debug.Log("IS WAREHOUSE DATA NULL!" + warehouseData.shelves != null);
        Debug.Log("IS SHELF PREFAB NULL!" + shelfPrefab != null);

        if (warehouseData.shelves != null && shelfPrefab != null)
        {
            Debug.Log("IS WAREHOUSE DATA NULL!" + warehouseData.shelves != null);
            Debug.Log("IS SHELF PREFAB NULL!" + shelfPrefab != null);
            foreach (int[] shelfPos in warehouseData.shelves)
            {
                Vector3 position = new Vector3(shelfPos[0], yOffset, shelfPos[1]);
                Instantiate(shelfPrefab, position, Quaternion.identity, transform);
                Debug.Log("Prefab Spawned");
            }
        }

        // Colocar maquinaria
        if (warehouseData.machinery != null && machineryPrefab != null)
        {
            foreach (int[] machineryPos in warehouseData.machinery)
            {
                Vector3 position = new Vector3(machineryPos[0], yOffset, machineryPos[1]);
                Instantiate(machineryPrefab, position, Quaternion.identity, transform);
            }
        }

        // Colocar cajas
        if (warehouseData.boxes != null && boxPrefab != null)
        {
            foreach (int[] boxPos in warehouseData.boxes)
            {
                Vector3 position = new Vector3(boxPos[0], yOffset, boxPos[1]);
                Instantiate(boxPrefab, position, Quaternion.identity, transform);
            }
        }

        // Colocar punto de entrega
        if (warehouseData.delivery_point != null && deliveryPointPrefab != null)
        {
            Vector3 position = new Vector3(warehouseData.delivery_point[0], yOffset, warehouseData.delivery_point[1]);
            Instantiate(deliveryPointPrefab, position, Quaternion.identity, transform);
        }
    }

    IEnumerator AnimatePaths()
    {
        if (movingObject == null) yield break;

        // Esperar un frame para asegurar que todo está inicializado
        yield return null;

        // Primero mover hacia la caja
        if (warehouseData.path_to_box != null && warehouseData.path_to_box.Count > 0)
        {
            foreach (int[] point in warehouseData.path_to_box)
            {
                Vector3 targetPosition = new Vector3(point[0], yOffset, point[1]);

                while (Vector3.Distance(movingObject.transform.position, targetPosition) > 0.05f)
                {
                    movingObject.transform.position = Vector3.MoveTowards(
                        movingObject.transform.position,
                        targetPosition,
                        moveSpeed * Time.deltaTime
                    );
                    yield return null;
                }
            }
        }

        // Luego mover hacia el punto de entrega
        if (warehouseData.path_to_delivery != null && warehouseData.path_to_delivery.Count > 0)
        {
            foreach (int[] point in warehouseData.path_to_delivery)
            {
                Vector3 targetPosition = new Vector3(point[0], yOffset, point[1]);

                while (Vector3.Distance(movingObject.transform.position, targetPosition) > 0.05f)
                {
                    movingObject.transform.position = Vector3.MoveTowards(
                        movingObject.transform.position,
                        targetPosition,
                        moveSpeed * Time.deltaTime
                    );
                    yield return null;
                }
            }
        }
    }

    // Para debug: visualizar las rutas en el editor
    void OnDrawGizmos()
    {
        if (warehouseData == null) return;

        // Dibujar ruta hacia la caja
        if (warehouseData.path_to_box != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < warehouseData.path_to_box.Count - 1; i++)
            {
                Vector3 start = new Vector3(warehouseData.path_to_box[i][0], 0, warehouseData.path_to_box[i][1]);
                Vector3 end = new Vector3(warehouseData.path_to_box[i + 1][0], 0, warehouseData.path_to_box[i + 1][1]);
                Gizmos.DrawLine(start, end);
            }
        }

        // Dibujar ruta hacia entrega
        if (warehouseData.path_to_delivery != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < warehouseData.path_to_delivery.Count - 1; i++)
            {
                Vector3 start = new Vector3(warehouseData.path_to_delivery[i][0], 0, warehouseData.path_to_delivery[i][1]);
                Vector3 end = new Vector3(warehouseData.path_to_delivery[i + 1][0], 0, warehouseData.path_to_delivery[i + 1][1]);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}