using System.Collections.Generic;
using UnityEngine;

public enum Estado { Menu, Jugando, Pregunta, Retroalimentacion, Pausa, FinNivel, GameOver, Victoria, TiempoAgotado, Reto, RetoResultado }

// Este script se crea SOLO al darle Play (no tienes que ponerlo en ningun objeto).
// Construye el nivel, la camara, el menu, los puntos, las vidas y las preguntas.
public class FroxiGame : MonoBehaviour
{
    public static FroxiGame I;

    // ============ AJUSTES FACILES (cambialos a tu gusto) ============
    public const int VIDAS_INICIALES = 3;
    public const int PUNTOS_SALUDABLE = 10;          // por cada alimento saludable
    public const int PUNTOS_RESPUESTA_CORRECTA = 20; // por contestar bien
    public const int PUNTOS_PISAR_CHATARRA = 5;      // por saltarle encima a la chatarra
    public const int BONUS_POR_VIDA = 10;            // al terminar el nivel, por cada vida
    public const bool PISAR_VENCE_CHATARRA = true;   // false = siempre sale la pregunta
    public const float SEGUNDOS_INVULNERABLE = 2f;
    public const int PUNTOS_POR_SEGUNDO = 2;         // al terminar, por cada segundo que sobre
    public const int PUNTOS_PODER = 5;               // por recoger un poder
    public const float SEGUNDOS_SUPER_SALTO = 10f;
    public const float SEGUNDOS_VELOCIDAD = 8f;
    public const float SEGUNDOS_TIEMPO_EXTRA = 10f;
    public const float FUERZA_SUPER_SALTO = 1.35f;   // multiplica el salto
    public const float FUERZA_VELOCIDAD = 1.45f;     // multiplica la velocidad
    public const float SEGUNDOS_ENTRE_DISPAROS = 0.32f;
    public const int PUNTOS_DISPARO = 10;            // por derrotar chatarra con una fruta
    public const int PUNTOS_RETO = 30;               // por acertar el reto del lanzafrutas
    public const int PUNTOS_JEFE = 150;              // por derrotar al monstruo
    // ================================================================

    public Estado estado = Estado.Menu;
    public int puntos, vidas, nivelActual, saludables, totalSaludables;
    public JugadorFroxi jugador;

    // reloj y poderes
    public float tiempoRestante;
    public bool escudo;
    bool relojCorre, escudoSalvo;
    float finSalto, finVelocidad;
    int bonusTiempo, disparos;
    GameObject auraObj;

    // lanzafrutas (se gana contestando bien el reto del inicio del nivel) y jefe
    public bool tieneArma;
    public JefeChatarra jefe;
    float proximoDisparo, avisoMeta;
    bool retoAcertado;
    static readonly List<int> retoPool = new List<int>();

    Camera cam;
    GameObject raiz;
    Transform fondoT;
    int W, H;
    float cx0, introHasta;
    int puntosInicioNivel;
    NivelDatos nivel;
    readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // pregunta
    Pregunta preguntaActual;
    int[] orden = new int[] { 0, 1, 2 };
    bool ultimaCorrecta;
    ComidaChatarra chatarraPendiente;
    readonly List<int> pool = new List<int>();

    // textos flotantes (+10)
    class Flotante { public Vector3 pos; public string txt; public float t; public Color color; }
    readonly List<Flotante> flot = new List<Flotante>();

