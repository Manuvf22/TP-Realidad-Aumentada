using UnityEngine;

// datos de cada mueble del catalogo
[CreateAssetMenu(fileName = "NuevoMueble", menuName = "Amueblar/Mueble")]
public class FurnitureData : ScriptableObject
{
    public string nombre;
    public string categoria;
    public GameObject prefab;
    public Sprite icono;
}
