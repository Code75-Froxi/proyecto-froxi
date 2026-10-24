using UnityEngine;

// ----------------------------------------------------------------------------
//  Entrada de la cueva: cuando Froxi la toca, se pasa al siguiente nivel
// ----------------------------------------------------------------------------
public class EntradaCueva : MonoBehaviour
{
    bool usada;
    float espera;

    void OnEnable() { usada = false; }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (usada || otro.GetComponent<JugadorMundo>() == null) return;
        if (Time.time < espera) return;
        if (!Progreso.CuevaAbierta)
        {
            espera = Time.time + 1.5f;
            Flujo.EntrarCueva();                       // solo muestra el aviso
            otro.transform.position += Vector3.down * 1.6f;
            return;
        }
        usada = true;
        Flujo.EntrarCueva();
    }
}
