using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

// sonidos generados por codigo y vibracion corta
[RequireComponent(typeof(AudioSource))]
public class Feedback : MonoBehaviour
{
    static Feedback instancia;

    AudioSource fuente;
    AudioClip pop, click, borrar, tick;
    float ultimoTick;

    void Awake()
    {
        instancia = this;
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;

        pop = Barrido("pop", 0.14f, 950f, 260f, 28f, 0.6f);
        click = Barrido("click", 0.04f, 2200f, 1600f, 90f, 0.35f);
        borrar = Barrido("borrar", 0.2f, 520f, 120f, 16f, 0.5f);
        tick = Barrido("tick", 0.025f, 3200f, 3000f, 140f, 0.2f);
    }

    // seno que baja de frecuencia con caida exponencial
    static AudioClip Barrido(string nombre, float dur, float f0, float f1, float caida, float vol)
    {
        const int sr = 44100;
        int n = Mathf.CeilToInt(sr * dur);
        var datos = new float[n];
        float fase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            float f = Mathf.Lerp(f0, f1, t / dur);
            fase += 2f * Mathf.PI * f / sr;
            float ataque = Mathf.Clamp01(t / 0.004f);
            datos[i] = Mathf.Sin(fase) * Mathf.Exp(-t * caida) * ataque * vol;
        }
        var clip = AudioClip.Create(nombre, n, 1, sr, false);
        clip.SetData(datos, 0);
        return clip;
    }

    void Sonar(AudioClip c, float pitchVar = 0.06f)
    {
        if (c == null) return;
        fuente.pitch = 1f + Random.Range(-pitchVar, pitchVar);
        fuente.PlayOneShot(c);
    }

    public static void Pop()
    {
        if (instancia == null) return;
        instancia.Sonar(instancia.pop);
        Vibrar(35);
    }

    public static void Click()
    {
        if (instancia != null) instancia.Sonar(instancia.click, 0.02f);
    }

    public static void Seleccion()
    {
        if (instancia != null) instancia.Sonar(instancia.click, 0.02f);
        Vibrar(18);
    }

    public static void Borrar()
    {
        if (instancia != null) instancia.Sonar(instancia.borrar);
        Vibrar(45);
    }

    public static void Tick()
    {
        if (instancia == null || Time.time - instancia.ultimoTick < 0.05f) return;
        instancia.ultimoTick = Time.time;
        instancia.Sonar(instancia.tick, 0f);
        Vibrar(8);
    }

    public static void Vibrar(long ms)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var actividad = AndroidApplication.currentActivity;
            using (var vibrador = actividad.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            using (var clase = new AndroidJavaClass("android.os.VibrationEffect"))
            using (var efecto = clase.CallStatic<AndroidJavaObject>("createOneShot", ms, 150))
            {
                if (vibrador != null) vibrador.Call("vibrate", efecto);
            }
        }
        catch
        {
            Handheld.Vibrate();
        }
#endif
    }
}
