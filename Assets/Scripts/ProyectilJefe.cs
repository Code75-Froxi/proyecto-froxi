using UnityEngine;

// ----------------------------------------------------------------------------
//  Comida chatarra que lanza el monstruo. Si toca a Froxi le quita una vida.
//  Froxi puede saltarla o destruirla con sus frutas.
// ----------------------------------------------------------------------------
public class ProyectilJefe : MonoBehaviour
{
    public Vector2 vel;
    public float gravedad = 0f;
    float nace;

    void Start() { nace = Time.time; }

    void Update()
    {
        FroxiGame g = FroxiGame.I;
        if (g == null || g.estado != Estado.Jugando || g.jugador == null) return;
        vel.y -= gravedad * Time.deltaTime;
        transform.position += (Vector3)(vel * Time.deltaTime);
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);
        if (Time.time - nace > 5f || transform.position.y < -3f) { Destroy(gameObject); return; }

        Bounds b = g.jugador.col.bounds;
        b.Expand(-0.1f);
        float x = Mathf.Clamp(transform.position.x, b.min.x, b.max.x);
        float y = Mathf.Clamp(transform.position.y, b.min.y, b.max.y);
        float dx = transform.position.x - x, dy = transform.position.y - y;
        if (dx * dx + dy * dy <= 0.35f * 0.35f)
        {
            g.GolpeJefe(transform.position);
            Destroy(gameObject);
        }
    }

    // las frutas de Froxi la destruyen
    void OnTriggerEnter2D(Collider2D o)
    {
        if (o.GetComponent<Proyectil>() != null)
        {
            Destroy(o.gameObject);
            Destroy(gameObject);
        }
    }
}
