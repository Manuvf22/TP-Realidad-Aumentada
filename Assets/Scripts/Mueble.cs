using UnityEngine;

// va en cada prefab de mueble
public class Mueble : MonoBehaviour
{
    public string nombre = "Mueble";
    public Renderer[] partesColor;
    public GameObject indicador;

    static readonly Color[] colores =
    {
        new Color(0.55f, 0.36f, 0.22f), // madera
        new Color(0.85f, 0.85f, 0.82f), // blanco
        new Color(0.15f, 0.15f, 0.17f), // negro
        new Color(0.20f, 0.35f, 0.60f), // azul
        new Color(0.65f, 0.20f, 0.20f), // rojo
        new Color(0.30f, 0.55f, 0.30f)  // verde
    };

    int colorActual = -1;

    void Awake()
    {
        SetSeleccionado(false);
    }

    public void SetSeleccionado(bool valor)
    {
        if (indicador != null && indicador.activeSelf != valor)
            indicador.SetActive(valor);
    }

    public void Rotar(float grados)
    {
        transform.Rotate(0f, grados, 0f, Space.World);
    }

    public void CambiarColor()
    {
        colorActual = (colorActual + 1) % colores.Length;
        foreach (var r in partesColor)
        {
            if (r == null) continue;
            // material propio para no cambiar el de los otros muebles
            r.material.SetColor("_BaseColor", colores[colorActual]);
        }
    }
}
