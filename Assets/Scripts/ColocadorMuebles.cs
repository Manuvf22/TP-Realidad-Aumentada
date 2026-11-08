using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

// muestra el fantasma del mueble elegido y lo coloca al soltar el dedo
[DefaultExecutionOrder(-200)]
public class ColocadorMuebles : MonoBehaviour
{
    public ObjectSpawner spawner;
    public ARRaycastManager raycastManager;
    public XRInteractionGroup grupoInteraccion;
    public Material materialFantasma;
    public Color colorValido = new Color(0.25f, 1f, 0.45f, 0.45f);
    public Color colorInvalido = new Color(1f, 0.25f, 0.2f, 0.45f);

    public bool hayLugar { get; private set; }
    public bool lugarValido { get; private set; }
    public bool arrastrando => tocandoVacio && !cancelado;

    GameObject prefabActual;
    Mueble datosPrefab;
    Transform fantasma;
    Renderer[] rendsFantasma;
    Bounds boundsFantasma;
    MaterialPropertyBlock mpb;
    bool fantasmaVisible;

    bool tocandoVacio, cancelado;
    Vector2 ultimoPunto;
    float pendienteApoyo;
    readonly List<ARRaycastHit> hitsAR = new List<ARRaycastHit>();
    readonly List<RaycastResult> resultadosUI = new List<RaycastResult>();
    readonly Collider[] solapados = new Collider[16];
    PointerEventData datosUI;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        spawner.onlySpawnInView = false;
        spawner.objectSpawned += AlAparecer;
    }

    void OnDestroy()
    {
        if (spawner != null) spawner.objectSpawned -= AlAparecer;
    }

    void OnDisable()
    {
        MostrarFantasma(false);
        tocandoVacio = false;
    }

    public void SetPrefab(GameObject prefab)
    {
        if (prefab == prefabActual) return;
        prefabActual = prefab;
        datosPrefab = prefab != null ? prefab.GetComponent<Mueble>() : null;
        ArmarFantasma();
    }

    // copia solo las mallas del prefab, sin colliders ni interactables
    void ArmarFantasma()
    {
        if (fantasma != null) Destroy(fantasma.gameObject);
        fantasmaVisible = false;
        if (prefabActual == null) return;

        fantasma = new GameObject("Fantasma").transform;
        var raiz = prefabActual.transform;
        var origen = datosPrefab != null && datosPrefab.visuales != null ? datosPrefab.visuales : raiz;
        var aLocal = raiz.worldToLocalMatrix;
        var lista = new List<Renderer>();
        foreach (var mf in origen.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var m = aLocal * mf.transform.localToWorldMatrix;
            var go = new GameObject(mf.name);
            go.transform.SetParent(fantasma, false);
            go.transform.localPosition = m.GetColumn(3);
            go.transform.localRotation = m.rotation;
            go.transform.localScale = m.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = materialFantasma;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            lista.Add(r);
        }
        rendsFantasma = lista.ToArray();
        boundsFantasma = Mueble.BoundsDe(raiz, origen);
        fantasma.gameObject.SetActive(false);
    }

    void MostrarFantasma(bool valor)
    {
        if (fantasma == null) return;
        if (fantasma.gameObject.activeSelf != valor) fantasma.gameObject.SetActive(valor);
        if (!valor) fantasmaVisible = false;
    }

    void Update()
    {
        var cam = Camera.main;
        if (cam == null || fantasma == null) return;

        bool haySeleccion = grupoInteraccion != null && grupoInteraccion.focusInteractable != null;
        var centro = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        LeerEntrada(out bool empezo, out bool presionado, out bool solto, out int dedos, out Vector2 pos);

        if (empezo)
        {
            // si ya hay algo seleccionado, el toque sirve para deseleccionar
            tocandoVacio = !haySeleccion && !SobreUI(pos) && !SobreMueble(pos, cam);
            cancelado = false;
        }
        if (dedos > 1) cancelado = true;
        if (presionado) ultimoPunto = pos;

        Vector2 punto = arrastrando ? ultimoPunto : centro;

        if (haySeleccion && !arrastrando)
        {
            MostrarFantasma(false);
            hayLugar = false;
            if (solto) tocandoVacio = false;
            return;
        }

        hayLugar = BuscarLugar(punto, cam, out Vector3 lugar, out float apoyo);
        if (!hayLugar)
        {
            MostrarFantasma(false);
            lugarValido = false;
            if (solto) tocandoVacio = false;
            return;
        }

        float extra = datosPrefab != null ? datosPrefab.alturaInicial : 0f;
        var objetivo = new Vector3(lugar.x, apoyo + extra, lugar.z);
        var adelante = Vector3.ProjectOnPlane(cam.transform.position - objetivo, Vector3.up);
        var rot = adelante.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(adelante, Vector3.up) : Quaternion.identity;

        MostrarFantasma(true);
        if (!fantasmaVisible)
        {
            fantasma.SetPositionAndRotation(objetivo, rot);
            fantasmaVisible = true;
        }
        else
        {
            // sigue al dedo suavizado
            float k = 1f - Mathf.Exp(-18f * Time.deltaTime);
            fantasma.SetPositionAndRotation(Vector3.Lerp(fantasma.position, objetivo, k), Quaternion.Slerp(fantasma.rotation, rot, k));
        }

        lugarValido = !Solapa(objetivo, rot);
        var color = lugarValido ? colorValido : colorInvalido;
        color.a *= 0.8f + Mathf.Sin(Time.time * 6f) * 0.2f;
        mpb.SetColor("_BaseColor", color);
        foreach (var r in rendsFantasma) r.SetPropertyBlock(mpb);

        if (solto && arrastrando)
        {
            if (lugarValido)
            {
                pendienteApoyo = apoyo;
                spawner.TrySpawnObject(new Vector3(lugar.x, apoyo, lugar.z), Vector3.up);
            }
            else Feedback.Vibrar(60);
        }
        if (solto) tocandoVacio = false;
    }

    void LeerEntrada(out bool empezo, out bool presionado, out bool solto, out int dedos, out Vector2 pos)
    {
        empezo = presionado = solto = false;
        dedos = 0;
        pos = default;

        var ts = Touchscreen.current;
        if (ts != null)
        {
            foreach (var t in ts.touches)
                if (t.press.isPressed) dedos++;
            var p = ts.primaryTouch;
            if (p.press.isPressed || p.press.wasReleasedThisFrame)
            {
                empezo = p.press.wasPressedThisFrame;
                presionado = p.press.isPressed;
                solto = p.press.wasReleasedThisFrame;
                pos = p.position.ReadValue();
                return;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            empezo = mouse.leftButton.wasPressedThisFrame;
            presionado = mouse.leftButton.isPressed;
            solto = mouse.leftButton.wasReleasedThisFrame;
            if (presionado) dedos = 1;
            pos = mouse.position.ReadValue();
        }
    }

    bool BuscarLugar(Vector2 punto, Camera cam, out Vector3 lugar, out float apoyo)
    {
        lugar = default;
        apoyo = 0f;
        var rayo = cam.ScreenPointToRay(punto);

        // primero arriba de otro mueble
        if (Physics.Raycast(rayo, out var hit, 15f, ~0, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.7f)
        {
            var m = hit.collider.GetComponentInParent<Mueble>();
            if (m != null && m.permiteApilar)
            {
                lugar = hit.point;
                apoyo = Apoyo.Calcular(lugar, null);
                return true;
            }
        }

        if (raycastManager != null && raycastManager.Raycast(punto, hitsAR, TrackableType.PlaneWithinPolygon))
        {
            foreach (var h in hitsAR)
            {
                if ((h.pose.rotation * Vector3.up).y < 0.7f) continue;
                lugar = h.pose.position;
                Apoyo.AvisarPiso(lugar.y);
                apoyo = Apoyo.Calcular(lugar, null);
                return true;
            }
        }
        return false;
    }

    bool Solapa(Vector3 pos, Quaternion rot)
    {
        var centro = pos + rot * boundsFantasma.center + Vector3.up * 0.02f;
        var mitad = Vector3.Max(boundsFantasma.extents * 0.85f - Vector3.one * 0.01f, Vector3.one * 0.01f);
        int n = Physics.OverlapBoxNonAlloc(centro, mitad, solapados, rot, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var m = solapados[i].GetComponentInParent<Mueble>();
            if (m != null && m.gameObject.activeInHierarchy) return true;
        }
        return false;
    }

    bool SobreMueble(Vector2 pos, Camera cam)
    {
        return Physics.Raycast(cam.ScreenPointToRay(pos), out var hit, 15f, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<Mueble>() != null;
    }

    bool SobreUI(Vector2 pos)
    {
        if (EventSystem.current == null) return false;
        if (datosUI == null) datosUI = new PointerEventData(EventSystem.current);
        datosUI.position = pos;
        resultadosUI.Clear();
        EventSystem.current.RaycastAll(datosUI, resultadosUI);
        return resultadosUI.Count > 0;
    }

    void AlAparecer(GameObject obj)
    {
        var m = obj.GetComponent<Mueble>();
        if (m == null) return;
        m.alturaExtra = m.alturaInicial;
        m.apoyoY = pendienteApoyo;
        var p = obj.transform.position;
        obj.transform.position = new Vector3(p.x, pendienteApoyo + m.alturaExtra, p.z);
        m.AnimarEntrada();
        Historial.Colocado(m);
        Feedback.Pop();
    }
}
