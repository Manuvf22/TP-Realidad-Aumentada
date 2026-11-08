using System.Collections.Generic;
using UnityEngine;

// lista de muebles, el menu se arma solo con esto
[CreateAssetMenu(fileName = "Catalogo", menuName = "Amueblar/Catalogo")]
public class CatalogoMuebles : ScriptableObject
{
    public List<FurnitureData> muebles = new List<FurnitureData>();

    // categorias en el orden en que aparecen
    public List<string> Categorias()
    {
        var lista = new List<string>();
        foreach (var m in muebles)
            if (m != null && m.prefab != null && !lista.Contains(m.categoria)) lista.Add(m.categoria);
        return lista;
    }

    public List<int> IndicesDe(string categoria)
    {
        var lista = new List<int>();
        for (int i = 0; i < muebles.Count; i++)
            if (muebles[i] != null && muebles[i].prefab != null && muebles[i].categoria == categoria) lista.Add(i);
        return lista;
    }
}
