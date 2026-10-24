using UnityEngine;

// ----------------------------------------------------------------------------
//  FROXI (vista desde arriba): camina en 4 direcciones
// ----------------------------------------------------------------------------
public class JugadorMundo : MonoBehaviour
{
    public float velocidad = 4.5f;
    Sprite[][] anim;
    SpriteRenderer sr;
    Rigidbody2D rb;
    int dir = 3;
    Vector2 mov;

    void OnEnable() { mov = Vector2.zero; }

    public void Preparar(Sprite[][] a)
    {
        anim = a;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        sr.sprite = anim[dir][0];
    }

    void Update()
    {
        if (anim == null) return;
        float x = Flujo.Ocupado ? 0f : Input.GetAxisRaw("Horizontal");
        float y = Flujo.Ocupado ? 0f : Input.GetAxisRaw("Vertical");
        if (!Flujo.Ocupado)
        {
            if (Mathf.Abs(ControlTactil.H) > 0.1f) x = ControlTactil.H;     // controles tactiles
            if (Mathf.Abs(ControlTactil.V) > 0.1f) y = ControlTactil.V;
        }
        mov = new Vector2(x, y);
        if (mov.sqrMagnitude > 1f) mov.Normalize();

        if (Mathf.Abs(x) > Mathf.Abs(y)) dir = x > 0f ? 0 : 1;
        else if (Mathf.Abs(y) > 0f) dir = y > 0f ? 2 : 3;

        int cuadro = mov.sqrMagnitude > 0.01f ? (int)(Time.time * 9f) % 4 : 0;
        sr.sprite = anim[dir][cuadro];
    }

    void FixedUpdate()
    {
        if (rb != null) FisicaMundo.PonerVel(rb, mov * velocidad);
    }
}
