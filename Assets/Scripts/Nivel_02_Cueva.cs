// Nivel 2: LA CUEVA (60 segundos, 65 frutas). Un archivo por nivel: copia este archivo para hacer otro (Nivel_NN_Nombre).
// Letras:  . vacio   # suelo   = plataforma (se atraviesa desde abajo)   B bloque solido
//          P Froxi   f fruta o verdura   j chatarra que camina   x chatarra quieta   G meta
// Poderes: e escudo   s super salto   v velocidad   t tiempo extra   h vida
// Trampas: ^ pinchos   o sierra que va y viene   a hacha que se balancea   m maza que se balancea   p tabla de puas que cae   l tronco con puas
public static class Nivel_02_Cueva
{
    public static NivelDatos Datos()
    {
        return new NivelDatos
        {
            nombre = "La cueva", descripcion = "Entra a la cueva, salta los abismos, junta las 65 frutas y busca la salida con luz. Hay más chatarra y poderes escondidos.",
            fondo = "fondo_cueva",
            top = "l4_roca_top",
            cuerpo = "l4_roca_cuerpo",
            plataformaL = "l4_saliente_izq",
            plataformaM = "l4_saliente_med",
            plataformaR = "l4_saliente_der",
            bloque = "l4_roca",
            meta = "meta_cueva",
            cueva = true,
            tiempo = 60,
            dificultad = 3,
            mapa = new string[]
            {
                "..........................................................................................m...............a...............................................................",
                "..........................................................................................................................................................................",
                "..........................................................................................................................................................................",
                "......................................................................fff.t...............................................p............fff.h..............................",
                "......................................................................===..............................................................===................................",
                "............fff.........fff...f..........fff..fff...........fff..fff..........fff...f............v..........ff..fff...f...........fff...............fff...................",
                "............===...............B..........===.....................===................B..e....................==........B...........===..........sB..................t......",
                "...P..fffff.^..f.j.fff........B.f.jf.fff.^l.........xf.j.B..........o......j........BB...x..j.......fff..j............B..x.fff.j........^...j.l.B........j..x.fff..o..G^..",
                "########################...###################...###########...###############...###############...#############...###############...###############...###################",
                "########################...###################...###########...###############...###############...#############...###############...###############...###################",
            }
        };
    }
}
