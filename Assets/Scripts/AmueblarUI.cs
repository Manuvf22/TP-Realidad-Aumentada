using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Templates.AR;

// controla toda la UI de la app y usa el menu manager del template
public class AmueblarUI : MonoBehaviour
{
    [Header("Template")]
    public ARTemplateMenuManager menuManager;
    public ObjectSpawner spawner;
    public ARPlaneManager planeManager;
    public XRInteractionGroup grupoInteraccion;

    [Header("Propios")]
    public CatalogoMuebles catalogo;
    public ColocadorMuebles colocador;
    public CapturaPantalla captura;

    [Header("Menu")]
    public RectTransform contenedorCategorias;
    public RectTransform contenedorVariantes;
    public GameObject panelVariantes;
    public TMP_Text textoTituloVariantes;
    public Button plantillaBoton;

    [Header("Textos")]
    public TMP_Text textoAyuda;
    public TMP_Text textoContador;
    public TMP_Text textoEstado;
    public TMP_Text textoSeleccion;
    public TMP_Text textoMedidas;
    public TMP_Text textoAviso;
    public TMP_Text textoBotonPlanos;

    [Header("Botones")]
    public Button botonRotar;
    public Button botonColor;
    public Button botonMaterial;
    public Button botonDuplicar;
    public Button botonBorrar;
    public BotonRepetir botonSubir;
    public BotonRepetir botonBajar;
    public Button botonPlanos;
    public Button botonBorrarTodo;
    public Button botonDeshacer;
    public Button botonCaptura;

    [Header("Paneles")]
    public RectTransform panelSeleccion;
    public GameObject panelEstado;
    public CanvasGroup panelAviso;

    public Color colorNormal = new Color(1f, 1f, 1f, 0.9f);
    public Color colorElegido = new Color(1f, 0.75f, 0.2f, 1f);
    public float pasoAltura = 0.05f;

    bool planosVisibles = true;
    Mueble seleccionado;
    int elegido = -1;
    string categoriaAbierta;
    float avisoHasta;
    float ySeleccion, altoVariantes;
    Mueble.Estado estadoAltura;

    readonly List<string> categorias = new List<string>();
    readonly List<Button> botonesCategoria = new List<Button>();
    readonly List<KeyValuePair<int, Button>> botonesVariante = new List<KeyValuePair<int, Button>>();

    void Start()
    {
        Historial.Limpiar();
        Historial.contenedor = spawner.transform;

        // el spawner del template usa la lista del catalogo
        var prefabs = new List<GameObject>();
        foreach (var d in catalogo.muebles) prefabs.Add(d != null ? d.prefab : null);
        spawner.objectPrefabs = prefabs;
        spawner.applyRandomAngleAtSpawn = false;
        if (spawner.cameraToFace == null) spawner.cameraToFace = Camera.main;

        // sonido en todos los botones fijos
        foreach (var b in GetComponentsInChildren<Button>(true))
            if (b != plantillaBoton) b.onClick.AddListener(Feedback.Click);
        plantillaBoton.gameObject.SetActive(false);

        botonRotar.onClick.AddListener(() => ConSeleccion(m => { var e = m.Guardar(); m.Rotar(45f); Historial.Movido(m, e); }));
        botonColor.onClick.AddListener(() => ConSeleccion(m => { var e = m.Guardar(); m.CambiarColor(); Historial.Estilo(m, e); }));
        botonMaterial.onClick.AddListener(() => ConSeleccion(m =>
        {
            var e = m.Guardar();
            m.CambiarMaterial();
            Historial.Estilo(m, e);
            Aviso("Material: " + m.nombreMaterial);
        }));
        botonDuplicar.onClick.AddListener(Duplicar);
        botonBorrar.onClick.AddListener(() => ConSeleccion(m => Borrar(new List<Mueble> { m })));
        botonPlanos.onClick.AddListener(CambiarPlanos);
        botonBorrarTodo.onClick.AddListener(BorrarTodo);
        botonDeshacer.onClick.AddListener(Deshacer);
        botonCaptura.onClick.AddListener(() => captura.Sacar());
        captura.alTerminar = ok => Aviso(ok ? "Foto guardada en la galería" : "No se pudo guardar la foto");

        ConfigurarAltura(botonSubir, pasoAltura);
        ConfigurarAltura(botonBajar, -pasoAltura);

        ySeleccion = panelSeleccion.anchoredPosition.y;
        altoVariantes = ((RectTransform)panelVariantes.transform).rect.height + 12f;

        ArmarCategorias();
        panelVariantes.SetActive(false);
        if (catalogo.muebles.Count > 0) Elegir(0);
        ActualizarTextoPlanos();
        if (panelAviso) panelAviso.alpha = 0f;
    }

