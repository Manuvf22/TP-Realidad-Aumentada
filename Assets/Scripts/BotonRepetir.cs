using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// boton que repite la accion mientras se mantiene apretado
public class BotonRepetir : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float espera = 0.35f;
    public float intervalo = 0.1f;

    public Action alEmpezar;
    public Action alRepetir;
    public Action alTerminar;

    bool apretado;
    float proximo;
    Selectable boton;

    void Awake()
    {
        boton = GetComponent<Selectable>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (boton != null && !boton.IsInteractable()) return;
        apretado = true;
        alEmpezar?.Invoke();
        alRepetir?.Invoke();
        proximo = Time.unscaledTime + espera;
    }

    public void OnPointerUp(PointerEventData eventData) { Soltar(); }
    public void OnPointerExit(PointerEventData eventData) { Soltar(); }
    void OnDisable() { Soltar(); }

    void Soltar()
    {
        if (!apretado) return;
        apretado = false;
        alTerminar?.Invoke();
    }

    void Update()
    {
        if (!apretado || Time.unscaledTime < proximo) return;
        proximo = Time.unscaledTime + intervalo;
        alRepetir?.Invoke();
    }
}
