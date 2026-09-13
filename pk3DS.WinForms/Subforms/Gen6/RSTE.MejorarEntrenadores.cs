using System;
using System.IO;
using System.Windows.Forms;
using pk3DS.Core.Structures;

namespace pk3DS.WinForms;

public partial class RSTE
{
    private void B_MejorarEntrenadores_Click(object sender, EventArgs e)
    {
        MejorarTodosLosEntrenadores();
    }

    private void MejorarTodosLosEntrenadores()
    {
        if (!ConfirmarMejora())
            return;

        if (trdata.Length != trpoke.Length)
        {
            MessageBox.Show(this, "Los datos y los equipos de entrenadores no tienen la misma cantidad de entradas.",
                "No se aplicaron los cambios", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Guardamos primero lo que el usuario haya editado en el entrenador abierto.
        if (index > 0)
            WriteFile();

        // Preparamos todas las entradas antes de reemplazar las originales.
        byte[][] nuevosDatos = (byte[][])trdata.Clone();
        byte[][] nuevosEquipos = (byte[][])trpoke.Clone();

        try
        {
            // El índice 0 es una entrada de relleno; los entrenadores empiezan en el 1.
            for (int i = 1; i < trdata.Length; i++)
            {
                // Leemos los datos del entrenador y los de sus Pokémon para aplicar las reglas.
                TrainerData6 entrenador = new(trdata[i], trpoke[i], Main.Config.ORAS);
                bool teniaObjetosEquipados = entrenador.Item;
                ActivarObjetosEquipados(entrenador);
                foreach (TrainerData6.Pokemon pokemon in entrenador.Team)
                {
                    MejorarIV(pokemon);
                    IntentarEquiparBayaZidra(pokemon);
                }

                AsignarRestaurarTodoSiProcede(entrenador);
                nuevosDatos[i] = CrearDatosEntrenadorMejorados(trdata[i], entrenador, Main.Config.ORAS);
                nuevosEquipos[i] = CrearEquipoMejorado(trpoke[i], entrenador, teniaObjetosEquipados);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se aplicaron las mejoras: " + ex.Message,
                "Error al mejorar entrenadores", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Al terminar el recorrido, copiamos todos los resultados a los archivos abiertos.
        Array.Copy(nuevosDatos, trdata, trdata.Length);
        Array.Copy(nuevosEquipos, trpoke, trpoke.Length);

        // Mostramos los valores nuevos y evitamos que el cierre sobrescriba el entrenador visible.
        if (index > 0)
            ReadFile();
    }

    private static byte[] CrearDatosEntrenadorMejorados(byte[] original, TrainerData6 entrenador, bool oras)
    {
        // Copiamos el archivo para conservar campos que esta función no edita.
        byte[] resultado = (byte[])original.Clone();
        resultado[0] |= 0x02; // Activa el indicador de objetos equipados.

        // El primer objeto empieza en el byte 8 de ORAS o en el 4 de X/Y.
        int posicionObjeto = oras ? 8 : 4;
        ushort objeto = entrenador.Items[0];
        resultado[posicionObjeto] = (byte)objeto;
        resultado[posicionObjeto + 1] = (byte)(objeto >> 8);
        return resultado;
    }

    private static byte[] CrearEquipoMejorado(byte[] original, TrainerData6 entrenador, bool teniaObjetosEquipados)
    {
        int cantidad = entrenador.Team.Length;
        if (cantidad == 0 || original.Length % cantidad != 0)
            throw new InvalidDataException("El equipo de un entrenador tiene un tamaño inválido.");

        int longitudAnterior = original.Length / cantidad;
        if (longitudAnterior < (teniaObjetosEquipados ? 10 : 8))
            throw new InvalidDataException("Los datos de un Pokémon están incompletos.");

        int longitudNueva = longitudAnterior + (teniaObjetosEquipados ? 0 : 2);
        byte[] resultado = new byte[cantidad * longitudNueva];
        for (int i = 0; i < cantidad; i++)
        {
            int origen = i * longitudAnterior;
            int destino = i * longitudNueva;
            if (teniaObjetosEquipados)
            {
                // Si ya existía el campo de objeto, conservamos el registro completo.
                Buffer.BlockCopy(original, origen, resultado, destino, longitudAnterior);
            }
            else
            {
                // Añadimos dos bytes para el objeto después de los ocho bytes iniciales.
                Buffer.BlockCopy(original, origen, resultado, destino, 8);
                Buffer.BlockCopy(original, origen + 8, resultado, destino + 10, longitudAnterior - 8);
            }

            // Solo cambiamos el IV y el objeto equipado de este Pokémon.
            resultado[destino] = entrenador.Team[i].IVs;
            ushort objeto = entrenador.Team[i].Item;
            resultado[destino + 8] = (byte)objeto;
            resultado[destino + 9] = (byte)(objeto >> 8);
        }

        return resultado;
    }

    private bool ConfirmarMejora()
    {
        const string mensaje =
            "Este botón recorrerá todos los entrenadores y sus Pokémon.\r\n\r\n" +
            "• IV: si vale 0, pasará a 60; los demás se duplicarán (máximo 255).\r\n" +
            "• Pokémon sin objeto: 87 % de recibir una Baya Zidra.\r\n" +
            "  Los objetos equipados se conservarán.\r\n" +
            "• Se activará «Items» para guardar los objetos equipados.\r\n" +
            "• Si las cuatro ranuras de objetos del entrenador están vacías,\r\n" +
            "  se pondrá un Restaurar Todo en la primera.\r\n" +
            "  Los objetos existentes se conservarán.\r\n\r\n" +
            "Es posible que ya hayas aplicado estos cambios.\r\n" +
            "Si continúas, los IV volverán a cambiar y los Pokémon sin objeto\r\n" +
            "tendrán otra oportunidad de recibir una baya.\r\n\r\n" +
            "¿Quieres continuar?";

        DialogResult respuesta = MessageBox.Show(this, mensaje, "Mejorar entrenadores",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        return respuesta == DialogResult.Yes;
    }

    private static void MejorarIV(TrainerData6.Pokemon pokemon)
    {
        // El valor almacenado es un byte, así que nunca puede superar 255.
        if (pokemon.IVs == 0)
        {
            pokemon.IVs = 60;
            return;
        }

        int valorDuplicado = pokemon.IVs * 2;
        pokemon.IVs = (byte)Math.Min(valorDuplicado, byte.MaxValue);
    }

    private static void ActivarObjetosEquipados(TrainerData6 entrenador)
    {
        // Este indicador hace que cada Pokémon tenga un campo para su objeto equipado.
        entrenador.Item = true;
    }

    private static void IntentarEquiparBayaZidra(TrainerData6.Pokemon pokemon)
    {
        const ushort idBayaZidra = 158;
        const int probabilidad = 87;

        // Un objeto ya equipado se conserva, incluida una Baya Zidra anterior.
        if (pokemon.Item != 0)
            return;

        // Next(100) da un número de 0 a 99: 87 de esos 100 valores son menores que 87.
        if (Random.Shared.Next(100) < probabilidad)
            pokemon.Item = idBayaZidra;
    }

    private static void AsignarRestaurarTodoSiProcede(TrainerData6 entrenador)
    {
        const ushort idRestaurarTodo = 23;

        // Si alguna ranura ya tiene un objeto, respetamos los cuatro valores originales.
        foreach (ushort objeto in entrenador.Items)
        {
            if (objeto != 0)
                return;
        }

        // Las cuatro ranuras están vacías: añadimos Restaurar Todo en la primera.
        entrenador.Items[0] = idRestaurarTodo;
    }
}
