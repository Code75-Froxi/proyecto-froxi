using UnityEngine;

// ============================================================================
//  EL MONSTRUO DE LA COMIDA CHATARRA (jefe)
//  Camina hacia Froxi, salta y lanza comida chatarra.
//  Se le quita vida con las frutas que dispara Froxi (1 de dano) o saltandole encima (3 de dano).
//  Mientras siga vivo, la meta del nivel no se abre.
//  En el mapa se pone con la letra  J
// ============================================================================
public class JefeChatarra : MonoBehaviour
{
    public int vidaMax = 14;
    public int vida;
    public float izq, der;             // limites de la arena (coordenada x)
    public bool activo, muerto;
    public Sprite[] cuadros;
    public Sprite[] proyectiles;       // comida que lanza

    Rigidbody2D rb;
    BoxCollider2D col;
    SpriteRenderer sr;
    float proximoAtaque, proximoSalto, flashHasta, muereEn;
    int tanda;

    public bool Furioso { get { return vida <= vidaMax / 2; } }

    public void Iniciar()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        vida = vidaMax;
        proximoAtaque = Time.time + 2f;
        proximoSalto = Time.time + 3f;
    }

    bool EnSuelo()
    {
        Vector2 p = transform.position;
        return Physics2D.Raycast(p + Vector2.down * 0.98f, Vector2.down, 0.2f).collider != null;
    }

    public void Golpe(int dano)
    {
        if (muerto || !activo) return;
        vida -= dano;
        flashHasta = Time.time + 0.15f;
        if (FroxiGame.I != null) FroxiGame.I.Flota(transform.position + Vector3.up * 1.4f, "-" + dano, new Color(1f, 0.9f, 0.4f));
        if (vida <= 0)
        {
            vida = 0;
            muerto = true;
            muereEn = Time.time;
            col.enabled = false;
            Fisica.PonerVel(rb, Vector2.zero);
            rb.simulated = false;
            if (FroxiGame.I != null) FroxiGame.I.JefeDerrotado(this);
        }
    }

    void Update()
    {
        FroxiGame g = FroxiGame.I;
        if (g == null || sr == null) return;

        if (muerto)
        {
            // se encoge y desaparece
            float t = (Time.time - muereEn) / 1.2f;
            sr.color = new Color(1f, 0.5f, 0.5f, 1f - t);
            transform.localScale = Vector3.one * Mathf.Max(0.05f, 1f - t * 0.6f);
            transform.Rotate(0f, 0f, 220f * Time.deltaTime);
            if (t >= 1f) Destroy(gameObject);
            return;
        }
        if (g.estado != Estado.Jugando || g.jugador == null) return;

        JugadorFroxi pl = g.jugador;
        if (!activo && Mathf.Abs(pl.transform.position.x - transform.position.x) < 13f) activo = true;

        // animacion y destello rojo al recibir un golpe
        sr.sprite = cuadros[(int)(Time.time * (Furioso ? 9f : 6f)) % cuadros.Length];
        sr.color = Time.time < flashHasta ? new Color(1f, 0.35f, 0.35f) : (Furioso ? new Color(1f, 0.82f, 0.82f) : Color.white);
        sr.flipX = pl.transform.position.x > transform.position.x;
        if (!activo) return;

        // contacto con Froxi
        if (pl.col.bounds.Intersects(col.bounds))
        {
            bool pisada = pl.transform.position.y - transform.position.y > 0.9f && Fisica.Vel(pl.rb).y <= 0.5f;
            if (pisada && !pl.invulnerable)
            {
                Golpe(3);
                pl.Rebotar();
                pl.Invulnerabilidad(0.4f);
            }
            else g.GolpeJefe(transform.position);
        }

        // lanza comida chatarra
        if (Time.time > proximoAtaque) Lanzar(pl);
    }

    void FixedUpdate()
    {
        FroxiGame g = FroxiGame.I;
        if (muerto || !activo || g == null || g.estado != Estado.Jugando || g.jugador == null) return;

        float x = transform.position.x;
        float haciaFroxi = Mathf.Sign(g.jugador.transform.position.x - x);
        float v = Furioso ? 3.1f : 2.1f;
        // si esta muy cerca de Froxi se detiene para no empujarlo
        float vx = Mathf.Abs(g.jugador.transform.position.x - x) < 1.2f ? 0f : haciaFroxi * v;
        if ((x < izq && vx < 0f) || (x > der && vx > 0f)) vx = 0f;
        Vector2 vel = Fisica.Vel(rb);

        if (Time.time > proximoSalto && EnSuelo())
        {
            vel.y = 15f;
            proximoSalto = Time.time + (Furioso ? 2.6f : 3.8f);
        }
        Fisica.PonerVel(rb, new Vector2(vx, vel.y));
    }

    void Lanzar(JugadorFroxi pl)
    {
        tanda++;
        float dir = Mathf.Sign(pl.transform.position.x - transform.position.x);
        Sprite sp = proyectiles != null && proyectiles.Length > 0 ? proyectiles[tanda % proyectiles.Length] : null;

        // un tiro a ras de piso (hay que saltarlo o destruirlo)
        Disparo(sp, new Vector2(dir * (Furioso ? 8f : 6.5f), 0f), 0f, -0.35f);
        // y, en modo furioso, otro en arco
        if (Furioso && tanda % 2 == 0) Disparo(sp, new Vector2(dir * 4.5f, 10f), 24f, 0.5f);

        proximoAtaque = Time.time + (Furioso ? 1.7f : 2.6f);
    }

    void Disparo(Sprite sp, Vector2 vel, float gravedad, float dy)
    {
        GameObject p = new GameObject("BalaJefe");
        p.transform.position = transform.position + new Vector3(Mathf.Sign(vel.x) * 1.1f, dy, 0f);
        p.transform.SetParent(transform.parent);
        p.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        SpriteRenderer s = p.AddComponent<SpriteRenderer>();
        s.sprite = sp;
        s.sortingOrder = 6;
        CircleCollider2D cc = p.AddComponent<CircleCollider2D>();
        cc.radius = 0.3f; cc.isTrigger = true;
        ProyectilJefe pj = p.AddComponent<ProyectilJefe>();
        pj.vel = vel; pj.gravedad = gravedad;
    }
}