    // En el kit completo el que arranca todo es "Flujo" (portada, niveles, mundo).
    // Si quieres probar SOLO el juego de plataformas, pon true.
    public const bool AUTO_INICIAR = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        if (AUTO_INICIAR && I == null) new GameObject("FroxiGame").AddComponent<FroxiGame>();
    }

    // La llama Flujo para empezar directamente en un nivel
    public void Empezar(int nivelInicial) { NuevaPartida(Mathf.Clamp(nivelInicial, 0, Niveles.Todos.Length - 1)); }

    void OnDestroy()
    {
        LimpiarNivel();
        Time.timeScale = 1f;
        if (I == this) I = null;
    }

    void Awake()
    {
        I = this;
        Time.timeScale = 1f;
        Physics2D.queriesStartInColliders = false;
        Physics2D.queriesHitTriggers = false;

        cam = Camera.main;
        if (cam == null)
        {
            GameObject g = new GameObject("Main Camera");
            g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.72f, 0.92f);
        cam.transform.position = new Vector3(0f, 4.5f, -10f);
        estado = Estado.Menu;
    }

    // ---------------------------------------------------------------- sprites
    public Sprite S(string n)
    {
        Sprite s;
        if (!cache.TryGetValue(n, out s))
        {
            s = Resources.Load<Sprite>("Sprites/" + n);
            if (s == null) s = Resources.Load<Sprite>("UI/" + n);
            if (s == null) Debug.LogError("Falta el sprite: Assets/Resources/Sprites/" + n + ".png");
            cache[n] = s;
        }
        return s;
    }

    public Sprite[] Cuadros(string prefijo, int n)
    {
        Sprite[] a = new Sprite[n];
        for (int i = 0; i < n; i++) a[i] = S(prefijo + "_" + i);
        return a;
    }

    // ---------------------------------------------------------------- flujo del juego
    void NuevaPartida(int nivelInicial)
    {
        puntos = 0;
        nivelActual = nivelInicial;
        CargarNivel(nivelActual);
    }

    void LimpiarNivel()
    {
        if (raiz != null) Destroy(raiz);
        raiz = null; jugador = null; fondoT = null; auraObj = null; jefe = null;
        flot.Clear();
    }

    void IrAlMenu()
    {
        if (Flujo.Activo) { Flujo.IrA(Pantalla.Portada); return; }
        LimpiarNivel();
        estado = Estado.Menu;
        Time.timeScale = 1f;
        cam.transform.position = new Vector3(0f, 4.5f, -10f);
    }

    GameObject Nuevo(string nombre, Vector3 pos)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(raiz.transform);
        g.transform.position = pos;
        return g;
    }

    SpriteRenderer ConSprite(GameObject g, Sprite s, int capa)
    {
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = capa;
        return sr;
    }

    char Tile(int c, int r)
    {
        if (r < 0 || r >= H || c < 0 || c >= W) return '.';
        return nivel.mapa[r][c];
    }

    Vector3 Pos(int c, int r) { return new Vector3(c, H - 1 - r, 0f); }

    void CargarNivel(int idx)
    {
        LimpiarNivel();
        nivel = Niveles.Todos[idx];
        H = nivel.mapa.Length;
        W = nivel.mapa[0].Length;
        raiz = new GameObject("Nivel");
        saludables = 0; totalSaludables = 0;
        vidas = VIDAS_INICIALES;
        puntosInicioNivel = puntos;
        chatarraPendiente = null;
        pool.Clear();                       // cada nivel usa su propia dificultad de preguntas
        tiempoRestante = nivel.tiempo;
        relojCorre = false;
        escudo = false; escudoSalvo = false;
        finSalto = 0f; finVelocidad = 0f; bonusTiempo = 0;
        tieneArma = nivel.armaGratis; jefe = null; proximoDisparo = 0f;

        // ---- fondo con parallax
        Sprite fs = S(nivel.fondo);
        GameObject fondo = Nuevo("Fondo", new Vector3(W / 2f, (H - 1) / 2f, 0f));
        SpriteRenderer fsr = ConSprite(fondo, fs, -10);
        fsr.drawMode = SpriteDrawMode.Tiled;
        fsr.size = new Vector2(W * 2f + 60f, fs.bounds.size.y);
        fondoT = fondo.transform;
        cx0 = W / 2f;

        // ---- suelo y plataformas (se juntan en tiras para que sea ligero)
        for (int r = 0; r < H; r++)
        {
            int c = 0;
            while (c < W)
            {
                char ch = Tile(c, r);
                if (ch == '#' || ch == '=')
                {
                    int c0 = c;
                    while (c < W && Tile(c, r) == ch) c++;
                    if (ch == '#') CrearSuelo(c0, c - c0, r); else CrearPlataforma(c0, c - c0, r);
                }
                else c++;
            }
        }

        // ---- bloques solidos
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
                if (Tile(c, r) == 'B')
                {
                    GameObject b = Nuevo("Bloque", Pos(c, r));
                    ConSprite(b, S(nivel.bloque), 1);
                    b.AddComponent<BoxCollider2D>().size = Vector2.one;
                }

        // ---- paredes invisibles a los lados
        CrearPared(-1.0f); CrearPared(W);

        // ---- Froxi primero (los enemigos lo necesitan)
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
                if (Tile(c, r) == 'P') CrearJugador(c, r);
        if (jugador == null) CrearJugador(2, H - 3);

        // ---- comida, enemigos y meta
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
            {
                char ch = Tile(c, r);
                if (ch == 'f') CrearSaludable(c, r);
                else if (ch == 'j' || ch == 'x') CrearChatarra(c, r, ch == 'j');
                else if (ch == 'G') CrearMeta(c, r);
                else if (ch == 'e') CrearPoder(c, r, TipoPoder.Escudo);
                else if (ch == 's') CrearPoder(c, r, TipoPoder.Salto);
                else if (ch == 'v') CrearPoder(c, r, TipoPoder.Velocidad);
                else if (ch == 't') CrearPoder(c, r, TipoPoder.Tiempo);
                else if (ch == 'h') CrearPoder(c, r, TipoPoder.Vida);
                else if (ch == '^' || ch == 'o' || ch == 'a' || ch == 'm' || ch == 'p' || ch == 'l') CrearTrampa(c, r, ch);
                else if (ch == 'J') CrearJefe(c, r);
            }

        PosicionarCamara(true);
        if (nivel.reto && !nivel.armaGratis) PrepararReto();
        else EmpezarAJugar();
    }

    void EmpezarAJugar()
    {
        estado = Estado.Jugando;
        Time.timeScale = 1f;
        introHasta = Time.unscaledTime + 3.5f;
    }

    // saca un reto (pregunta muy dificil) para ganar el lanzafrutas
    void PrepararReto()
    {
        if (retoPool.Count == 0)
        {
            for (int i = 0; i < BancoPreguntas.Retos.Length; i++) retoPool.Add(i);
            Mezclar(retoPool);
        }
        int k = retoPool[retoPool.Count - 1];
        retoPool.RemoveAt(retoPool.Count - 1);
        preguntaActual = BancoPreguntas.Retos[k];
        MezclarOrden();
        estado = Estado.Reto;
        Time.timeScale = 0f;
    }

    void MezclarOrden()
    {
        orden = new int[] { 0, 1, 2 };
        for (int i = 2; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int t = orden[i]; orden[i] = orden[j]; orden[j] = t;
        }
    }

    void ResponderReto(int i)
    {
        if (estado != Estado.Reto) return;
        retoAcertado = orden[i] == 0;
        tieneArma = retoAcertado;
        if (retoAcertado) puntos += PUNTOS_RETO;
        estado = Estado.RetoResultado;
    }

    // ---------------------------------------------------------------- trampas y jefe
    // y (en el mundo) de la superficie del primer suelo que hay debajo de esa casilla
    float SuperficieBajo(int c, int r)
    {
        for (int rr = r; rr < H; rr++)
        {
            char t = Tile(c, rr);
            if (t == '#' || t == 'B' || t == '=') return (H - 1 - rr) + 0.5f;
        }
        return 0.5f;
    }

    void CrearTrampa(int c, int r, char ch)
    {
        Vector3 p = Pos(c, r);
        float suelo = SuperficieBajo(c, r);
        GameObject g = Nuevo("Trampa", p);
        Trampa t = g.AddComponent<Trampa>();
        t.fase = c * 0.61f;

        switch (ch)
        {
            case '^':
                t.tipo = TipoTrampa.Pinchos;
                ConSprite(g, S("trampa_pinchos"), 2);
                t.caja = new Vector2(0.8f, 0.5f); t.centro = new Vector2(0f, -0.22f);
                break;
            case 'l':
                t.tipo = TipoTrampa.Tronco;
                ConSprite(g, S("trampa_tronco"), 2);
                t.caja = new Vector2(0.95f, 0.85f); t.centro = new Vector2(0f, -0.05f);
                break;
            case 'o':
                {
                    t.tipo = TipoTrampa.Sierra;
                    ConSprite(g, S("trampa_sierra_0"), 2);
                    AnimacionSprite an = g.AddComponent<AnimacionSprite>();
                    an.cuadros = Cuadros("trampa_sierra", 4);
                    an.fps = 16f;
                    t.radio = 0.42f; t.periodo = 3.4f; t.recorrido = 2f;
                    break;
                }
            case 'a':
            case 'm':
                {
                    bool hacha = ch == 'a';
                    t.tipo = hacha ? TipoTrampa.Hacha : TipoTrampa.Maza;
                    float largo = Mathf.Max(1.6f, p.y - (suelo + (hacha ? 0.85f : 1.0f)));
                    t.amplitud = hacha ? 48f : 58f;
                    t.periodo = hacha ? 2.8f : 3.4f;
                    t.radio = hacha ? 0.55f : 0.5f;
                    int n = Mathf.Max(1, Mathf.CeilToInt(largo - 0.5f));
                    float paso = (largo - 0.5f) / n;
                    for (int k = 0; k < n; k++)
                    {
                        GameObject e = new GameObject("Cadena");
                        e.transform.SetParent(g.transform, false);
                        e.transform.localPosition = new Vector3(0f, -paso * (k + 0.5f), 0f);
                        e.transform.localScale = new Vector3(1f, paso, 1f);
                        ConSprite(e, S("trampa_cadena"), 2);
                    }
                    GameObject cab = new GameObject(hacha ? "Hacha" : "Maza");
                    cab.transform.SetParent(g.transform, false);
                    cab.transform.localPosition = new Vector3(0f, -largo + 0.1f, 0f);
                    ConSprite(cab, S(hacha ? "trampa_hacha" : "trampa_maza"), 3);
                    t.cabeza = cab.transform;
                    break;
                }
            case 'p':
                t.tipo = TipoTrampa.Tabla;
                ConSprite(g, S("trampa_tabla"), 3);
                t.caja = new Vector2(0.95f, 0.95f);
                t.periodo = 3.2f;
                t.recorrido = Mathf.Max(1f, p.y - (suelo + 0.5f));
                break;
        }
        t.Iniciar();
    }

    void CrearJefe(int c, int r)
    {
        float suelo = SuperficieBajo(c, r);
        GameObject g = Nuevo("Jefe", new Vector3(c, suelo + 1.05f, 0f));
        ConSprite(g, S("jefe_0"), 4);
        Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(1.7f, 1.8f);
        bc.offset = new Vector2(0f, -0.1f);
        PhysicsMaterial2D mat = new PhysicsMaterial2D("sinFriccion");
        mat.friction = 0f; mat.bounciness = 0f;
        bc.sharedMaterial = mat;
        if (jugador != null) Physics2D.IgnoreCollision(jugador.col, bc);
        JefeChatarra j = g.AddComponent<JefeChatarra>();
        j.cuadros = Cuadros("jefe", 4);
        j.proyectiles = new Sprite[] { S("chatarra_dona_0"), S("chatarra_papas_0"), S("chatarra_hamburguesa_0"), S("chatarra_refresco_0") };
        j.izq = Mathf.Max(1f, c - 18f);
        j.der = Mathf.Min(W - 3f, c + 18f);
        j.Iniciar();
        jefe = j;
    }

    void CrearSuelo(int c0, int largo, int r)
    {
        GameObject g = Nuevo("Suelo", new Vector3(c0 + (largo - 1) / 2f, H - 1 - r, 0f));
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(largo, 1f);
        for (int k = 0; k < largo; k++)
        {
            GameObject t = new GameObject("t");
            t.transform.SetParent(g.transform);
            t.transform.position = Pos(c0 + k, r);
            bool arriba = Tile(c0 + k, r - 1) == '#';
            ConSprite(t, S(arriba ? nivel.cuerpo : nivel.top), 0);
        }
    }

    void CrearPlataforma(int c0, int largo, int r)
    {
        GameObject g = Nuevo("Plataforma", new Vector3(c0 + (largo - 1) / 2f, H - 1 - r, 0f));
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(largo, 0.3f);
        bc.offset = new Vector2(0f, 0.35f);
        PlatformEffector2D ef = g.AddComponent<PlatformEffector2D>();
        ef.useOneWay = true;
        ef.surfaceArc = 160f;
        ef.useSideFriction = false;
        bc.usedByEffector = true;
        for (int k = 0; k < largo; k++)
        {
            GameObject t = new GameObject("t");
            t.transform.SetParent(g.transform);
            t.transform.position = Pos(c0 + k, r);
            string n = nivel.plataformaM;
            if (largo > 1 && k == 0) n = nivel.plataformaL;
            else if (largo > 1 && k == largo - 1) n = nivel.plataformaR;
            ConSprite(t, S(n), 1);
        }
    }

    void CrearPared(float x)
    {
        GameObject g = Nuevo("Pared", new Vector3(x, (H - 1) / 2f, 0f));
        g.AddComponent<BoxCollider2D>().size = new Vector2(1f, 60f);
    }

    void CrearJugador(int c, int r)
    {
        GameObject g = Nuevo("Froxi", Pos(c, r));
        SpriteRenderer sr = ConSprite(g, S("froxi_der_1"), 5);
        Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.6f, 0.75f);
        bc.offset = new Vector2(0f, -0.095f);
        PhysicsMaterial2D mat = new PhysicsMaterial2D("sinFriccion");
        mat.friction = 0f; mat.bounciness = 0f;
        bc.sharedMaterial = mat;
        jugador = g.AddComponent<JugadorFroxi>();
        jugador.caminar = Cuadros("froxi_der", 4);
        jugador.ultimaSegura = g.transform.position;

        // burbuja azul que se ve mientras Froxi tiene el escudo
        GameObject aura = new GameObject("Aura");
        aura.transform.SetParent(g.transform, false);
        aura.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
        SpriteRenderer asr = aura.AddComponent<SpriteRenderer>();
        asr.sprite = S("aura_escudo");
        asr.sortingOrder = 6;
        aura.SetActive(false);
        auraObj = aura;
    }

    void CrearPoder(int c, int r, TipoPoder tipo)
    {
        string n = "poder_" + tipo.ToString().ToLower();   // poder_escudo, poder_salto, poder_velocidad, poder_tiempo, poder_vida
        GameObject g = Nuevo("Poder " + tipo, Pos(c, r));
        ConSprite(g, S(n + "_0"), 3);
        AnimacionSprite an = g.AddComponent<AnimacionSprite>();
        an.cuadros = Cuadros(n, 4);
        an.fps = 6f;
        CircleCollider2D cc = g.AddComponent<CircleCollider2D>();
        cc.radius = 0.5f; cc.isTrigger = true;
        Poder p = g.AddComponent<Poder>();
        p.tipo = tipo;
    }

    void CrearSaludable(int c, int r)
    {
        string[] n = { "saludable_manzana", "saludable_brocoli", "saludable_zanahoria", "saludable_fresa" };
        GameObject g = Nuevo("Saludable", Pos(c, r));
        g.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        ConSprite(g, S(n[(c + r) % 4] + "_0"), 3);
        AnimacionSprite an = g.AddComponent<AnimacionSprite>();
        an.cuadros = Cuadros(n[(c + r) % 4], 4);
        an.fps = 5f;
        CircleCollider2D cc = g.AddComponent<CircleCollider2D>();
        cc.radius = 0.45f; cc.isTrigger = true;
        g.AddComponent<ComidaSaludable>();
        totalSaludables++;
    }

    void CrearChatarra(int c, int r, bool camina)
    {
        string[] n = { "chatarra_hamburguesa", "chatarra_papas", "chatarra_dona", "chatarra_refresco" };
        string nom = n[(c * 3 + r) % 4];
        GameObject g = Nuevo("Chatarra", Pos(c, r));
        ConSprite(g, S(nom + "_0"), 4);
        AnimacionSprite an = g.AddComponent<AnimacionSprite>();
        an.cuadros = Cuadros(nom, 4);
        an.fps = camina ? 6f : 3f;
        Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.7f, 0.8f);
        bc.offset = new Vector2(0f, -0.05f);
        PhysicsMaterial2D mat = new PhysicsMaterial2D("sinFriccion");
        mat.friction = 0f; mat.bounciness = 0f;
        bc.sharedMaterial = mat;
        ComidaChatarra cc = g.AddComponent<ComidaChatarra>();
        cc.patrulla = camina;
        if (jugador != null) Physics2D.IgnoreCollision(jugador.col, bc);
    }

    void CrearMeta(int c, int r)
    {
        Sprite sp = S(nivel.meta);
        int rr = r;
        while (rr < H - 1 && Tile(c, rr) != '#') rr++;
        float superficie = (H - 1 - rr) + 0.5f;
        float ancho = sp.bounds.size.x, alto = sp.bounds.size.y;
        GameObject g = Nuevo("Meta", new Vector3(c + (ancho - 1f) / 2f, superficie + alto / 2f, 0f));
        ConSprite(g, sp, 2);
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(Mathf.Min(1.4f, ancho), alto);
        g.AddComponent<MetaNivel>();
    }

    // ---------------------------------------------------------------- eventos del juego
    public void RecogerSaludable(ComidaSaludable f)
    {
        if (estado != Estado.Jugando) return;
        puntos += PUNTOS_SALUDABLE;
        saludables++;
        Flota(f.transform.position, "+" + PUNTOS_SALUDABLE, new Color(1f, 0.92f, 0.3f));
        Destroy(f.gameObject);
    }

    public void RecogerPoder(Poder p)
    {
        if (estado != Estado.Jugando) return;
        string txt = "";
        Color col = new Color(0.7f, 0.95f, 1f);
        switch (p.tipo)
        {
            case TipoPoder.Escudo:
                escudo = true; txt = "¡Escudo!"; break;
            case TipoPoder.Salto:
                finSalto = Time.time + SEGUNDOS_SUPER_SALTO; txt = "¡Super salto!"; break;
            case TipoPoder.Velocidad:
                finVelocidad = Time.time + SEGUNDOS_VELOCIDAD; txt = "¡Velocidad!"; break;
            case TipoPoder.Tiempo:
                if (nivel != null && nivel.tiempo > 0) tiempoRestante += SEGUNDOS_TIEMPO_EXTRA;
                txt = "+" + (int)SEGUNDOS_TIEMPO_EXTRA + " seg"; break;
            case TipoPoder.Vida:
                if (vidas < VIDAS_INICIALES) { vidas++; txt = "+1 vida"; }
                else { puntos += 20; txt = "+20"; }
                break;
        }
        puntos += PUNTOS_PODER;
        Flota(p.transform.position + Vector3.up * 0.6f, txt, col);
        Destroy(p.gameObject);
    }

    // ---- disparo
    void Disparar()
    {
        if (!tieneArma || jugador == null || Time.time < proximoDisparo) return;
        proximoDisparo = Time.time + SEGUNDOS_ENTRE_DISPAROS;
        int dir = jugador.Dir;
        string[] n = { "saludable_manzana", "saludable_brocoli", "saludable_zanahoria", "saludable_fresa" };
        GameObject g = Nuevo("Disparo", jugador.transform.position + new Vector3(dir * 0.55f, 0.05f, 0f));
        g.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        ConSprite(g, S(n[disparos % 4] + "_0"), 7);
        disparos++;
        Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        CircleCollider2D cc = g.AddComponent<CircleCollider2D>();
        cc.radius = 0.6f; cc.isTrigger = true;
        Proyectil pr = g.AddComponent<Proyectil>();
        pr.dir = dir;
    }

    public void MatarChatarra(ComidaChatarra c)
    {
        if (c == null || estado != Estado.Jugando) return;
        puntos += PUNTOS_DISPARO;
        Flota(c.transform.position, "+" + PUNTOS_DISPARO, new Color(1f, 0.92f, 0.3f));
        Destroy(c.gameObject);
    }

    // ---- danos sin pregunta: trampas y monstruo
    public void GolpeTrampa(Trampa t) { Herir(t.transform.position); }
    public void GolpeJefe(Vector3 desde) { Herir(desde); }

    void Herir(Vector3 desde)
    {
        if (estado != Estado.Jugando || jugador == null || jugador.invulnerable) return;
        bool perdio = PerderVida();
        jugador.Invulnerabilidad(SEGUNDOS_INVULNERABLE);
        jugador.Empujar(jugador.transform.position.x - desde.x);
        Flota(jugador.transform.position + Vector3.up * 1.2f, perdio ? "-1 vida" : "¡Escudo!",
              perdio ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 0.85f, 1f));
        if (vidas <= 0)
        {
            estado = Estado.GameOver;
            Time.timeScale = 0f;
        }
    }

    public void JefeDerrotado(JefeChatarra j)
    {
        puntos += PUNTOS_JEFE;
        Flota(j.transform.position + Vector3.up * 1.6f, "¡Monstruo derrotado! +" + PUNTOS_JEFE, new Color(1f, 0.92f, 0.3f));
    }

    // Quita una vida, salvo que Froxi tenga el escudo. Devuelve true si de verdad la perdio.
    bool PerderVida()
    {
        if (escudo) { escudo = false; escudoSalvo = true; return false; }
        escudoSalvo = false;
        vidas--;
        return true;
    }

    // Froxi se cayo a un hueco: pierde una vida (o la salva el escudo) y vuelve al ultimo lugar seguro
    public void Caida()
    {
        if (estado != Estado.Jugando || jugador == null) return;
        bool perdio = PerderVida();
        jugador.Reaparecer();
        Flota(jugador.transform.position + Vector3.up * 1.2f, perdio ? "-1 vida" : "¡Escudo!",
              perdio ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 0.85f, 1f));
        if (vidas <= 0)
        {
            estado = Estado.GameOver;
            Time.timeScale = 0f;
        }
    }

    void AgotarTiempo()
    {
        tiempoRestante = 0f;
        estado = Estado.TiempoAgotado;
        Time.timeScale = 0f;
    }

    public void ContactoChatarra(ComidaChatarra c, bool pisada)
    {
        if (estado != Estado.Jugando || jugador == null || jugador.invulnerable) return;

        if (pisada && PISAR_VENCE_CHATARRA)
        {
            puntos += PUNTOS_PISAR_CHATARRA;
            Flota(c.transform.position, "+" + PUNTOS_PISAR_CHATARRA, Color.white);
            jugador.Rebotar();
            Destroy(c.gameObject);
            return;
        }

        chatarraPendiente = c;
        c.Congelar();
        preguntaActual = SiguientePregunta();
        MezclarOrden();
        estado = Estado.Pregunta;
        Time.timeScale = 0f;
    }

    Pregunta SiguientePregunta()
    {
        if (pool.Count == 0)
        {
            int dif = nivel != null ? nivel.dificultad : 0;
            List<int> propias = new List<int>();     // de la dificultad del nivel: salen primero
            List<int> faciles = new List<int>();     // de la dificultad de abajo: solo cuando se acaban las otras
            for (int i = 0; i < BancoPreguntas.Todas.Length; i++)
            {
                Pregunta q = BancoPreguntas.Todas[i];
                if (!BancoPreguntas.Sirve(q, dif)) continue;
                if (dif > 0 && q.dificultad < dif) faciles.Add(i); else propias.Add(i);
            }
            if (propias.Count + faciles.Count == 0)
                for (int i = 0; i < BancoPreguntas.Todas.Length; i++) propias.Add(i);
            Mezclar(faciles);
            Mezclar(propias);
            pool.AddRange(faciles);                  // se sacan del final: primero las 'propias'
            pool.AddRange(propias);
        }
        int k = pool[pool.Count - 1];
        pool.RemoveAt(pool.Count - 1);
        return BancoPreguntas.Todas[k];
    }

    static void Mezclar(List<int> l)
    {
        for (int i = l.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int t = l[i]; l[i] = l[j]; l[j] = t;
        }
    }

    string Opcion(int i)
    {
        int o = orden[i];
        return o == 0 ? preguntaActual.correcta : (o == 1 ? preguntaActual.mala1 : preguntaActual.mala2);
    }

    void Responder(int i)
    {
        if (estado != Estado.Pregunta) return;
        ultimaCorrecta = orden[i] == 0;
        escudoSalvo = false;
        if (ultimaCorrecta) puntos += PUNTOS_RESPUESTA_CORRECTA;
        else PerderVida();
        estado = Estado.Retroalimentacion;
    }

    void ContinuarTrasPregunta()
    {
        if (chatarraPendiente != null) Destroy(chatarraPendiente.gameObject);
        chatarraPendiente = null;
        if (vidas <= 0)
        {
            estado = Estado.GameOver;
            Time.timeScale = 0f;
            return;
        }
        estado = Estado.Jugando;
        Time.timeScale = 1f;
        if (jugador != null) jugador.Invulnerabilidad(SEGUNDOS_INVULNERABLE);
    }

    public void TerminarNivel()
    {
        if (estado != Estado.Jugando) return;
        if (jefe != null && !jefe.muerto)
        {
            if (Time.time > avisoMeta)
            {
                avisoMeta = Time.time + 1.5f;
                Flota(jugador.transform.position + Vector3.up * 1.4f, "¡Primero derrota al monstruo!", new Color(1f, 0.6f, 0.5f));
            }
            return;
        }
        puntos += vidas * BONUS_POR_VIDA;
        bonusTiempo = 0;
        if (nivel != null && nivel.tiempo > 0)
        {
            bonusTiempo = Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante)) * PUNTOS_POR_SEGUNDO;
            puntos += bonusTiempo;
        }
        Progreso.Completar(nivelActual, vidas, puntos);
        estado = Estado.FinNivel;
        Time.timeScale = 0f;
    }

    void SiguienteNivel()
    {
        nivelActual++;
        if (nivelActual >= Niveles.Todos.Length)
        {
            estado = Estado.Victoria;
            Time.timeScale = 0f;
            return;
        }
        CargarNivel(nivelActual);
    }

    void ReintentarNivel()
    {
        puntos = puntosInicioNivel;
        CargarNivel(nivelActual);
    }

    public void Flota(Vector3 p, string t, Color c)
    {
        Flotante f = new Flotante();
        f.pos = p; f.txt = t; f.color = c;
        flot.Add(f);
    }

    // ---------------------------------------------------------------- Update / camara
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Escape) || ControlTactil.PausaDown)
        {
            if (estado == Estado.Jugando) { estado = Estado.Pausa; Time.timeScale = 0f; }
            else if (estado == Estado.Pausa) { estado = Estado.Jugando; Time.timeScale = 1f; }
        }
        if (estado == Estado.Pregunta)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Responder(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Responder(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Responder(2);
        }
        if (estado == Estado.Retroalimentacion && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z)))
            ContinuarTrasPregunta();
        if (estado == Estado.Reto)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ResponderReto(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) ResponderReto(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) ResponderReto(2);
        }
        if (estado == Estado.RetoResultado && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z)))
            EmpezarAJugar();

        if (estado == Estado.Jugando && jugador != null)
        {
            // reloj: empieza cuando Froxi se mueve por primera vez
            if (!relojCorre && jugador.seMovio) relojCorre = true;
            if (relojCorre && nivel != null && nivel.tiempo > 0)
            {
                tiempoRestante -= Time.deltaTime;
                if (tiempoRestante <= 0f) AgotarTiempo();
            }
            // poderes activos
            jugador.multSalto = Time.time < finSalto ? FUERZA_SUPER_SALTO : 1f;
            jugador.multVel = Time.time < finVelocidad ? FUERZA_VELOCIDAD : 1f;
            if (auraObj != null && auraObj.activeSelf != escudo) auraObj.SetActive(escudo);
            // disparar (Z, Ctrl izquierdo o K)
            if (tieneArma && (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.K)))
                Disparar();
        }

        for (int i = flot.Count - 1; i >= 0; i--)
        {
            flot[i].t += Time.deltaTime;
            flot[i].pos += Vector3.up * 1.2f * Time.deltaTime;
            if (flot[i].t > 1f) flot.RemoveAt(i);
        }
    }

    void LateUpdate()
    {
        if (raiz != null && jugador != null) PosicionarCamara(false);
    }

    void PosicionarCamara(bool brusco)
    {
        if (jugador == null) return;
        float mitad = cam.orthographicSize * cam.aspect;
        float tx = Mathf.Clamp(jugador.transform.position.x, mitad - 0.5f, W - 0.5f - mitad);
        if (W < mitad * 2f) tx = (W - 1) / 2f;
        float x = cam.transform.position.x;
        x = brusco ? tx : Mathf.Lerp(x, tx, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        cam.transform.position = new Vector3(x, (H - 1) / 2f, -10f);
        if (fondoT != null)
            fondoT.position = new Vector3(cx0 + (x - cx0) * 0.6f, (H - 1) / 2f, 0f);
    }

    // ================================================================ INTERFAZ (OnGUI)
    GUIStyle sTitulo, sEtiqueta, sParrafo, sBoton, sOpcion, sCentro, sPeque, sBloque;
    float esc, vw;
    const float VH = 720f;

    void Estilos()
    {
        if (sEtiqueta != null) return;
        sEtiqueta = new GUIStyle(GUI.skin.label); sEtiqueta.fontSize = 28; sEtiqueta.fontStyle = FontStyle.Bold; sEtiqueta.alignment = TextAnchor.MiddleLeft;
        sCentro = new GUIStyle(GUI.skin.label); sCentro.fontSize = 30; sCentro.fontStyle = FontStyle.Bold; sCentro.alignment = TextAnchor.MiddleCenter; sCentro.wordWrap = true;
        sPeque = new GUIStyle(GUI.skin.label); sPeque.fontSize = 22; sPeque.fontStyle = FontStyle.Bold; sPeque.alignment = TextAnchor.MiddleCenter;
        sTitulo = new GUIStyle(GUI.skin.label); sTitulo.fontSize = 130; sTitulo.fontStyle = FontStyle.Bold; sTitulo.alignment = TextAnchor.MiddleCenter;
        sParrafo = new GUIStyle(GUI.skin.label); sParrafo.fontSize = 30; sParrafo.fontStyle = FontStyle.Bold; sParrafo.alignment = TextAnchor.MiddleCenter; sParrafo.wordWrap = true;
        sBloque = new GUIStyle(GUI.skin.label); sBloque.fontStyle = FontStyle.Bold; sBloque.alignment = TextAnchor.UpperCenter; sBloque.wordWrap = true; sBloque.fontSize = 30;
        sBoton = new GUIStyle(GUI.skin.button); sBoton.fontSize = 30; sBoton.fontStyle = FontStyle.Bold;
        sOpcion = new GUIStyle(GUI.skin.button); sOpcion.fontSize = 26; sOpcion.fontStyle = FontStyle.Bold; sOpcion.wordWrap = true; sOpcion.alignment = TextAnchor.MiddleLeft; sOpcion.padding = new RectOffset(24, 12, 6, 6);
    }

    void Texto(Rect r, string t, GUIStyle s, Color c)
    {
        Color viejo = s.normal.textColor;
        s.normal.textColor = new Color(0f, 0f, 0f, 0.75f * c.a);
        Rect o = r; o.x += 2f; o.y += 2f;
        GUI.Label(o, t, s);
        s.normal.textColor = c;
        GUI.Label(r, t, s);
        s.normal.textColor = viejo;
    }

    // Escribe un parrafo desde ARRIBA del rectangulo, achicando la letra hasta que quepa.
    // Devuelve la coordenada y donde termina el texto (para apilar lo siguiente sin que se encime).
    float Bloque(Rect r, string t, Color c, int fMax, int fMin)
    {
        int f = fMax;
        GUIContent gc = new GUIContent(t);
        for (; f > fMin; f -= 2)
        {
            sBloque.fontSize = f;
            if (sBloque.CalcHeight(gc, r.width) <= r.height) break;
        }
        sBloque.fontSize = f;
        float h = Mathf.Min(r.height, sBloque.CalcHeight(gc, r.width));
        Texto(new Rect(r.x, r.y, r.width, h + 4f), t, sBloque, c);
        return r.y + h;
    }

    void TextoContorno(Rect r, string t, GUIStyle s, Color c, Color borde)
    {
        Color viejo = s.normal.textColor;
        s.normal.textColor = borde;
        for (int dx = -4; dx <= 4; dx += 4)
            for (int dy = -4; dy <= 4; dy += 4)
                if (dx != 0 || dy != 0) { Rect o = r; o.x += dx; o.y += dy; GUI.Label(o, t, s); }
        s.normal.textColor = c;
        GUI.Label(r, t, s);
        s.normal.textColor = viejo;
    }

    void Panel(Rect r, Color c)
    {
        Color v = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = v;
    }

    void DibujarSprite(Rect r, Sprite s)
    {
        if (s == null) return;
        Texture2D t = s.texture; Rect sr = s.rect;
        GUI.DrawTextureWithTexCoords(r, t, new Rect(sr.x / t.width, sr.y / t.height, sr.width / t.width, sr.height / t.height));
    }

    bool Boton(Rect r, string t, Color tinte)
    {
        Color v = GUI.backgroundColor; GUI.backgroundColor = tinte;
        bool b = GUI.Button(r, t, sBoton);
        GUI.backgroundColor = v;
        return b;
    }

    static readonly Color Verde = new Color(0.45f, 0.85f, 0.4f);
    static readonly Color Morado = new Color(0.7f, 0.5f, 0.95f);
    static readonly Color Rosa = new Color(1f, 0.55f, 0.7f);

    void OnGUI()
    {
        Estilos();
        esc = Screen.height / VH;
        vw = Screen.width / esc;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(esc, esc, 1f));

        switch (estado)
        {
            case Estado.Menu: UIMenu(); break;
            case Estado.Jugando: HUD(); UIIntro(); UIFlotantes(); break;
            case Estado.Pregunta: HUD(); UIPregunta(); break;
            case Estado.Reto: HUD(); UIReto(); break;
            case Estado.RetoResultado: HUD(); UIRetoResultado(); break;
            case Estado.Retroalimentacion: HUD(); UIRetro(); break;
            case Estado.Pausa: HUD(); UIPausa(); break;
            case Estado.FinNivel: HUD(); UIFinNivel(); break;
            case Estado.GameOver: HUD(); UIGameOver(); break;
            case Estado.TiempoAgotado: HUD(); UITiempoAgotado(); break;
            case Estado.Victoria: UIVictoria(); break;
        }
        GUI.matrix = Matrix4x4.identity;
    }

    void UIMenu()
    {
        Sprite bg = S("fondo_calle");
        if (bg != null)
        {
            float alto = bg.texture.height, ancho = bg.texture.width;
            float uvw = Mathf.Min(1f, (vw / (VH / alto)) / ancho);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, vw, VH), bg.texture, new Rect(0.05f, 0f, uvw, 1f));
        }
        Panel(new Rect(0, 0, vw, VH), new Color(1f, 1f, 1f, 0.12f));
        TextoContorno(new Rect(0, 20, vw, 150), "FROXI", sTitulo, new Color(0.5f, 0.95f, 0.35f), new Color(0.1f, 0.3f, 0.15f));
        Texto(new Rect(0, 160, vw, 40), "Aventura de un pequeño héroe", sCentro, Color.white);

        DibujarSprite(new Rect(vw * 0.5f - 420f, 270f, 288f, 288f), S("froxi_frente_0"));

        float bx = vw * 0.5f - 60f;
        if (Boton(new Rect(bx, 250, 380, 68), "¡Jugar!", Verde)) NuevaPartida(0);
        for (int i = 0; i < Niveles.Todos.Length; i++)
            if (Boton(new Rect(bx, 334 + i * 66, 380, 56), "Nivel " + (i + 1) + ": " + Niveles.Todos[i].nombre, Morado)) NuevaPartida(i);
        if (Boton(new Rect(bx, 334 + Niveles.Todos.Length * 66 + 6, 380, 56), "Salir", Rosa)) Application.Quit();

        Panel(new Rect(0, VH - 70, vw, 70), new Color(0f, 0f, 0f, 0.45f));
        Texto(new Rect(0, VH - 70, vw, 70), "Flechas o A/D: mover   •   X, W o Espacio: saltar   •   C: pausa   •   Toca la comida chatarra y contesta bien para no perder vida", sParrafo, Color.white);
        sParrafo.fontSize = 30;
    }

    void HUD()
    {
        for (int i = 0; i < VIDAS_INICIALES; i++)
            DibujarSprite(new Rect(18 + i * 52, 14, 48, 48), S(i < vidas ? "ui_corazon" : "ui_corazon_vacio"));
        DibujarSprite(new Rect(vw - 280, 14, 48, 48), S("ui_estrella"));
        Texto(new Rect(vw - 226, 14, 220, 48), puntos + " pts", sEtiqueta, Color.white);
        Texto(new Rect(18, 66, 400, 40), "Saludables: " + saludables + "/" + totalSaludables, sEtiqueta, new Color(0.7f, 1f, 0.6f));
        Texto(new Rect(0, 14, vw, 48), "Nivel " + (nivelActual + 1) + ": " + (nivel != null ? nivel.nombre : ""), sCentro, Color.white);

        // reloj
        if (nivel != null && nivel.tiempo > 0)
        {
            int s = Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante));
            Color cr = Color.white;
            if (relojCorre && s <= 10) cr = Mathf.Sin(Time.unscaledTime * 8f) > 0f ? new Color(1f, 0.35f, 0.35f) : Color.white;
            Texto(new Rect(vw - 280, 64, 270, 44), "Tiempo " + (s / 60) + ":" + (s % 60).ToString("00"), sEtiqueta, cr);
        }

        // lanzafrutas
        if (tieneArma)
        {
            DibujarSprite(new Rect(18, 190, 44, 44), S("saludable_manzana_0"));
            Texto(new Rect(70, 192, 360, 40), "Z / Ctrl: disparar", sEtiqueta, new Color(0.7f, 1f, 0.6f));
        }

        // vida del monstruo
        if (jefe != null && jefe.activo && !jefe.muerto)
        {
            Rect b = new Rect(vw / 2f - 260f, 68f, 520f, 26f);
            Panel(new Rect(b.x - 3, b.y - 3, b.width + 6, b.height + 6), new Color(0.1f, 0.05f, 0.08f, 0.9f));
            Panel(new Rect(b.x, b.y, b.width * Mathf.Clamp01(jefe.vida / (float)jefe.vidaMax), b.height), jefe.Furioso ? new Color(1f, 0.3f, 0.2f) : new Color(0.95f, 0.6f, 0.2f));
            Texto(new Rect(b.x, b.y - 2, b.width, b.height + 4), "Monstruo Chatarra", sPeque, Color.white);
        }

        // poderes activos
        float px = 18f;
        if (escudo) { DibujarSprite(new Rect(px, 112, 44, 44), S("poder_escudo_0")); Texto(new Rect(px - 4, 156, 52, 26), "Escudo", sPeque, Color.white); px += 64f; }
        if (Time.time < finSalto) { DibujarSprite(new Rect(px, 112, 44, 44), S("poder_salto_0")); Texto(new Rect(px - 4, 156, 52, 26), Mathf.CeilToInt(finSalto - Time.time) + " s", sPeque, Color.white); px += 64f; }
        if (Time.time < finVelocidad) { DibujarSprite(new Rect(px, 112, 44, 44), S("poder_velocidad_0")); Texto(new Rect(px - 4, 156, 52, 26), Mathf.CeilToInt(finVelocidad - Time.time) + " s", sPeque, Color.white); px += 64f; }
    }

    void UIIntro()
    {
        float falta = introHasta - Time.unscaledTime;
        if (falta <= 0f || nivel == null) return;
        float a = Mathf.Clamp01(falta);
        string desc = nivel.descripcion;
        if (nivel.tiempo > 0) desc += "\nTienes " + nivel.tiempo + " segundos. El reloj empieza cuando te muevas.";
        if (tieneArma) desc += "\n¡Tienes el LANZAFRUTAS! Pulsa Z (o Ctrl) para disparar.";
        Rect p = new Rect(vw * 0.5f - 460, 200, 920, 300);
        Panel(p, new Color(0f, 0f, 0f, 0.6f * a));
        Texto(new Rect(p.x + 20, p.y + 8, p.width - 40, 60), "Nivel " + (nivelActual + 1) + ": " + nivel.nombre, sCentro, new Color(1f, 1f, 1f, a));
        Bloque(new Rect(p.x + 30, p.y + 76, p.width - 60, p.height - 90), desc, new Color(1f, 1f, 0.7f, a), 30, 20);
    }

    void UIFlotantes()
    {
        for (int i = 0; i < flot.Count; i++)
        {
            Vector3 sp = cam.WorldToScreenPoint(flot[i].pos);
            Color c = flot[i].color; c.a = 1f - flot[i].t;
            Texto(new Rect(sp.x / esc - 70f, (Screen.height - sp.y) / esc - 25f, 140f, 50f), flot[i].txt, sCentro, c);
        }
    }

    void UIPregunta()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.6f));
        Rect p = new Rect((vw - 900f) / 2f, 40f, 900f, 640f);
        Panel(p, new Color(0.12f, 0.18f, 0.28f, 0.97f));
        string cab = "¡Comida chatarra! Contesta bien para no perder una vida";
        Color ccab = new Color(1f, 0.7f, 0.3f);
        if (preguntaActual.tipo == "Trampa") { cab = "¡PREGUNTA TRAMPA! Lee con mucho cuidado"; ccab = new Color(1f, 0.45f, 0.45f); }
        else if (preguntaActual.tipo == "Acertijo") { cab = "¡ACERTIJO! Piensa bien la respuesta"; ccab = new Color(0.65f, 0.85f, 1f); }
        Texto(new Rect(p.x, p.y + 14, p.width, 50), cab, sCentro, ccab);
        float y = Bloque(new Rect(p.x + 40, p.y + 80, p.width - 80, 190), preguntaActual.texto, Color.white, 30, 20);
        DibujarOpciones(p, p.y + 290f, false);
        Texto(new Rect(p.x, p.y + p.height - 50, p.width, 40), "Tip: también puedes pulsar 1, 2 o 3", sPeque, new Color(0.8f, 0.85f, 1f));
    }

    // las 3 opciones de respuesta (letra mas chica si el texto es largo)
    void DibujarOpciones(Rect p, float y0, bool reto)
    {
        for (int i = 0; i < 3; i++)
        {
            string txt = (char)('A' + i) + ")  " + Opcion(i);
            sOpcion.fontSize = txt.Length > 80 ? 20 : (txt.Length > 55 ? 22 : 26);
            if (GUI.Button(new Rect(p.x + 40, y0 + i * 88, p.width - 80, 80), txt, sOpcion))
            {
                if (reto) ResponderReto(i); else Responder(i);
            }
        }
        sOpcion.fontSize = 26;
    }

    // Reto del lanzafrutas (al empezar el nivel)
    void UIReto()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.65f));
        Rect p = new Rect((vw - 900f) / 2f, 20f, 900f, 680f);
        Panel(p, new Color(0.25f, 0.14f, 0.32f, 0.98f));
        Texto(new Rect(p.x, p.y + 10, p.width, 46), "¡RETO DEL LANZAFRUTAS!", sCentro, new Color(1f, 0.85f, 0.3f));
        Bloque(new Rect(p.x + 40, p.y + 58, p.width - 80, 64), "Si contestas BIEN esta pregunta tan difícil, ganas el lanzafrutas y podrás disparar fruta a la comida chatarra (tecla Z). Si fallas, el nivel empieza normal.", new Color(0.85f, 0.95f, 1f), 22, 16);
        Bloque(new Rect(p.x + 40, p.y + 132, p.width - 80, 170), preguntaActual.texto, Color.white, 30, 20);
        DibujarOpciones(p, p.y + 318f, true);
        if (GUI.Button(new Rect(p.x + 250, p.y + 602, 400, 56), "No, empezar sin arma", sOpcion)) { tieneArma = false; EmpezarAJugar(); }
    }

    void UIRetoResultado()
    {
        string cab = retoAcertado ? "¡Correcto!  +" + PUNTOS_RETO + " puntos" : "Incorrecto... el nivel empieza normal";
        string l2 = retoAcertado ? "¡Ganaste el LANZAFRUTAS! Pulsa Z (o Ctrl) para disparar." : "Respuesta correcta: " + preguntaActual.correcta;
        MostrarRetro(cab, retoAcertado ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.7f, 0.5f), l2, "¡A jugar!", EmpezarAJugar);
    }

    void UIRetro()
    {
        string cab, l2;
        Color cc;
        if (ultimaCorrecta) { cab = "¡Correcto!  +" + PUNTOS_RESPUESTA_CORRECTA + " puntos"; cc = new Color(0.5f, 1f, 0.5f); l2 = "Conservas tu vida."; }
        else
        {
            if (escudoSalvo) { cab = "Incorrecto... ¡pero tu escudo te salvó!"; cc = new Color(0.6f, 0.85f, 1f); }
            else { cab = "Incorrecto... pierdes una vida"; cc = new Color(1f, 0.5f, 0.5f); }
            l2 = "Respuesta correcta: " + preguntaActual.correcta;
        }
        MostrarRetro(cab, cc, l2, "Continuar", ContinuarTrasPregunta);
    }

    // Pantalla de respuesta: cada cosa va una debajo de la otra (nada se encima)
    void MostrarRetro(string cab, Color ccab, string linea2, string textoBoton, System.Action alContinuar)
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.6f));
        Rect p = new Rect((vw - 900f) / 2f, 40f, 900f, 640f);
        Panel(p, new Color(0.12f, 0.18f, 0.28f, 0.97f));
        float btnY = p.yMax - 96f;

        float y = p.y + 18f;
        y = Bloque(new Rect(p.x + 30, y, p.width - 60, 56), cab, ccab, 34, 22) + 16f;
        y = Bloque(new Rect(p.x + 40, y, p.width - 80, 140), linea2, Color.white, 32, 20) + 14f;
        Panel(new Rect(p.x + 60, y, p.width - 120, 3), new Color(1f, 1f, 1f, 0.25f));
        y += 16f;
        Bloque(new Rect(p.x + 40, y, p.width - 80, btnY - y - 12f), preguntaActual.explicacion, new Color(1f, 1f, 0.75f), 30, 18);
        if (Boton(new Rect(p.x + 260, btnY, 380, 70), textoBoton, Verde)) alContinuar();
    }

    void UIPausa()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.55f));
        Texto(new Rect(0, 140, vw, 70), "Pausa", sCentro, Color.white);
        float x = vw / 2f - 190f;
        if (Boton(new Rect(x, 240, 380, 70), "Continuar", Verde)) { estado = Estado.Jugando; Time.timeScale = 1f; }
        if (Boton(new Rect(x, 330, 380, 70), "Reiniciar nivel", Morado)) ReintentarNivel();
        if (Boton(new Rect(x, 420, 380, 70), "Menú principal", Rosa)) IrAlMenu();
    }

    void UIFinNivel()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.6f));
        Rect p = new Rect((vw - 820f) / 2f, 130f, 820f, 460f);
        Panel(p, new Color(0.12f, 0.28f, 0.2f, 0.97f));
        Texto(new Rect(p.x, p.y + 20, p.width, 70), "¡Nivel completado!", sCentro, new Color(0.6f, 1f, 0.6f));
        Texto(new Rect(p.x + 30, p.y + 100, p.width - 60, 50), "Alimentos saludables: " + saludables + " de " + totalSaludables, sParrafo, Color.white);
        Texto(new Rect(p.x + 30, p.y + 150, p.width - 60, 50), "Vidas restantes: " + vidas + "  (+" + (vidas * BONUS_POR_VIDA) + " pts)", sParrafo, Color.white);
        if (nivel != null && nivel.tiempo > 0)
            Texto(new Rect(p.x + 30, p.y + 200, p.width - 60, 50), "Tiempo que sobró: " + Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante)) + " s  (+" + bonusTiempo + " pts)", sParrafo, Color.white);
        Texto(new Rect(p.x + 30, p.y + 255, p.width - 60, 60), "Puntos totales: " + puntos, sCentro, new Color(1f, 0.92f, 0.3f));
        bool ultimo = nivelActual + 1 >= Niveles.Todos.Length;
        if (Boton(new Rect(p.x + 30, p.y + 340, 360, 74), ultimo ? "Ver resultado final" : "Siguiente nivel", Verde)) SiguienteNivel();
        if (Flujo.Activo && Boton(new Rect(p.x + 430, p.y + 340, 360, 74), "Volver al mundo", Morado)) Flujo.IrA(Pantalla.Mundo);
    }

    void UITiempoAgotado()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.65f));
        Rect p = new Rect((vw - 820f) / 2f, 130f, 820f, 460f);
        Panel(p, new Color(0.3f, 0.2f, 0.1f, 0.97f));
        Texto(new Rect(p.x, p.y + 20, p.width, 70), "¡Se acabó el tiempo!", sCentro, new Color(1f, 0.75f, 0.4f));
        Texto(new Rect(p.x + 30, p.y + 100, p.width - 60, 50), "No llegaste a la meta a tiempo. El nivel termina aquí.", sParrafo, Color.white);
        Texto(new Rect(p.x + 30, p.y + 170, p.width - 60, 50), "Alimentos saludables: " + saludables + " de " + totalSaludables, sParrafo, Color.white);
        Texto(new Rect(p.x + 30, p.y + 230, p.width - 60, 60), "Puntos: " + puntos, sCentro, new Color(1f, 0.92f, 0.3f));
        if (Boton(new Rect(p.x + 30, p.y + 340, 360, 74), "Reintentar nivel", Verde)) ReintentarNivel();
        if (Flujo.Activo) { if (Boton(new Rect(p.x + 430, p.y + 340, 360, 74), "Volver al mundo", Morado)) Flujo.IrA(Pantalla.Mundo); }
        else if (Boton(new Rect(p.x + 430, p.y + 340, 360, 74), "Menú principal", Rosa)) IrAlMenu();
    }

    void UIGameOver()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.65f));
        Texto(new Rect(0, 150, vw, 80), "¡Te quedaste sin vidas!", sCentro, new Color(1f, 0.55f, 0.55f));
        Texto(new Rect(0, 230, vw, 60), "Cuidado con la comida chatarra. ¡Tú puedes!", sParrafo, Color.white);
        float x = vw / 2f - 190f;
        if (Boton(new Rect(x, 330, 380, 70), "Reintentar nivel", Verde)) ReintentarNivel();
        if (Flujo.Activo) { if (Boton(new Rect(x, 420, 380, 70), "Volver al mundo", Rosa)) Flujo.IrA(Pantalla.Mundo); }
        else if (Boton(new Rect(x, 420, 380, 70), "Menú principal", Rosa)) IrAlMenu();
    }

    void UIVictoria()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0.1f, 0.25f, 0.2f, 1f));
        TextoContorno(new Rect(0, 60, vw, 150), "¡Ganaste!", sTitulo, new Color(1f, 0.92f, 0.3f), new Color(0.3f, 0.2f, 0f));
        DibujarSprite(new Rect(vw / 2f - 128f, 230f, 256f, 256f), S("froxi_frente_0"));
        Texto(new Rect(0, 500, vw, 50), "Froxi llegó a salvo y comió sano. Puntos totales: " + puntos, sCentro, Color.white);
        if (Boton(new Rect(vw / 2f - 190f, 580, 380, 70), "Menú principal", Verde)) IrAlMenu();
    }
}
