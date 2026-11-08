using UnityEngine;
using UnityEngine.XR.ARFoundation;

// aplica la luz estimada por ARCore a la luz direccional
[RequireComponent(typeof(Light))]
public class LuzEstimada : MonoBehaviour
{
    public ARCameraManager camara;
    public float suavizado = 4f;

    Light luz;
    float intensidadBase;
    Color colorBase;
    float intensidadObjetivo;
    Color colorObjetivo;
    Quaternion rotacionObjetivo;
    bool hayDireccion;

    void Awake()
    {
        luz = GetComponent<Light>();
        intensidadBase = luz.intensity;
        colorBase = luz.color;
        intensidadObjetivo = intensidadBase;
        colorObjetivo = colorBase;
        rotacionObjetivo = transform.rotation;
    }

    void OnEnable()
    {
        if (camara != null) camara.frameReceived += AlRecibirFrame;
    }

    void OnDisable()
    {
        if (camara != null) camara.frameReceived -= AlRecibirFrame;
    }

    void AlRecibirFrame(ARCameraFrameEventArgs args)
    {
        var le = args.lightEstimation;

        if (le.mainLightIntensityLumens.HasValue)
            intensidadObjetivo = Mathf.Clamp(le.mainLightIntensityLumens.Value / 1000f, 0.2f, 2.5f);
        else if (le.averageBrightness.HasValue)
            intensidadObjetivo = intensidadBase * Mathf.Clamp(le.averageBrightness.Value * 2f, 0.2f, 2.5f);

        if (le.mainLightColor.HasValue)
            colorObjetivo = le.mainLightColor.Value;
        else if (le.colorCorrection.HasValue)
        {
            var c = le.colorCorrection.Value;
            float m = Mathf.Max(c.r, c.g, c.b, 0.001f);
            colorObjetivo = colorBase * new Color(c.r / m, c.g / m, c.b / m, 1f);
        }

        if (le.mainLightDirection.HasValue)
        {
            rotacionObjetivo = Quaternion.LookRotation(le.mainLightDirection.Value);
            hayDireccion = true;
        }
    }

    void Update()
    {
        // suavizado para que no parpadee
        float k = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
        luz.intensity = Mathf.Lerp(luz.intensity, intensidadObjetivo, k);
        luz.color = Color.Lerp(luz.color, colorObjetivo, k);
        if (hayDireccion) transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, k);
    }
}
