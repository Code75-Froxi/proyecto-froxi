using UnityEngine;

// ============================================================================
//  PROGRESO: que niveles estan abiertos y cuantas estrellas lleva cada uno.
//  Se guarda solo en la computadora (PlayerPrefs). Estrellas = vidas que te sobraron (1 a 3).
// ============================================================================
public static class Progreso
{
    const string CLAVE = "froxi_desbloqueados";

    // cuantos niveles estan abiertos (el 1 siempre)
    public static int Desbloqueados { get { return Mathf.Max(1, PlayerPrefs.GetInt(CLAVE, 1)); } }

    public static int Estrellas(int nivel) { return PlayerPrefs.GetInt("froxi_estrellas_" + nivel, 0); }
    public static int MejorPuntaje(int nivel) { return PlayerPrefs.GetInt("froxi_puntos_" + nivel, 0); }

    // Cada nivel se abre cuando terminas el anterior:
    //   Nivel 1 (escuela) siempre abierto -> terminarlo abre la CUEVA (nivel 2).
    // (la tecla U en el selector de niveles abre todos para hacer pruebas)
    const string CLAVE_TODO = "froxi_todo_abierto";
    public static bool Completado(int nivel) { return PlayerPrefs.GetInt(CLAVE_TODO, 0) == 1 || Estrellas(nivel) > 0; }
    public static bool Abierto(int nivel) { return nivel <= 0 || Completado(nivel - 1); }
    public static bool CuevaAbierta { get { return Abierto(NivelCueva()); } }

    // la llama FroxiGame al terminar un nivel
    public static void Completar(int nivel, int vidasRestantes, int puntos)
    {
        int est = Mathf.Clamp(vidasRestantes, 1, 3);
        if (est > Estrellas(nivel)) PlayerPrefs.SetInt("froxi_estrellas_" + nivel, est);
        if (puntos > MejorPuntaje(nivel)) PlayerPrefs.SetInt("froxi_puntos_" + nivel, puntos);
        if (nivel + 1 > Desbloqueados) PlayerPrefs.SetInt(CLAVE, nivel + 1);
        PlayerPrefs.Save();
    }

    // a que nivel te lleva la cueva del mundo: el nivel marcado con  cueva = true  (si no hay, el ultimo abierto)
    public static int NivelCueva()
    {
        for (int i = 0; i < Niveles.Todos.Length; i++)
            if (Niveles.Todos[i].cueva) return i;
        return SiguienteNivel();
    }

    // a que nivel te lleva la puerta de la casita: el primer nivel que no es de la cueva
    public static int NivelCasa()
    {
        for (int i = 0; i < Niveles.Todos.Length; i++)
            if (!Niveles.Todos[i].cueva) return i;
        return 0;
    }

    // el ultimo nivel normal que tienes abierto
    public static int SiguienteNivel()
    {
        return Mathf.Clamp(Desbloqueados - 1, 0, Niveles.Todos.Length - 1);
    }

    public static void DesbloquearTodo()
    {
        PlayerPrefs.SetInt(CLAVE, Niveles.Todos.Length);
        PlayerPrefs.SetInt(CLAVE_TODO, 1);
        PlayerPrefs.Save();
    }

    public static void Borrar()
    {
        PlayerPrefs.SetInt(CLAVE, 1);
        PlayerPrefs.SetInt(CLAVE_TODO, 0);
        for (int i = 0; i < 50; i++)
        {
            PlayerPrefs.DeleteKey("froxi_estrellas_" + i);
            PlayerPrefs.DeleteKey("froxi_puntos_" + i);
        }
        PlayerPrefs.Save();
    }
}
