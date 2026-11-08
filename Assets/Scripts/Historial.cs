using System.Collections.Generic;
using UnityEngine;

// pila para deshacer la ultima accion
public static class Historial
{
    public enum Tipo { Colocar, Mover, Borrar, Estilo }

    class Accion
    {
        public Tipo tipo;
        public List<Mueble> muebles = new List<Mueble>();
        public List<Mueble.Estado> estados = new List<Mueble.Estado>();
    }

    const int maximo = 40;
    static readonly List<Accion> pila = new List<Accion>();

    public static int cantidad => pila.Count;
    public static Transform contenedor;

    static void Agregar(Accion a)
    {
        pila.Add(a);
        if (pila.Count > maximo)
        {
            var vieja = pila[0];
            pila.RemoveAt(0);
            // los borrados que ya no se pueden recuperar se destruyen de verdad
            if (vieja.tipo == Tipo.Borrar)
                foreach (var m in vieja.muebles)
                    if (m != null && !m.gameObject.activeSelf) Object.Destroy(m.gameObject);
        }
    }

    public static void Colocado(Mueble m)
    {
        var a = new Accion { tipo = Tipo.Colocar };
        a.muebles.Add(m);
        a.estados.Add(m.Guardar());
        Agregar(a);
    }

    public static void Movido(Mueble m, Mueble.Estado antes)
    {
        Agregar(Una(Tipo.Mover, m, antes));
    }

    public static void Estilo(Mueble m, Mueble.Estado antes)
    {
        Agregar(Una(Tipo.Estilo, m, antes));
    }

    public static void Borrado(List<Mueble> muebles)
    {
        if (muebles.Count == 0) return;
        var a = new Accion { tipo = Tipo.Borrar };
        foreach (var m in muebles)
        {
            a.muebles.Add(m);
            a.estados.Add(m.Guardar());
        }
        Agregar(a);
    }

    static Accion Una(Tipo t, Mueble m, Mueble.Estado e)
    {
        var a = new Accion { tipo = t };
        a.muebles.Add(m);
        a.estados.Add(e);
        return a;
    }

    // devuelve el nombre de lo que se deshizo
    public static string Deshacer()
    {
        while (pila.Count > 0)
        {
            var a = pila[pila.Count - 1];
            pila.RemoveAt(pila.Count - 1);

            bool hizoAlgo = false;
            for (int i = 0; i < a.muebles.Count; i++)
            {
                var m = a.muebles[i];
                if (m == null) continue;
                hizoAlgo = true;
                switch (a.tipo)
                {
                    case Tipo.Colocar:
                        m.AnimarSalida(() =>
                        {
                            m.gameObject.SetActive(false);
                            Object.Destroy(m.gameObject);
                            if (contenedor != null) Apoyo.ReacomodarTodos(contenedor);
                        });
                        break;
                    case Tipo.Borrar:
                        m.gameObject.SetActive(true);
                        m.Restaurar(a.estados[i]);
                        m.AnimarEntrada();
                        break;
                    default:
                        m.Restaurar(a.estados[i]);
                        break;
                }
            }
            if (!hizoAlgo) continue;
            return NombreDe(a.tipo);
        }
        return null;
    }

    static string NombreDe(Tipo t)
    {
        switch (t)
        {
            case Tipo.Colocar: return "colocar";
            case Tipo.Mover: return "mover";
            case Tipo.Borrar: return "borrar";
            default: return "cambio de estilo";
        }
    }

    public static void Limpiar()
    {
        pila.Clear();
    }
}
