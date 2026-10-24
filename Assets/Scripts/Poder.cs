using UnityEngine;

// ============================================================================
//  PODERES que Froxi puede recoger en los niveles (burbujas con un dibujo):
//    e = Escudo      te salva de perder UNA vida (una caida o una respuesta mala)
//    s = Super salto  salta mucho mas alto por unos segundos
//    v = Velocidad    corre mas rapido por unos segundos
//    t = Tiempo       suma segundos al reloj
//    h = Vida         te regresa un corazon
// ============================================================================
public enum TipoPoder { Escudo, Salto, Velocidad, Tiempo, Vida }

public class Poder : MonoBehaviour
{
    public TipoPoder tipo;
    Vector3 basePos;
    float fase;

    void Start()
    {
        basePos = transform.position;
        fase = Random.value * 6.28f;
    }

    void Update()
    {
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 3f + fase) * 0.1f;
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        if (FroxiGame.I != null && o.GetComponent<JugadorFroxi>() != null)
            FroxiGame.I.RecogerPoder(this);
    }
}
