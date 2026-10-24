#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Menu:  Froxi > Limpiar niveles viejos
// Ahora el juego tiene 2 niveles: Nivel_01_Escuela y Nivel_02_Cueva.
// Este menu borra los archivos de los niveles anteriores (calle, escuela vieja, barrio y cueva vieja)
// para que no aparezcan en el menu de niveles. Los que no existan se ignoran.
public static class LimpiarViejos
{
    static readonly string[] VIEJOS =
    {
        "Assets/Scripts/Nivel_01_Calle.cs",
        "Assets/Scripts/Nivel_02_Escuela.cs",
        "Assets/Scripts/Nivel_03_Barrio.cs",
        "Assets/Scripts/Nivel_04_Cueva.cs"
    };

    [MenuItem("Froxi/Limpiar niveles viejos")]
    public static void Limpiar()
    {
        System.Collections.Generic.List<string> hay = new System.Collections.Generic.List<string>();
        foreach (string r in VIEJOS)
            if (AssetDatabase.LoadAssetAtPath<Object>(r) != null) hay.Add(r);

        if (hay.Count == 0)
        {
            EditorUtility.DisplayDialog("Todo limpio", "No hay niveles viejos. Solo quedan Nivel_01_Escuela y Nivel_02_Cueva.", "Ok");
            return;
        }
        if (!EditorUtility.DisplayDialog("Limpiar niveles viejos",
            "Se van a borrar estos archivos:\n\n- " + string.Join("\n- ", hay.ToArray()) +
            "\n\nLos niveles que quedan son Nivel_01_Escuela y Nivel_02_Cueva.\n\n¿Continuar?", "Si, borrar", "Cancelar")) return;

        foreach (string r in hay) AssetDatabase.DeleteAsset(r);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Listo", "Archivos borrados. Unity va a compilar unos segundos.", "Ok");
    }
}
#endif
