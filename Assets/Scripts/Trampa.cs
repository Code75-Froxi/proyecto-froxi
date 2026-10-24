using UnityEngine;

// ============================================================================
//  TRAMPAS Y OBSTACULOS (letras del mapa):
//    ^  pinchos en el piso            o  sierra que va y viene
//    a  hacha doble que se balancea   m  maza con puas que se balancea
//    p  tabla de puas que cae         l  tronco con puas
//  Si Froxi las toca pierde una vida (el escudo lo salva) y sale empujado.
//  Las trampas NO se pueden destruir: hay que saltarlas o cruzar en el momento justo.
// ============================================================================
public enum TipoTrampa { Pinchos, Sierra, Hacha, Maza, Tronco, Tabla }

public class Trampa : MonoBehaviour
{
    public TipoTrampa tipo;
    public float periodo = 3f;        // segundos que tarda un ciclo
    public float amplitud = 50f;      // pendulos: grados a cada lado
    public float recorrido = 2f;      // sierra: casillas a cada lado | tabla: cuanto baja
    public Vector2 caja = new Vector2(0.8f, 0.5f);   // zona que lastima (pinchos, tronco, tabla)
    public Vector2 centro = Vector2.zero;            // desplazamiento de esa zona
    public float radio = 0.45f;                      // sierra, hacha y maza
    public Transform cabeza;                         // pendulos: el objeto que lastima
    public float fase;

    Vector3 origen;
    bool listo;

    public void Iniciar()
    {
        origen = transform.position;
        listo = true;
    }

    void Update()
    {
        FroxiGame g = FroxiGame.I;
        if (!listo || g == null || g.estado != Estado.Jugando || g.jugador == null) return;
        float t = Time.time + fase;

        switch (tipo)
        {
            case TipoTrampa.Sierra:
                transform.position = origen + new Vector3(Mathf.Sin(t * Mathf.PI * 2f / periodo) * recorrido, 0f, 0f);
                break;
            case TipoTrampa.Hacha:
            case TipoTrampa.Maza:
                transform.rotation = Quaternion.Euler(0f, 0f, amplitud * Mathf.Sin(t * Mathf.PI * 2f / periodo));
                break;
            case TipoTrampa.Tabla:
                {
                    float p = Mathf.Repeat(t, periodo) / periodo;
                    float k;
                    if (p < 0.40f) k = 0f;                              // espera arriba
                    else if (p < 0.48f) k = (p - 0.40f) / 0.08f;        // cae rapido
                    else if (p < 0.65f) k = 1f;                         // se queda abajo
                    else k = 1f - (p - 0.65f) / 0.35f;                  // sube despacio
                    transform.position = origen + new Vector3(0f, -recorrido * k, 0f);
                    break;
                }
        }

        // contacto con Froxi
        Bounds b = g.jugador.col.bounds;
        b.Expand(-0.12f);
        bool toca;
        if (tipo == TipoTrampa.Sierra) toca = Circulo(transform.position, radio, b);
        else if (tipo == TipoTrampa.Hacha || tipo == TipoTrampa.Maza) toca = cabeza != null && Circulo(cabeza.position, radio, b);
        else toca = new Bounds(transform.position + (Vector3)centro, new Vector3(caja.x, caja.y, 10f)).Intersects(b);

        if (toca) g.GolpeTrampa(this);
    }

    static bool Circulo(Vector3 c, float r, Bounds b)
    {
        float x = Mathf.Clamp(c.x, b.min.x, b.max.x);
        float y = Mathf.Clamp(c.y, b.min.y, b.max.y);
        float dx = c.x - x, dy = c.y - y;
        return dx * dx + dy * dy <= r * r;
    }
}
