using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

// saca una foto sin la UI y la guarda en la galeria
public class CapturaPantalla : MonoBehaviour
{
    public ColocadorMuebles colocador;
    public Action<bool> alTerminar;

    bool sacando;

    public void Sacar()
    {
        if (!sacando) StartCoroutine(Rutina());
    }

    IEnumerator Rutina()
    {
        sacando = true;
        var ocultos = new List<Canvas>();
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.enabled && c.isRootCanvas)
            {
                c.enabled = false;
                ocultos.Add(c);
            }
        }
        bool colocadorActivo = colocador != null && colocador.enabled;
        if (colocador != null) colocador.enabled = false;
        Mueble.ocultarIndicadores = true;

        yield return null;
        yield return new WaitForEndOfFrame();
        var tex = ScreenCapture.CaptureScreenshotAsTexture();

        foreach (var c in ocultos) if (c != null) c.enabled = true;
        if (colocador != null) colocador.enabled = colocadorActivo;
        Mueble.ocultarIndicadores = false;

        var png = tex.EncodeToPNG();
        Destroy(tex);
        string nombre = "Pieza_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        bool ok = Guardar(png, nombre);
        Feedback.Pop();
        alTerminar?.Invoke(ok);
        sacando = false;
    }

    static bool Guardar(byte[] png, string nombre)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // MediaStore, en Android 10+ no hace falta permiso
            var actividad = AndroidApplication.currentActivity;
            using (var resolver = actividad.Call<AndroidJavaObject>("getContentResolver"))
            using (var valores = new AndroidJavaObject("android.content.ContentValues"))
            using (var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media"))
            using (var uriBase = media.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI"))
            {
                valores.Call("put", "_display_name", nombre);
                valores.Call("put", "mime_type", "image/png");
                valores.Call("put", "relative_path", "Pictures/AmueblaTuPieza");
                using (var uri = resolver.Call<AndroidJavaObject>("insert", uriBase, valores))
                {
                    if (uri == null) return false;
                    using (var salida = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
                    {
                        var datos = new sbyte[png.Length];
                        Buffer.BlockCopy(png, 0, datos, 0, png.Length);
                        salida.Call("write", datos);
                        salida.Call("flush");
                        salida.Call("close");
                    }
                }
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("No se pudo guardar la captura: " + e.Message);
            return false;
        }
#else
        var ruta = System.IO.Path.Combine(Application.persistentDataPath, nombre);
        System.IO.File.WriteAllBytes(ruta, png);
        Debug.Log("Captura guardada en " + ruta);
        return true;
#endif
    }
}
