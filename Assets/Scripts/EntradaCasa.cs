using UnityEngine;

// ----------------------------------------------------------------------------
//  Puerta de la casita (mundo armado por codigo): cuando Froxi la toca, entra al nivel de la escuela
// ----------------------------------------------------------------------------
public class EntradaCasa : MonoBehaviour
{
    bool usada;

    void OnEnable() { usada = false; }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (usada || otro.GetComponent<JugadorMundo>() == null) return;
        usada = true;
        Flujo.EntrarCasa();
    }
}