    void ConfigurarAltura(BotonRepetir boton, float paso)
    {
        boton.alEmpezar = () => { if (seleccionado) estadoAltura = seleccionado.Guardar(); };
        boton.alRepetir = () => { if (seleccionado) seleccionado.CambiarAltura(paso); };
        boton.alTerminar = () =>
        {
            if (seleccionado && Mueble.Distintos(estadoAltura, seleccionado.Guardar()))
                Historial.Movido(seleccionado, estadoAltura);
        };
    }

    Button CrearBoton(Transform padre, string texto, Sprite icono)
    {
        var b = Instantiate(plantillaBoton, padre);
        b.gameObject.SetActive(true);
        b.name = "Boton " + texto;
        var t = b.GetComponentInChildren<TMP_Text>(true);
        if (t) t.text = texto;
        var img = b.transform.Find("Icono");
        if (img)
        {
            var i = img.GetComponent<Image>();
            i.sprite = icono;
            i.enabled = icono != null;
        }
        b.onClick.AddListener(Feedback.Click);
        return b;
    }

    void ArmarCategorias()
    {
        categorias.AddRange(catalogo.Categorias());
        foreach (var c in categorias)
        {
            var indices = catalogo.IndicesDe(c);
            var icono = indices.Count > 0 ? catalogo.muebles[indices[0]].icono : null;
            var b = CrearBoton(contenedorCategorias, c, icono);
            string cat = c;
            b.onClick.AddListener(() => AbrirCategoria(cat));
            botonesCategoria.Add(b);
        }
    }

    void AbrirCategoria(string cat)
    {
        if (panelVariantes.activeSelf && categoriaAbierta == cat)
        {
            panelVariantes.SetActive(false);
            return;
        }

        foreach (Transform hijo in contenedorVariantes) Destroy(hijo.gameObject);
        botonesVariante.Clear();
        foreach (var i in catalogo.IndicesDe(cat))
        {
            var d = catalogo.muebles[i];
            var b = CrearBoton(contenedorVariantes, d.nombre, d.icono);
            int indice = i;
            b.onClick.AddListener(() => Elegir(indice));
            botonesVariante.Add(new KeyValuePair<int, Button>(i, b));
        }
        categoriaAbierta = cat;
        if (textoTituloVariantes) textoTituloVariantes.text = cat;
        panelVariantes.SetActive(true);
        Resaltar();
    }

    public void Elegir(int indice)
    {
        elegido = indice;
        menuManager.SetObjectToSpawn(indice);
        colocador.SetPrefab(catalogo.muebles[indice].prefab);
        panelVariantes.SetActive(false);
        Resaltar();
    }

    void Resaltar()
    {
        string cat = elegido >= 0 ? catalogo.muebles[elegido].categoria : null;
        for (int i = 0; i < botonesCategoria.Count; i++)
            botonesCategoria[i].GetComponent<Image>().color = categorias[i] == cat ? colorElegido : colorNormal;
        foreach (var par in botonesVariante)
            if (par.Value) par.Value.GetComponent<Image>().color = par.Key == elegido ? colorElegido : colorNormal;
    }

    void Update()
    {
        // estado de la sesion
        bool sesionLista = ARSession.state == ARSessionState.SessionTracking;
        panelEstado.SetActive(!sesionLista);
        if (!sesionLista) textoEstado.text = TextoEstado(ARSession.state);

        int planos = planeManager.trackables.count;
        textoContador.text = "Planos: " + planos + "   Muebles: " + ContarMuebles();

        // el seleccionado es el que tiene el foco del interactor
        Mueble nuevo = null;
        var foco = grupoInteraccion != null ? grupoInteraccion.focusInteractable : null;
        if (foco != null) nuevo = foco.transform.GetComponent<Mueble>();
        if (nuevo != null && !nuevo.gameObject.activeInHierarchy) nuevo = null;

        if (nuevo != seleccionado)
        {
            if (seleccionado) seleccionado.SetSeleccionado(false);
            seleccionado = nuevo;
            if (seleccionado)
            {
                seleccionado.SetSeleccionado(true);
                panelVariantes.SetActive(false);
                Feedback.Seleccion();
            }
        }

        panelSeleccion.gameObject.SetActive(seleccionado != null);
        // si el submenu esta abierto el panel sube
        panelSeleccion.anchoredPosition = new Vector2(panelSeleccion.anchoredPosition.x,
            ySeleccion + (panelVariantes.activeSelf ? altoVariantes : 0f));
        if (seleccionado)
        {
            textoSeleccion.text = seleccionado.nombre;
            var med = seleccionado.MedidasCm();
            textoMedidas.text = Mathf.RoundToInt(med.x) + " × " + Mathf.RoundToInt(med.y) + " × " + Mathf.RoundToInt(med.z) + " cm";
            if (seleccionado.alturaExtra > 0.001f) textoMedidas.text += "  ·  elevado " + Mathf.RoundToInt(seleccionado.alturaExtra * 100f) + " cm";
        }

        botonDeshacer.interactable = Historial.cantidad > 0;
        textoAyuda.text = TextoAyuda(planos);

        if (panelAviso)
        {
            float objetivo = Time.time < avisoHasta ? 1f : 0f;
            panelAviso.alpha = Mathf.MoveTowards(panelAviso.alpha, objetivo, Time.deltaTime * 4f);
        }
    }

