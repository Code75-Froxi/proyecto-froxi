#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// ============================================================================
//  Menus de Froxi para los niveles de plataformas:
//   Froxi > Nivel > Construir nivel en esta escena
//        dibuja el nivel (el del archivo Nivel_XX.cs) con Tilemaps en la escena:
//        lo ves en Scene y lo editas con la Tile Palette.
//   Froxi > Nivel > Guardar nivel en el codigo
//        lee lo que dibujaste y lo escribe de vuelta en el archivo Nivel_XX.cs
//        (guarda una copia del archivo anterior con la extension .respaldo~).
//  Cada pieza del dibujo es una letra del mapa:
//     suelo_top / suelo_cuerpo = #     plataforma = =     bloque = B     froxi = P
//     fruta = f     chatarra_camina = j     chatarra_quieta = x     meta = G
//     poder_escudo = e   poder_salto = s   poder_velocidad = v   poder_tiempo = t   poder_vida = h
// ============================================================================
public static class ConstructorNivel
{
    const string CARPETA_TILES = "Assets/Tiles";
    const string NOMBRE_RAIZ = "NivelEditable";

    static readonly Dictionary<string, char> LETRAS = new Dictionary<string, char>
    {
        { "suelo_top", '#' }, { "suelo_cuerpo", '#' }, { "plataforma", '=' }, { "bloque", 'B' },
        { "froxi", 'P' }, { "fruta", 'f' }, { "chatarra_camina", 'j' }, { "chatarra_quieta", 'x' }, { "meta", 'G' },
        { "poder_escudo", 'e' }, { "poder_salto", 's' }, { "poder_velocidad", 'v' }, { "poder_tiempo", 't' }, { "poder_vida", 'h' },
        { "trampa_pinchos", '^' }, { "trampa_sierra", 'o' }, { "trampa_hacha", 'a' }, { "trampa_maza", 'm' }, { "trampa_tabla", 'p' }, { "trampa_tronco", 'l' },
        { "jefe", 'J' }
    };

    // ------------------------------------------------------------ utilidades
    static List<System.Type> Clases()
    {
        List<System.Type> l = new List<System.Type>();
        foreach (System.Type t in typeof(Niveles).Assembly.GetTypes())
        {
            if (!t.Name.StartsWith("Nivel_")) continue;
            MethodInfo m = t.GetMethod("Datos", BindingFlags.Public | BindingFlags.Static);
            if (m == null || m.ReturnType != typeof(NivelDatos)) continue;
            l.Add(t);
        }
        l.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return l;
    }

    static NivelDatos Datos(System.Type clase)
    {
        MethodInfo m = clase.GetMethod("Datos", BindingFlags.Public | BindingFlags.Static);
        return (NivelDatos)m.Invoke(null, null);
    }

