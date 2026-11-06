using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// apaga el spawn mientras se toca la UI, para no poner muebles atras de los botones
[DefaultExecutionOrder(-200)]
public class BloqueoToqueUI : MonoBehaviour
{
    public Behaviour spawnTrigger;
    public float tiempoExtra = 0.3f;

    float ultimoToqueUI = -10f;
    readonly List<RaycastResult> resultados = new List<RaycastResult>();
    PointerEventData datos;

    void Update()
    {
        if (spawnTrigger == null || EventSystem.current == null) return;

        if (datos == null) datos = new PointerEventData(EventSystem.current);

        if (Touchscreen.current != null)
        {
            foreach (var t in Touchscreen.current.touches)
            {
                if (t.press.isPressed && SobreUI(t.position.ReadValue()))
                    ultimoToqueUI = Time.time;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed && SobreUI(Mouse.current.position.ReadValue()))
            ultimoToqueUI = Time.time;

        spawnTrigger.enabled = Time.time - ultimoToqueUI > tiempoExtra;
    }

    bool SobreUI(Vector2 pos)
    {
        datos.position = pos;
        resultados.Clear();
        EventSystem.current.RaycastAll(datos, resultados);
        return resultados.Count > 0;
    }
}