    string TextoAyuda(int planos)
    {
        if (planos == 0) return "Mové el celular despacio para escanear el piso";
        if (seleccionado) return "Arrastrá para mover · girá o pellizcá con dos dedos";
        string nombre = elegido >= 0 ? catalogo.muebles[elegido].nombre : "un mueble";
        if (colocador.arrastrando)
            return colocador.lugarValido ? "Soltá para colocar " + nombre : "Ahí no entra, probá en otro lugar";
        if (!colocador.hayLugar) return "Apuntá al piso para ver " + nombre;
        return "Arrastrá el dedo por el piso y soltá para colocar " + nombre;
    }

    void ConSeleccion(System.Action<Mueble> accion)
    {
        if (seleccionado) accion(seleccionado);
    }

    void Duplicar()
    {
        if (!seleccionado) return;
        var t = seleccionado.transform;
        // lo corro al costado segun la camara
        var cam = Camera.main.transform;
        var costado = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        var tam = seleccionado.BoundsLocal.size * t.localScale.x;
        var pos = t.position + costado * (Mathf.Max(tam.x, tam.z) + 0.1f);

        var copia = Instantiate(seleccionado.gameObject, pos, t.rotation, spawner.transform);
        copia.transform.localScale = t.localScale;
        var m = copia.GetComponent<Mueble>();
        m.CopiarDe(seleccionado);
        m.SetSeleccionado(false);
        m.Reacomodar();
        m.AnimarEntrada();
        Historial.Colocado(m);
        Feedback.Pop();
    }

    void Borrar(List<Mueble> lista)
    {
        if (lista.Count == 0) return;
        Historial.Borrado(lista);
        foreach (var m in lista)
        {
            var mueble = m;
            mueble.AnimarSalida(() =>
            {
                mueble.gameObject.SetActive(false);
                Apoyo.ReacomodarTodos(spawner.transform);
            });
        }
        seleccionado = null;
        Feedback.Borrar();
    }

    void BorrarTodo()
    {
        var lista = new List<Mueble>();
        foreach (Transform hijo in spawner.transform)
        {
            var m = hijo.GetComponent<Mueble>();
            if (m != null && hijo.gameObject.activeSelf) lista.Add(m);
        }
        if (lista.Count > 0)
        {
            Borrar(lista);
            Aviso("Se borraron " + lista.Count + " muebles (podés deshacer)");
        }
    }

    void Deshacer()
    {
        var hecho = Historial.Deshacer();
        if (hecho != null) Aviso("Deshecho: " + hecho);
    }

    void Aviso(string texto)
    {
        if (textoAviso == null) return;
        textoAviso.text = texto;
        avisoHasta = Time.time + 1.8f;
    }

    void CambiarPlanos()
    {
        planosVisibles = !planosVisibles;
        menuManager.ShowHideDebugPlane();
        ActualizarTextoPlanos();
    }

    void ActualizarTextoPlanos()
    {
        if (textoBotonPlanos) textoBotonPlanos.text = planosVisibles ? "Ocultar planos" : "Mostrar planos";
    }

    int ContarMuebles()
    {
        int n = 0;
        foreach (Transform hijo in spawner.transform)
            if (hijo.gameObject.activeSelf && hijo.GetComponent<Mueble>() != null) n++;
        return n;
    }

    string TextoEstado(ARSessionState estado)
    {
        switch (estado)
        {
            case ARSessionState.Unsupported: return "Este celular no soporta AR";
            case ARSessionState.NeedsInstall: return "Hay que instalar Google Play Services para AR";
            case ARSessionState.Installing: return "Instalando servicios de AR...";
            case ARSessionState.CheckingAvailability: return "Chequeando AR...";
            case ARSessionState.SessionInitializing: return "Iniciando AR, mové el celular";
            default: return "Esperando sesión de AR...";
        }
    }
}
