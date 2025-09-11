using UnityEngine;
using System.Collections;

public class AgenteController : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject agentePrefab;
    public GameObject agenteConCajaPrefab;
    public GameObject cajaPrefab;

    [Header("Parámetros")]
    public float stepTime = 1f;      // duración de cada movimiento/giro (segundos)
    public float x = 3f;             // distancia de ida/vuelta en cada dirección cardinal
    public float pickupDistance = 1f;// cuánto avanza para recoger/soltar la caja
    public float afterActionDelay = 0.15f; // pequeña pausa visual entre acciones
    public float visibleBoxOffset = 0.6f;  // cuánto delante aparece la caja (visual)

    GameObject agente;
    bool cargando = false;

    // Mantener la rotación "base" para las 4 direcciones (orientación absoluta)
    Quaternion baseRotation;

    void Start()
    {
        if (agentePrefab == null || agenteConCajaPrefab == null || cajaPrefab == null)
        {
            Debug.LogError("Asigna los prefabs Agente, AgenteConCaja y Caja en el inspector.");
            enabled = false;
            return;
        }

        // Instanciar el agente en la posición del manager
        agente = Instantiate(agentePrefab, transform.position, transform.rotation);

        // Guardamos la rotación base (la orientación "0" de las direcciones cardinales)
        baseRotation = agente.transform.rotation;

        StartCoroutine(LoopPrincipal());
    }

    IEnumerator LoopPrincipal()
    {
        while (true)
        {
            // --- 1) Barrido por las 4 direcciones cardinales ---
            for (int dir = 0; dir < 4; dir++)
            {
                // Rotar a la orientación absoluta correspondiente (0,90,180,270) respecto a baseRotation
                yield return StartCoroutine(RotarAIndice(dir));

                // Hacer ida y vuelta en esa dirección (sin rotar 180): avanzar x y regresar
                yield return StartCoroutine(MoverIdaVueltaCardinal(dir));
            }

            // Después de haber cubierto las 4 direcciones, volver a la orientación inicial antes del pickup/drop
            yield return StartCoroutine(RotarAIndice(0));
            yield return new WaitForSeconds(afterActionDelay);

            // --- 2) PICKUP / DROP ---
            if (!cargando)
            {
                // Avanza un poco para "alcanzar" la caja
                yield return StartCoroutine(Mover(pickupDistance));
                yield return new WaitForSeconds(afterActionDelay);

                // Instanciar la caja en frente (visible antes de que el agente la "recoga")
                Vector3 cajaPos = agente.transform.position + agente.transform.forward * visibleBoxOffset;
                GameObject cajaVisible = Instantiate(cajaPrefab, cajaPos, Quaternion.identity);

                // Tiempo para ver la caja antes de ser recogida
                yield return new WaitForSeconds(stepTime * 0.5f);

                // Reemplazar agente por AgenteConCaja (la caja visual se elimina)
                Vector3 pos = agente.transform.position;
                Quaternion rot = agente.transform.rotation;
                Destroy(agente);
                if (cajaVisible != null) Destroy(cajaVisible);

                agente = Instantiate(agenteConCajaPrefab, pos, rot);
                // no cambiamos baseRotation: las direcciones siguen ancladas a la rotación inicial original
                cargando = true;

                yield return new WaitForSeconds(afterActionDelay);
            }
            else
            {
                // Si ya está cargando -> soltar: avanzar, instanciar caja en escena y reemplazar agente por sin-caja
                yield return StartCoroutine(Mover(pickupDistance));
                yield return new WaitForSeconds(afterActionDelay);

                Vector3 dropPos = agente.transform.position + agente.transform.forward * visibleBoxOffset;
                Instantiate(cajaPrefab, dropPos, Quaternion.identity);

                // Pequeña espera para visual
                yield return new WaitForSeconds(stepTime * 0.5f);

                Vector3 pos = agente.transform.position;
                Quaternion rot = agente.transform.rotation;
                Destroy(agente);

                agente = Instantiate(agentePrefab, pos, rot);
                cargando = false;

                yield return new WaitForSeconds(afterActionDelay);
            }

            // luego el while(true) repite todo otra vez
        }
    }

    // ---- MOVIMIENTOS Y GIROS ----

    // Rota el agente a la orientación absoluta indexada (0..3 => 0°,90°,180°,270° respecto a baseRotation)
    IEnumerator RotarAIndice(int index)
    {
        EnsureAgente();
        Quaternion start = agente.transform.rotation;
        Quaternion target = baseRotation * Quaternion.Euler(0f, 90f * index, 0f);

        float elapsed = 0f;
        while (elapsed < stepTime)
        {
            float t = Mathf.Clamp01(elapsed / stepTime);
            agente.transform.rotation = Quaternion.Slerp(start, target, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        agente.transform.rotation = target;
        yield return new WaitForSeconds(afterActionDelay);
    }

    // Mover ida y vuelta en la dirección cardinal 'index' sin rotar 180 (avanza en forward absoluto y regresa)
    IEnumerator MoverIdaVueltaCardinal(int index)
    {
        EnsureAgente();
        // Dirección absoluta basada en baseRotation
        Quaternion dirRot = baseRotation * Quaternion.Euler(0f, 90f * index, 0f);
        Vector3 startPos = agente.transform.position;
        Vector3 endPos = startPos + dirRot * Vector3.forward * x;

        // Ida
        float elapsed = 0f;
        while (elapsed < stepTime)
        {
            float t = Mathf.Clamp01(elapsed / stepTime);
            agente.transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        agente.transform.position = endPos;
        yield return new WaitForSeconds(afterActionDelay);

        // Regreso
        elapsed = 0f;
        while (elapsed < stepTime)
        {
            float t = Mathf.Clamp01(elapsed / stepTime);
            agente.transform.position = Vector3.Lerp(endPos, startPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        agente.transform.position = startPos;
        yield return new WaitForSeconds(afterActionDelay);
    }

    // Mover 'distancia' hacia adelante respecto a la rotación actual del agente (su forward)
    IEnumerator Mover(float distancia)
    {
        EnsureAgente();
        Vector3 startPos = agente.transform.position;
        Vector3 endPos = startPos + agente.transform.forward * distancia;

        float elapsed = 0f;
        while (elapsed < stepTime)
        {
            float t = Mathf.Clamp01(elapsed / stepTime);
            agente.transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        agente.transform.position = endPos;
        yield return null;
    }

    void EnsureAgente()
    {
        if (agente == null)
        {
            Debug.LogError("El agente es null. Asegúrate de asignar los prefabs y que no se haya destruido inesperadamente.");
        }
    }
}
