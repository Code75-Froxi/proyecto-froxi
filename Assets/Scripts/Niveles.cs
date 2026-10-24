using System.Collections.Generic;
using System.Reflection;

// ============================================================================
//  NIVELES
//  Cada nivel vive en su propio archivo:  Nivel_01_Calle.cs, Nivel_02_Escuela.cs ...
//  Este archivo los encuentra solos y los ordena por el numero del nombre.
//  Para agregar un nivel solo crea un archivo nuevo (no hay que tocar este).
// ============================================================================

public class NivelDatos
{
    public string nombre, descripcion, fondo, top, cuerpo, plataformaL, plataformaM, plataformaR, bloque, meta;
    public string[] mapa;
    public bool cueva;      // true = es el nivel al que lleva la cueva del mundo
    public int tiempo;      // segundos para terminar el nivel (0 = sin limite)
    public int dificultad;  // 2 = preguntas faciles y medias, 3 = medias y dificiles (0 = todas)
    public bool reto = false;       // true = al empezar sale una pregunta muy dificil para ganar el lanzafrutas (desactivado)
    public bool armaGratis;         // true = Froxi ya empieza con el lanzafrutas (nivel del monstruo)
}

public static class Niveles
{
    static NivelDatos[] _todos;

    public static NivelDatos[] Todos
    {
        get
        {
            if (_todos == null) _todos = Construir();
            return _todos;
        }
    }

    static NivelDatos[] Construir()
    {
        List<KeyValuePair<string, NivelDatos>> lista = new List<KeyValuePair<string, NivelDatos>>();
        foreach (System.Type t in typeof(Niveles).Assembly.GetTypes())
        {
            if (!t.Name.StartsWith("Nivel_")) continue;
            MethodInfo m = t.GetMethod("Datos", BindingFlags.Public | BindingFlags.Static);
            if (m == null || m.ReturnType != typeof(NivelDatos)) continue;
            NivelDatos d = (NivelDatos)m.Invoke(null, null);
            if (d != null) lista.Add(new KeyValuePair<string, NivelDatos>(t.Name, d));
        }
        lista.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        NivelDatos[] r = new NivelDatos[lista.Count];
        for (int i = 0; i < r.Length; i++) r[i] = lista[i].Value;
        return r;
    }
}
