using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// calcula sobre que se apoya un mueble: el plano o la tapa de otro mueble
public static class Apoyo
{
    static ARRaycastManager raycastManager;
    static readonly List<ARRaycastHit> hitsAR = new List<ARRaycastHit>();
    static readonly RaycastHit[] hits = new RaycastHit[24];
    static float ultimoPiso;
    static bool hayPiso;

    public static float AlturaPlano(Vector3 pos)
    {
        if (raycastManager == null) raycastManager = Object.FindAnyObjectByType<ARRaycastManager>();
        if (raycastManager != null &&
            raycastManager.Raycast(new Ray(pos + Vector3.up * 1f, Vector3.down), hitsAR, TrackableType.PlaneWithinPolygon))
        {
            ultimoPiso = hitsAR[0].pose.position.y;
            hayPiso = true;
        }
        return hayPiso ? ultimoPiso : pos.y;
    }

    public static void AvisarPiso(float y)
    {
        ultimoPiso = y;
        hayPiso = true;
    }

    public static float Calcular(Vector3 pos, Mueble ignorar)
    {
        float piso = AlturaPlano(pos);
        float mejor = piso;
        var origen = new Vector3(pos.x, piso + 3f, pos.z);
        int n = Physics.RaycastNonAlloc(origen, Vector3.down, hits, 3.2f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var h = hits[i];
            if (h.normal.y < 0.7f) continue;
            var m = h.collider.GetComponentInParent<Mueble>();
            if (m == null || m == ignorar || !m.permiteApilar || m.agarrado) continue;
            if (ignorar != null && h.collider.transform.IsChildOf(ignorar.transform)) continue;
            if (h.point.y > mejor) mejor = h.point.y;
        }
        return mejor;
    }

    // despues de borrar algo, lo que estaba arriba baja
    public static void ReacomodarTodos(Transform contenedor)
    {
        var lista = new List<Mueble>();
        foreach (Transform hijo in contenedor)
        {
            var m = hijo.GetComponent<Mueble>();
            if (m != null && hijo.gameObject.activeInHierarchy && !m.agarrado) lista.Add(m);
        }
        lista.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
        foreach (var m in lista) m.Reacomodar();
    }
}