    static void CrearCarpeta(string ruta)
    {
        string[] partes = ruta.Split('/');
        string actual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string sig = actual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(sig)) AssetDatabase.CreateFolder(actual, partes[i]);
            actual = sig;
        }
    }

    static Tile ObtenerTile(string nombreTile, string nombreSprite, string carpeta, List<string> faltan)
    {
        Sprite sp = Resources.Load<Sprite>("Sprites/" + nombreSprite);
        if (sp == null) { faltan.Add(nombreSprite); return null; }
        string ruta = carpeta + "/" + nombreTile + ".asset";
        Tile t = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sp;
            t.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(t, ruta);
        }
        else
        {
            t.sprite = sp;
            t.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(t);
        }
        return t;
    }

    // ------------------------------------------------------------ construir
    [MenuItem("Froxi/Nivel/Construir nivel en esta escena")]
    public static void Construir()
    {
        List<System.Type> clases = Clases();
        if (clases.Count == 0)
        {
            EditorUtility.DisplayDialog("No hay niveles", "No encuentro archivos Nivel_01_..., Nivel_02_... en Assets/Scripts.", "Ok");
            return;
        }

        int idx = 0;
        EscenaFroxi ef = Object.FindObjectOfType<EscenaFroxi>();
        if (ef != null && ef.pantalla == Pantalla.Plataforma) idx = ef.nivel;
        else if (!EditorUtility.DisplayDialog("Que nivel",
            "Esta escena no tiene un objeto EscenaFroxi con Pantalla = Plataforma, asi que se dibujara el Nivel 1.\n\n(Abre la escena Nivel_01, Nivel_02... para dibujar cada uno.)\n\n¿Continuar?", "Si", "Cancelar")) return;
        idx = Mathf.Clamp(idx, 0, clases.Count - 1);

        System.Type clase = clases[idx];
        NivelDatos d = Datos(clase);
        string[] mapa = d.mapa;
        int H = mapa.Length, W = mapa[0].Length;

        GameObject viejo = GameObject.Find(NOMBRE_RAIZ);
        if (viejo != null)
        {
            if (!EditorUtility.DisplayDialog("Ya hay un nivel dibujado", "Ya existe un objeto '" + NOMBRE_RAIZ + "' en esta escena. Se borrara y se volvera a dibujar desde el archivo " + clase.Name + ".cs (pierdes lo que hayas pintado y no hayas guardado).\n\n¿Continuar?", "Si", "Cancelar")) return;
            Undo.DestroyObjectImmediate(viejo);
        }

        // las piezas (Tile assets) de este nivel
        string carpeta = CARPETA_TILES + "/" + clase.Name;
        CrearCarpeta(carpeta);
        List<string> faltan = new List<string>();
        Tile tTop = ObtenerTile("suelo_top", d.top, carpeta, faltan);
        Tile tCuerpo = ObtenerTile("suelo_cuerpo", d.cuerpo, carpeta, faltan);
        Tile tPlat = ObtenerTile("plataforma", d.plataformaM, carpeta, faltan);
        Tile tBloque = ObtenerTile("bloque", d.bloque, carpeta, faltan);
        Tile tFroxi = ObtenerTile("froxi", "froxi_der_1", carpeta, faltan);
        Tile tFruta = ObtenerTile("fruta", "saludable_manzana_0", carpeta, faltan);
        Tile tCam = ObtenerTile("chatarra_camina", "chatarra_hamburguesa_0", carpeta, faltan);
        Tile tQuieta = ObtenerTile("chatarra_quieta", "chatarra_refresco_0", carpeta, faltan);
        Tile tMeta = ObtenerTile("meta", d.meta, carpeta, faltan);
        Tile tEscudo = ObtenerTile("poder_escudo", "poder_escudo_0", carpeta, faltan);
        Tile tSalto = ObtenerTile("poder_salto", "poder_salto_0", carpeta, faltan);
        Tile tVel = ObtenerTile("poder_velocidad", "poder_velocidad_0", carpeta, faltan);
        Tile tTiempo = ObtenerTile("poder_tiempo", "poder_tiempo_0", carpeta, faltan);
        Tile tVida = ObtenerTile("poder_vida", "poder_vida_0", carpeta, faltan);
        Tile tPinchos = ObtenerTile("trampa_pinchos", "trampa_pinchos", carpeta, faltan);
        Tile tSierra = ObtenerTile("trampa_sierra", "trampa_sierra_0", carpeta, faltan);
        Tile tHacha = ObtenerTile("trampa_hacha", "trampa_hacha", carpeta, faltan);
        Tile tMaza = ObtenerTile("trampa_maza", "trampa_maza", carpeta, faltan);
        Tile tTabla = ObtenerTile("trampa_tabla", "trampa_tabla", carpeta, faltan);
        Tile tTronco = ObtenerTile("trampa_tronco", "trampa_tronco", carpeta, faltan);
        Tile tJefe = ObtenerTile("jefe", "jefe_0", carpeta, faltan);
        AssetDatabase.SaveAssets();

        // el objeto del nivel. Se mueve media casilla para que las casillas queden centradas en la rejilla del juego
        GameObject root = new GameObject(NOMBRE_RAIZ);
        Undo.RegisterCreatedObjectUndo(root, "Construir nivel de Froxi");
        root.transform.position = new Vector3(-0.5f, -0.5f, 0f);
        root.AddComponent<Grid>();
        NivelEditable ne = root.AddComponent<NivelEditable>();

        // fondo
        Sprite fs = Resources.Load<Sprite>("Sprites/" + d.fondo);
        if (fs != null)
        {
            GameObject fondo = new GameObject("Fondo");
            fondo.transform.SetParent(root.transform, false);
            fondo.transform.position = new Vector3(W / 2f, (H - 1) / 2f, 0f);
            SpriteRenderer fsr = fondo.AddComponent<SpriteRenderer>();
            fsr.sprite = fs;
            fsr.sortingOrder = -10;
            fsr.drawMode = SpriteDrawMode.Tiled;
            fsr.size = new Vector2(W * 2f + 60f, fs.bounds.size.y);
        }
        else faltan.Add(d.fondo);

        // el dibujo (una sola Tilemap)
        GameObject dib = new GameObject("Dibujo");
        dib.transform.SetParent(root.transform, false);
        Tilemap tm = dib.AddComponent<Tilemap>();
        TilemapRenderer tr = dib.GetComponent<TilemapRenderer>();
        if (tr == null) tr = dib.AddComponent<TilemapRenderer>();
        tr.sortingOrder = 0;

        List<Vector3Int> pos = new List<Vector3Int>();
        List<TileBase> tiles = new List<TileBase>();
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
            {
                char ch = mapa[r][c];
                Tile t = null;
                if (ch == '#')
                {
                    bool arriba = r > 0 && mapa[r - 1][c] == '#';
                    t = arriba ? tCuerpo : tTop;
                }
                else if (ch == '=') t = tPlat;
                else if (ch == 'B') t = tBloque;
                else if (ch == 'P') t = tFroxi;
                else if (ch == 'f') t = tFruta;
                else if (ch == 'j') t = tCam;
                else if (ch == 'x') t = tQuieta;
                else if (ch == 'G') t = tMeta;
                else if (ch == 'e') t = tEscudo;
                else if (ch == 's') t = tSalto;
                else if (ch == 'v') t = tVel;
                else if (ch == 't') t = tTiempo;
                else if (ch == 'h') t = tVida;
                else if (ch == '^') t = tPinchos;
                else if (ch == 'o') t = tSierra;
                else if (ch == 'a') t = tHacha;
                else if (ch == 'm') t = tMaza;
                else if (ch == 'p') t = tTabla;
                else if (ch == 'l') t = tTronco;
                else if (ch == 'J') t = tJefe;
                if (t == null) continue;
                pos.Add(new Vector3Int(c, H - 1 - r, 0));
                tiles.Add(t);
            }
        tm.SetTiles(pos.ToArray(), tiles.ToArray());

        ne.nivel = idx;
        ne.dibujo = tm;
        ne.ancho = W;
        ne.alto = H;

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();

        string aviso = "";
        if (faltan.Count > 0) aviso = "\n\nNo encontre estas imagenes en Assets/Resources/Sprites (esas piezas quedan sin dibujo):\n- " + string.Join("\n- ", faltan.ToArray());
        EditorUtility.DisplayDialog("Nivel dibujado",
            "Listo: '" + clase.Name + "' esta dibujado en la escena (objeto '" + NOMBRE_RAIZ + "').\n\n" +
            "Para editarlo: Window > 2D > Tile Palette, crea una paleta y arrastra a ella las piezas de " + carpeta + ".\n" +
            "Selecciona 'Dibujo' en la Hierarchy y pinta.\n\n" +
            "Cuando termines: Froxi > Nivel > Guardar nivel en el codigo." + aviso, "Ok");
    }

    // ------------------------------------------------------------ guardar
    static string Esc(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }

    [MenuItem("Froxi/Nivel/Guardar nivel en el codigo")]
    public static void Guardar()
    {
        NivelEditable ne = Object.FindObjectOfType<NivelEditable>();
        if (ne == null || ne.dibujo == null)
        {
            EditorUtility.DisplayDialog("No hay nivel dibujado", "Primero usa  Froxi > Nivel > Construir nivel en esta escena.", "Ok");
            return;
        }
        List<System.Type> clases = Clases();
        if (ne.nivel < 0 || ne.nivel >= clases.Count)
        {
            EditorUtility.DisplayDialog("Nivel no encontrado", "El nivel " + (ne.nivel + 1) + " ya no existe en Assets/Scripts.", "Ok");
            return;
        }
        System.Type clase = clases[ne.nivel];
        NivelDatos d = Datos(clase);

        string ruta = null;
        foreach (string g in AssetDatabase.FindAssets(clase.Name + " t:MonoScript"))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p) == clase.Name) { ruta = p; break; }
        }
        if (ruta == null)
        {
            EditorUtility.DisplayDialog("No encuentro el archivo", "No encuentro " + clase.Name + ".cs en Assets.", "Ok");
            return;
        }

        Tilemap tm = ne.dibujo;
        tm.CompressBounds();
        BoundsInt b = tm.cellBounds;
        int W = Mathf.Max(ne.ancho, b.xMax), H = Mathf.Max(ne.alto, b.yMax);
        char[][] mapa = new char[H][];
        for (int r = 0; r < H; r++) { mapa[r] = new char[W]; for (int c = 0; c < W; c++) mapa[r][c] = '.'; }

        int fuera = 0, desconocidas = 0, nFroxi = 0, nMeta = 0;
        for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
            {
                TileBase tb = tm.GetTile(new Vector3Int(x, y, 0));
                if (tb == null) continue;
                if (x < 0 || y < 0) { fuera++; continue; }
                char ch;
                if (!LETRAS.TryGetValue(tb.name, out ch)) { desconocidas++; continue; }
                mapa[H - 1 - y][x] = ch;
                if (ch == 'P') nFroxi++;
                if (ch == 'G') nMeta++;
            }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// Guardado desde Unity con  Froxi > Nivel > Guardar nivel en el codigo");
        sb.AppendLine("// Letras:  . vacio   # suelo   = plataforma (se atraviesa desde abajo)   B bloque solido");
        sb.AppendLine("//          P Froxi   f fruta o verdura   j chatarra que camina   x chatarra quieta   G meta");
        sb.AppendLine("// Poderes: e escudo   s super salto   v velocidad   t tiempo extra   h vida");
        sb.AppendLine("// Trampas: ^ pinchos   o sierra   a hacha   m maza   p tabla de puas   l tronco con puas      Jefe: J monstruo");
        sb.AppendLine("public static class " + clase.Name);
        sb.AppendLine("{");
        sb.AppendLine("    public static NivelDatos Datos()");
        sb.AppendLine("    {");
        sb.AppendLine("        return new NivelDatos");
        sb.AppendLine("        {");
        sb.AppendLine("            nombre = \"" + Esc(d.nombre) + "\", descripcion = \"" + Esc(d.descripcion) + "\",");
        sb.AppendLine("            fondo = \"" + Esc(d.fondo) + "\",");
        sb.AppendLine("            top = \"" + Esc(d.top) + "\",");
        sb.AppendLine("            cuerpo = \"" + Esc(d.cuerpo) + "\",");
        sb.AppendLine("            plataformaL = \"" + Esc(d.plataformaL) + "\",");
        sb.AppendLine("            plataformaM = \"" + Esc(d.plataformaM) + "\",");
        sb.AppendLine("            plataformaR = \"" + Esc(d.plataformaR) + "\",");
        sb.AppendLine("            bloque = \"" + Esc(d.bloque) + "\",");
        sb.AppendLine("            meta = \"" + Esc(d.meta) + "\",");
        if (d.cueva) sb.AppendLine("            cueva = true,");
        if (d.tiempo > 0) sb.AppendLine("            tiempo = " + d.tiempo + ",");
        if (d.dificultad > 0) sb.AppendLine("            dificultad = " + d.dificultad + ",");
        if (d.reto) sb.AppendLine("            reto = true,");
        if (d.armaGratis) sb.AppendLine("            armaGratis = true,");
        sb.AppendLine("            mapa = new string[]");
        sb.AppendLine("            {");
        for (int r = 0; r < H; r++) sb.AppendLine("                \"" + new string(mapa[r]) + "\",");
        sb.AppendLine("            }");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        File.Copy(ruta, ruta + ".respaldo~", true);
        File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(ruta);

        string aviso = "";
        if (nFroxi != 1) aviso += "\n- Hay " + nFroxi + " Froxi (debe haber 1, la pieza 'froxi').";
        if (nMeta == 0) aviso += "\n- No hay ninguna meta (pieza 'meta'): el nivel no se podra terminar.";
        if (fuera > 0) aviso += "\n- " + fuera + " piezas estan en posiciones negativas (a la izquierda o debajo del suelo): no se guardaron.";
        if (desconocidas > 0) aviso += "\n- " + desconocidas + " piezas no son de las del nivel y se ignoraron.";
        EditorUtility.DisplayDialog("Nivel guardado",
            "Se guardo en " + ruta + " (" + W + " x " + H + " casillas).\nLa copia anterior quedo como " + Path.GetFileName(ruta) + ".respaldo~ (Unity la ignora).\n\nUnity va a compilar unos segundos; despues dale Play para probarlo." +
            (aviso.Length > 0 ? "\n\nAvisos:" + aviso : ""), "Ok");
    }
}
#endif
