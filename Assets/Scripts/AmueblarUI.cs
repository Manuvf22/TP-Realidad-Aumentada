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

    [Header("Textos")]
    public TMP_Text textoAyuda;
    public TMP_Text textoContador;
    public TMP_Text textoEstado;
    public TMP_Text textoSeleccion;
    public TMP_Text textoBotonPlanos;

    [Header("Botones")]
    public Button[] botonesMuebles;
    public Button botonRotar;
    public Button botonColor;
    public Button botonDuplicar;
    public Button botonBorrar;
    public Button botonPlanos;
    public Button botonBorrarTodo;
    public GameObject panelSeleccion;
    public GameObject panelEstado;

    public Color colorNormal = new Color(1f, 1f, 1f, 0.85f);
    public Color colorElegido = new Color(1f, 0.75f, 0.2f, 1f);

    bool planosVisibles = true;
    Mueble seleccionado;

    void Start()
    {
        for (int i = 0; i < botonesMuebles.Length; i++)
        {
            int indice = i;
            botonesMuebles[i].onClick.AddListener(() => ElegirMueble(indice));
        }

        botonRotar.onClick.AddListener(() => { if (seleccionado) seleccionado.Rotar(45f); });
        botonColor.onClick.AddListener(() => { if (seleccionado) seleccionado.CambiarColor(); });
        botonDuplicar.onClick.AddListener(Duplicar);
        botonBorrar.onClick.AddListener(BorrarSeleccionado);
        botonPlanos.onClick.AddListener(CambiarPlanos);
        botonBorrarTodo.onClick.AddListener(() => menuManager.ClearAllObjects());

        // que aparezcan mirando a la camara, sin angulo random
        spawner.applyRandomAngleAtSpawn = false;
        if (spawner.cameraToFace == null) spawner.cameraToFace = Camera.main;

        ElegirMueble(0);
        ActualizarTextoPlanos();
    }

    void Update()
    {
        // estado de la sesion
        bool sesionLista = ARSession.state == ARSessionState.SessionTracking;
        panelEstado.SetActive(!sesionLista);
        if (!sesionLista) textoEstado.text = TextoEstado(ARSession.state);

        int planos = planeManager.trackables.count;
        textoContador.text = "Planos: " + planos + "   Muebles: " + ContarMuebles();

        if (planos == 0)
            textoAyuda.text = "Mové el celular despacio para escanear el piso";
        else
            textoAyuda.text = "Elegí un mueble y tocá el piso";

        // el seleccionado es el que tiene el foco del interactor
        Mueble nuevo = null;
        var foco = grupoInteraccion != null ? grupoInteraccion.focusInteractable : null;
        if (foco != null) nuevo = foco.transform.GetComponent<Mueble>();

        if (nuevo != seleccionado)
        {
            if (seleccionado) seleccionado.SetSeleccionado(false);
            seleccionado = nuevo;
            if (seleccionado) seleccionado.SetSeleccionado(true);
        }

        panelSeleccion.SetActive(seleccionado != null);
        if (seleccionado) textoSeleccion.text = seleccionado.nombre;
    }

    public void ElegirMueble(int indice)
    {
        menuManager.SetObjectToSpawn(indice);
        for (int i = 0; i < botonesMuebles.Length; i++)
        {
            var img = botonesMuebles[i].GetComponent<Image>();
            img.color = i == indice ? colorElegido : colorNormal;
        }
    }

    void Duplicar()
    {
        if (!seleccionado) return;
        var t = seleccionado.transform;
        // lo corro un poco al costado segun la camara
        var cam = Camera.main.transform;
        var costado = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        var copia = Instantiate(seleccionado.gameObject, t.position + costado * 0.6f * t.localScale.x, t.rotation, spawner.transform);
        copia.transform.localScale = t.localScale;
        copia.GetComponent<Mueble>().SetSeleccionado(false);
    }

    void BorrarSeleccionado()
    {
        if (!seleccionado) return;
        Destroy(seleccionado.gameObject);
        seleccionado = null;
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
            if (hijo.GetComponent<Mueble>() != null) n++;
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
