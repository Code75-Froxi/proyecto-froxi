using UnityEngine;

// ----------------------------------------------------------------------------
//  Proyectil de Froxi: una fruta o verdura que sale disparada.
//  Elimina a la comida chatarra y le quita vida al monstruo jefe.
// ----------------------------------------------------------------------------
public class Proyectil : MonoBehaviour
{
    public float velocidad = 13f;
    public int dir = 1;
    public float vida = 1.5f;
    public int dano = 1;

    float nace;
    Rigidbody2D rb;

    void Start()
    {
        nace = Time.time;
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (rb != null) Fisica.PonerVel(rb, new Vector2(dir * velocidad, 0f));
    }

    void Update()
    {
        transform.Rotate(0f, 0f, -dir * 600f * Time.deltaTime);
        if (Time.time - nace > vida) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        ComidaChatarra c = o.GetComponent<ComidaChatarra>();
        if (c != null)
        {
            if (FroxiGame.I != null) FroxiGame.I.MatarChatarra(c);
            Destroy(gameObject);
            return;
        }
        JefeChatarra j = o.GetComponent<JefeChatarra>();
        if (j != null)
        {
            j.Golpe(dano);
            Destroy(gameObject);
            return;
        }
        if (!o.isTrigger && (o.name == "Suelo" || o.name == "Bloque" || o.name == "Pared")) Destroy(gameObject);
    }
}
