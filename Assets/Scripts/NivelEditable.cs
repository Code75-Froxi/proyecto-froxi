using UnityEngine;
using UnityEngine.Tilemaps;

// ============================================================================
//  NIVEL EDITABLE
//  Va en el objeto "NivelEditable" que crea  Froxi > Nivel > Construir nivel en esta escena.
//  Ese objeto es el DIBUJO del nivel en la ventana Scene (para verlo y editarlo con la
//  Tile Palette). Al darle Play el juego arma el nivel de verdad desde el archivo
//  Nivel_XX.cs, asi que este dibujo se oculta para no duplicarlo.
//  Para que tus cambios cuenten:  Froxi > Nivel > Guardar nivel en el codigo.
// ============================================================================
public class NivelEditable : MonoBehaviour
{
    public int nivel;          // 0 = Nivel 1, 1 = Nivel 2 ...
    public Tilemap dibujo;     // el Tilemap donde se pinta
    public int ancho, alto;    // tamano del nivel en casillas

    void Awake()
    {
        gameObject.SetActive(false);
    }
}
