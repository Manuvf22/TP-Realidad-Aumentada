using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// va en cada prefab de mueble
public class Mueble : MonoBehaviour
{
    public string nombre = "Mueble";
    public Transform visuales;
    public Renderer[] partesColor;
    public Material[] materialesOpcion; // madera, metal, tela
    public GameObject indicador;
    public Transform sombra;
    public bool permiteApilar = true;
    public float alturaInicial = 0f; // para la lampara colgante

    [HideInInspector] public float apoyoY;      // altura de lo que tiene abajo
    [HideInInspector] public float alturaExtra; // lo que se subio con los botones

    public static bool ocultarIndicadores;

    static readonly Color[] colores =
    {
        new Color(0.85f, 0.85f, 0.82f), // blanco
        new Color(0.15f, 0.15f, 0.17f), // negro
        new Color(0.20f, 0.35f, 0.60f), // azul
        new Color(0.65f, 0.20f, 0.20f), // rojo
        new Color(0.30f, 0.55f, 0.30f), // verde
        new Color(0.55f, 0.36f, 0.22f)  // marron
    };
    static readonly string[] nombresMaterial = { "Madera", "Metal", "Tela" };

    public struct Estado
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 escala;
        public float altura, apoyo;
        public int color, material;
    }

    int colorActual = -1;
    int materialActual = -1;
    Material[] originales;
    Bounds boundsLocal;
    MaterialPropertyBlock mpb;
    Renderer sombraRend;
    float sombraAlfa = 0.55f;
    Coroutine anim;
    Estado alAgarrar;
    TransformadorMueble transformador;

    public XRGrabInteractable grab { get; private set; }
    public bool agarrado => grab != null && grab.isSelected;
    public string nombreMaterial => materialActual < 0 ? "Original" : nombresMaterial[materialActual % nombresMaterial.Length];

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        transformador = GetComponent<TransformadorMueble>();
        mpb = new MaterialPropertyBlock();
        originales = new Material[partesColor.Length];
        for (int i = 0; i < partesColor.Length; i++)
            if (partesColor[i]) originales[i] = partesColor[i].sharedMaterial;

        boundsLocal = BoundsDe(transform, visuales != null ? visuales : transform);
        if (sombra) sombraRend = sombra.GetComponent<Renderer>();
        apoyoY = transform.position.y;
        SetSeleccionado(false);

        if (grab != null)
        {
            grab.selectEntered.AddListener(_ => alAgarrar = Guardar());
            grab.selectExited.AddListener(_ => AlSoltar());
        }
    }

    void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveAllListeners();
            grab.selectExited.RemoveAllListeners();
        }
    }

    // calcula la caja del modelo en espacio local de la raiz
    public static Bounds BoundsDe(Transform raiz, Transform visuales)
    {
        var b = new Bounds();
        bool hay = false;
        var aLocal = raiz.worldToLocalMatrix;
        foreach (var mf in visuales.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var m = aLocal * mf.transform.localToWorldMatrix;
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var esquina = mb.center + Vector3.Scale(mb.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2f - Vector3.one);
                var p = m.MultiplyPoint3x4(esquina);
                if (!hay) { b = new Bounds(p, Vector3.zero); hay = true; }
                else b.Encapsulate(p);
            }
        }
        return b;
    }

    public Bounds BoundsLocal => boundsLocal;

    public Vector3 MedidasCm()
    {
        return boundsLocal.size * transform.localScale.x * 100f;
    }

    public void SetSeleccionado(bool valor)
    {
        if (indicador != null && indicador.activeSelf != valor)
            indicador.SetActive(valor);
    }

    void LateUpdate()
    {
        var p = transform.position;
        if (sombra != null)
        {
            // la sombra queda apoyada abajo aunque el mueble este levantado
            var c = transform.TransformPoint(new Vector3(boundsLocal.center.x, 0f, boundsLocal.center.z));
            sombra.position = new Vector3(c.x, apoyoY + 0.003f, c.z);
            sombra.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
            float h = Mathf.Max(0f, p.y - apoyoY);
            if (sombraRend)
            {
                sombraRend.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", new Color(0f, 0f, 0f, sombraAlfa * Mathf.Clamp01(1f - h / 2f)));
                sombraRend.SetPropertyBlock(mpb);
            }
        }

        if (indicador != null && indicador.activeSelf)
        {
            float s = 1f + Mathf.Sin(Time.time * 5f) * 0.04f;
            indicador.transform.localScale = new Vector3(s, 1f, s);
            var r = indicador.GetComponentInChildren<Renderer>();
            if (r) r.enabled = !ocultarIndicadores;
        }
    }

    public void Rotar(float grados)
    {
        transform.Rotate(0f, grados, 0f, Space.World);
    }

    public void CambiarColor()
    {
        colorActual = (colorActual + 1) % colores.Length;
        AplicarEstilo();
    }

    public void CambiarMaterial()
    {
        if (materialesOpcion == null || materialesOpcion.Length == 0) return;
        materialActual = (materialActual + 1) % materialesOpcion.Length;
        AplicarEstilo();
    }

    void AplicarEstilo()
    {
        for (int i = 0; i < partesColor.Length; i++)
        {
            var r = partesColor[i];
            if (r == null) continue;
            r.sharedMaterial = materialActual >= 0 ? materialesOpcion[materialActual] : originales[i];
            if (colorActual >= 0)
            {
                // property block asi no se crean materiales nuevos
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", colores[colorActual]);
                r.SetPropertyBlock(mpb);
            }
            else r.SetPropertyBlock(null);
        }
    }

    public void CopiarDe(Mueble otro)
    {
        for (int i = 0; i < originales.Length && i < otro.originales.Length; i++)
            originales[i] = otro.originales[i];
        colorActual = otro.colorActual;
        materialActual = otro.materialActual;
        alturaExtra = otro.alturaExtra;
        AplicarEstilo();
    }

    public Estado Guardar()
    {
        return new Estado
        {
            pos = transform.position, rot = transform.rotation, escala = transform.localScale,
            altura = alturaExtra, apoyo = apoyoY, color = colorActual, material = materialActual
        };
    }

    public void Restaurar(Estado e)
    {
        transform.SetPositionAndRotation(e.pos, e.rot);
        transform.localScale = e.escala;
        alturaExtra = e.altura;
        apoyoY = e.apoyo;
        colorActual = e.color;
        materialActual = e.material;
        AplicarEstilo();
    }

    public static bool Distintos(Estado a, Estado b)
    {
        return (a.pos - b.pos).sqrMagnitude > 0.0001f || Quaternion.Angle(a.rot, b.rot) > 0.5f
            || Mathf.Abs(a.escala.x - b.escala.x) > 0.001f || a.color != b.color || a.material != b.material;
    }

    // altura con los botones de subir y bajar
    public void CambiarAltura(float delta)
    {
        alturaExtra = Mathf.Max(0f, alturaExtra + delta);
        var p = transform.position;
        transform.position = new Vector3(p.x, apoyoY + alturaExtra, p.z);
    }

    public void Reacomodar()
    {
        apoyoY = Apoyo.Calcular(transform.position, this);
        var p = transform.position;
        transform.position = new Vector3(p.x, apoyoY + alturaExtra, p.z);
    }

    void AlSoltar()
    {
        // con el suavizado puede quedar a mitad de camino, lo termino de llevar
        if (transformador != null && transformador.tieneObjetivo)
        {
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Asentar(transformador.ultimoObjetivo));
        }
        else RegistrarMovimiento();
    }

    IEnumerator Asentar(Pose objetivo)
    {
        float t = 0f;
        var p0 = transform.position;
        var r0 = transform.rotation;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 0.12f);
            transform.SetPositionAndRotation(Vector3.Lerp(p0, objetivo.position, k), Quaternion.Slerp(r0, objetivo.rotation, k));
            yield return null;
        }
        transform.SetPositionAndRotation(objetivo.position, objetivo.rotation);
        anim = null;
        RegistrarMovimiento();
    }

    void RegistrarMovimiento()
    {
        if (Distintos(alAgarrar, Guardar()))
            Historial.Movido(this, alAgarrar);
    }

    public void AnimarEntrada()
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Entrada(transform.localScale.x > 0.05f ? transform.localScale : Vector3.one));
    }

    public void AnimarSalida(Action alTerminar)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Salida(alTerminar));
    }

    IEnumerator Entrada(Vector3 fin)
    {
        const float dur = 0.25f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = RebotarSalida(Mathf.Clamp01(t / dur));
            transform.localScale = fin * Mathf.Max(0.01f, k);
            yield return null;
        }
        transform.localScale = fin;
        anim = null;
    }

    IEnumerator Salida(Action alTerminar)
    {
        const float dur = 0.18f;
        var inicio = transform.localScale;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / dur);
            transform.localScale = inicio * Mathf.Max(0.01f, k * k);
            yield return null;
        }
        transform.localScale = inicio; // por si despues se deshace
        anim = null;
        alTerminar?.Invoke();
    }

    // ease out back, se pasa un poco y vuelve
    static float RebotarSalida(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
