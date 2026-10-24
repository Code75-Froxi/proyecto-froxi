// Nivel 1: LA ESCUELA (50 segundos). Un archivo por nivel: copia este archivo para hacer otro (Nivel_NN_Nombre).
// Letras:  . vacio   # suelo   = plataforma (se atraviesa desde abajo)   B bloque solido
//          P Froxi   f fruta o verdura   j chatarra que camina   x chatarra quieta   G meta
// Poderes: e escudo   s super salto   v velocidad   t tiempo extra   h vida
// Trampas: ^ pinchos   o sierra que va y viene   a hacha que se balancea   m maza que se balancea   p tabla de puas que cae   l tronco con puas
public static class Nivel_01_Escuela
{
    public static NivelDatos Datos()
    {
        return new NivelDatos
        {
            nombre = "La escuela", descripcion = "Cruza el patio de la escuela, junta fruta y verdura y esquiva la comida chatarra. ¡Cuidado con los huecos!",
            fondo = "fondo_escuela",
            top = "l2_pasto_top",
            cuerpo = "l2_ladrillo",
            plataformaL = "l2_banca_izq",
            plataformaM = "l2_banca_med",
            plataformaR = "l2_banca_der",
            bloque = "l2_casillero",
            meta = "meta_bandera",
            tiempo = 50,
            dificultad = 2,
            mapa = new string[]
            {
                "................................................................................a...................................................",
                "....................................................................................................................................",
                "....................................................................................................................................",
                ".........................................................................fff.t.............................................p........",
                ".........................................................................===........................................................",
                "............fff.............f...............fff.........fff.........fff.............fff...f..........s...........fff................",
                "............===.......f.....B.......e...................===.........===...................B...................h.........f...........",
                "...P...fff..^.....j...B.ff..BB.^.x..l.ff.........fff.j........x.ffj...o...x....v.j........BB..fffj.......fffj^........x.B..ff.j..G..",
                "############################################...#########...#########################...#############...##########...################",
                "############################################...#########...#########################...#############...##########...################",
            }
        };
    }
}
