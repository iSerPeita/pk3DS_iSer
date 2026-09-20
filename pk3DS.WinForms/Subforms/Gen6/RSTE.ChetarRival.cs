using System;
using System.IO;
using System.Windows.Forms;

namespace pk3DS.WinForms;

public partial class RSTE
{
    private const int UltimoEntrenadorRequerido = 908;
    private const int PosicionTipoCombateORAS = 6;
    private const int PosicionCantidadPokemonORAS = 7;
    private const int PosicionPrimerObjetoEntrenadorORAS = 8;
    private const int PosicionIAORAS = 16;
    private const byte IABaseMaxima = 0x07;
    private const byte IAMultiple = 0x80;
    private const byte IADobleMaxima = IABaseMaxima | IAMultiple;
    private const int LongitudPokemonRivalCompleto = 18;
    private const int PosicionEspeciePokemon = 4;
    private const int PosicionObjetoPokemon = 8;
    private const ushort IdBayaZidra = 158;
    private const ushort IdRaichu = 26;
    private const ushort IdRaikou = 243;
    private const ushort IdLugia = 249;
    private const ushort IdSwellow = 277;
    private const int CantidadObjetosEntrenador = 4;
    private const int ProbabilidadRetirarObjeto = 20;

    private static readonly int[] IdsRivalesDobles =
    [
        289, 290, 291, 292, 293, 294,
        295, 296, 297, 298, 299, 300,
        527, 528, 529, 530, 531, 532,
        699, 700, 701, 906, 907, 908,
    ];

    private static readonly int[] IdsRivalesConRhydon = [289, 290, 291, 295, 296, 297];

    private static readonly int[] IdsRivalesConRaikou =
    [
        527, 528, 529, 530, 531, 532,
        699, 700, 701, 906, 907, 908,
    ];

    private static readonly int[] IdsRivalesConLugia = [699, 700, 701, 906, 907, 908];

    // IV 255, habilidad 1, género aleatorio, nivel 24, Rhydon, forma 0,
    // Baya Zidra y movimientos Taladradora, Puño Trueno, Puño Hielo y Tumba Rocas.
    private static readonly byte[] DatosRhydonRival =
    [
        0xFF, 0x10, 0x18, 0x00, 0x70, 0x00, 0x00, 0x00, 0x9E,
        0x00, 0x11, 0x02, 0x09, 0x00, 0x08, 0x00, 0x3D, 0x01,
    ];

    private static readonly (int Id, ushort EspecieQuinto, ushort Megapiedra)[] RivalesMega =
    [
        (292, 254, 753), // Sceptile con Sceptilita
        (293, 257, 664), // Blaziken con Blazikenita
        (294, 260, 752), // Swampert con Swampertita
        (298, 254, 753), // Sceptile con Sceptilita
        (299, 257, 664), // Blaziken con Blazikenita
        (300, 260, 752), // Swampert con Swampertita
    ];

    // IV 255, habilidad aleatoria, género femenino, nivel 47, Alakazam, forma 0,
    // Cuchara Torcida y movimientos Psíquico, Ventisca, Brillo Mágico y Onda Certera.
    private static readonly byte[] DatosAlakazamRival =
    [
        0xFF, 0x02, 0x2F, 0x00, 0x41, 0x00, 0x00, 0x00, 0xF8,
        0x00, 0x5E, 0x00, 0x3B, 0x00, 0x5D, 0x02, 0x9B, 0x01,
    ];

    // El 298 de la base de trabajo contiene esta misma configuración con género aleatorio.
    private static readonly byte[] DatosAlakazamRivalAnterior =
    [
        0xFF, 0x00, 0x2F, 0x00, 0x41, 0x00, 0x00, 0x00, 0xF8,
        0x00, 0x5E, 0x00, 0x3B, 0x00, 0x5D, 0x02, 0x9B, 0x01,
    ];

    // IV 255, Compensación (habilidad oculta), género femenino, nivel 75, Lugia, forma 0,
    // Restos y movimientos Aerochorro, Psíquico, Paz Mental e Hidrobomba.
    private static readonly byte[] DatosLugiaRival =
    [
        0xFF, 0x32, 0x4B, 0x00, 0xF9, 0x00, 0x00, 0x00, 0xEA,
        0x00, 0xB1, 0x00, 0x5E, 0x00, 0x5B, 0x01, 0x38, 0x00,
    ];

    private void B_ChetarRival_Click(object sender, EventArgs e)
    {
        if (ConfirmarChetarRivales())
            ChetarRivales();
    }

    private bool ConfirmarChetarRivales()
    {
        const string mensaje =
            "Esta función aplicará todos los cambios acordados para los rivales de ORAS:\r\n\r\n" +
            "• IA máxima compatible con cada tipo de combate.\r\n" +
            "• Los 24 rivales seleccionados pasarán a combate doble con IA 135.\r\n" +
            "• Se añadirán Rhydon, megapiedras y Alakazam a los equipos indicados.\r\n" +
            "• Los Raichu y Swellow indicados se sustituirán por Raikou y Lugia.\r\n" +
            "• Un 20 % estable de los entrenadores con un único objeto perderá ese objeto.\r\n\r\n" +
            "Si también vas a usar «Mejorar entrenadores», úsalo antes que este botón;\r\n" +
            "si lo usas después, puede volver a colocar Restaurar Todo.\r\n\r\n" +
            "¿Quieres continuar?";

        DialogResult respuesta = MessageBox.Show(this, mensaje, "Chetar Rival",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        return respuesta == DialogResult.Yes;
    }

    /// <summary>
    /// Prepara y aplica de forma atómica las mejoras de rivales.
    /// El botón se conectará en una fase posterior.
    /// </summary>
    private void ChetarRivales()
    {
        var resultado = new ResultadoChetarRival();

        try
        {
            if (!Main.Config.ORAS)
                throw new InvalidOperationException("Chetar Rival solo está disponible para Rubí Omega y Zafiro Alfa.");

            // Conserva cualquier cambio que el usuario haya hecho en el entrenador visible.
            if (index > 0)
                WriteFile();

            ValidarArchivosEntrenadores(trdata, trpoke);

            byte[][] nuevosDatos = ClonarDatosEntrenadores(trdata);
            byte[][] nuevosEquipos = ClonarDatosEntrenadores(trpoke);

            AplicarCambiosChetarRival(nuevosDatos, nuevosEquipos, resultado);
            ValidarArchivosEntrenadores(nuevosDatos, nuevosEquipos);
            ValidarIAMaximizada(trdata, nuevosDatos);

            // Una ejecución sin transformaciones no toca los arrays originales ni la interfaz.
            if (DatosIguales(trdata, nuevosDatos) && DatosIguales(trpoke, nuevosEquipos))
            {
                MessageBox.Show(this, "Todos los cambios de Chetar Rival ya estaban aplicados.",
                    "Chetar Rival", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Array.Copy(nuevosDatos, trdata, trdata.Length);
            Array.Copy(nuevosEquipos, trpoke, trpoke.Length);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se aplicaron los cambios: " + ex.Message,
                "Error al chetar rivales", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Los datos ya están confirmados; un fallo visual no debe presentarse como fallo de la operación.
        if (index > 0)
        {
            try
            {
                ReadFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Los cambios se aplicaron, pero no se pudo actualizar la pantalla: " + ex.Message,
                    "Cambios aplicados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        string resumen =
            $"Cambios aplicados correctamente.\r\n\r\n" +
            $"IA actualizada: {resultado.IAActualizadas}\r\n" +
            $"Combates dobles configurados: {resultado.CombatesDobles}\r\n" +
            $"Rhydon añadidos: {resultado.RhydonAgregados}\r\n" +
            $"Megapiedras asignadas: {resultado.MegapiedrasAsignadas}\r\n" +
            $"Alakazam añadidos: {resultado.AlakazamAgregados}\r\n" +
            $"Raichu sustituidos por Raikou: {resultado.RaikouSustituidos}\r\n" +
            $"Swellow sustituidos por Lugia: {resultado.LugiaSustituidos}\r\n" +
            $"Objetos de entrenador retirados: {resultado.ObjetosRetirados}";
        MessageBox.Show(this, resumen, "Chetar Rival", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void AplicarCambiosChetarRival(byte[][] datos, byte[][] equipos, ResultadoChetarRival resultado)
    {
        MaximizarIAEntrenadores(datos, resultado);

        // Se conserva el estado inmediatamente anterior para aislar y validar esta transformación.
        byte[][] datosAntesDeDobles = ClonarDatosEntrenadores(datos);
        byte[][] equiposAntesDeDobles = ClonarDatosEntrenadores(equipos);

        ConvertirRivalesEnDobles(datos, resultado);
        ValidarRivalesDobles(datosAntesDeDobles, datos);

        if (!DatosIguales(equiposAntesDeDobles, equipos))
            throw new InvalidDataException("La conversión a combates dobles modificó inesperadamente los equipos.");

        byte[][] datosAntesDeRhydon = ClonarDatosEntrenadores(datos);
        byte[][] equiposAntesDeRhydon = ClonarDatosEntrenadores(equipos);

        AgregarRhydonAEntrenadores(datos, equipos, resultado);
        ValidarRhydonAgregados(datosAntesDeRhydon, equiposAntesDeRhydon, datos, equipos);

        byte[][] datosAntesDeMega = ClonarDatosEntrenadores(datos);
        byte[][] equiposAntesDeMega = ClonarDatosEntrenadores(equipos);

        ConfigurarMegaevolucionesYAlakazam(datos, equipos, resultado);
        ValidarMegaevolucionesYAlakazam(datosAntesDeMega, equiposAntesDeMega, datos, equipos);

        byte[][] datosAntesDeRaikou = ClonarDatosEntrenadores(datos);
        byte[][] equiposAntesDeRaikou = ClonarDatosEntrenadores(equipos);

        SustituirRaichuPorRaikou(datos, equipos, resultado);
        ValidarSustitucionesRaikou(datosAntesDeRaikou, equiposAntesDeRaikou, datos, equipos);

        byte[][] datosAntesDeLugia = ClonarDatosEntrenadores(datos);
        byte[][] equiposAntesDeLugia = ClonarDatosEntrenadores(equipos);

        SustituirSwellowPorLugia(datos, equipos, resultado);
        ValidarSustitucionesLugia(datosAntesDeLugia, equiposAntesDeLugia, datos, equipos);

        byte[][] datosAntesDeRetirarObjetos = ClonarDatosEntrenadores(datos);
        byte[][] equiposAntesDeRetirarObjetos = ClonarDatosEntrenadores(equipos);

        RetirarObjetoUnicoAlVeintePorCiento(datos, resultado);
        ValidarRetiradaObjetos(datosAntesDeRetirarObjetos, equiposAntesDeRetirarObjetos,
            datos, equipos, resultado.ObjetosRetirados);

        // Las demás transformaciones se incorporarán por separado en las siguientes fases.
    }

    private static void RetirarObjetoUnicoAlVeintePorCiento(
        byte[][] datos, ResultadoChetarRival resultado)
    {
        // La selección estable evita que varias pulsaciones retiren objetos adicionales.
        for (int id = 1; id < datos.Length; id++)
        {
            int posicionObjeto = ObtenerPosicionObjetoUnico(datos[id]);
            if (posicionObjeto < 0 || !SeleccionadoParaRetirarObjeto(id))
                continue;

            datos[id][posicionObjeto] = 0;
            datos[id][posicionObjeto + 1] = 0;
            resultado.ObjetosRetirados++;
        }
    }

    private static void ValidarRetiradaObjetos(
        byte[][] datosAnteriores, byte[][] equiposAnteriores, byte[][] datosModificados,
        byte[][] equiposModificados, int retiradosRegistrados)
    {
        if (datosAnteriores.Length != datosModificados.Length ||
            equiposAnteriores.Length != equiposModificados.Length ||
            datosModificados.Length != equiposModificados.Length)
        {
            throw new InvalidDataException("La retirada de objetos cambió la cantidad de entrenadores.");
        }

        int retiradosEsperados = 0;
        for (int id = 1; id < datosAnteriores.Length; id++)
        {
            if (!equiposAnteriores[id].AsSpan().SequenceEqual(equiposModificados[id]))
                throw new InvalidDataException($"La retirada de objetos modificó el equipo del entrenador {id}.");

            byte[] datosEsperados = (byte[])datosAnteriores[id].Clone();
            int posicionObjeto = ObtenerPosicionObjetoUnico(datosAnteriores[id]);
            if (posicionObjeto >= 0 && SeleccionadoParaRetirarObjeto(id))
            {
                datosEsperados[posicionObjeto] = 0;
                datosEsperados[posicionObjeto + 1] = 0;
                retiradosEsperados++;
            }

            if (!datosEsperados.AsSpan().SequenceEqual(datosModificados[id]))
            {
                throw new InvalidDataException(
                    $"La retirada del objeto único modificó bytes inesperados del entrenador {id}.");
            }
        }

        if (retiradosRegistrados != retiradosEsperados)
        {
            throw new InvalidDataException(
                $"Se registraron {retiradosRegistrados} objetos retirados, pero se esperaban {retiradosEsperados}.");
        }
    }

    private static int ObtenerPosicionObjetoUnico(byte[] datosEntrenador)
    {
        int posicionEncontrada = -1;
        for (int ranura = 0; ranura < CantidadObjetosEntrenador; ranura++)
        {
            int posicion = PosicionPrimerObjetoEntrenadorORAS + (ranura * sizeof(ushort));
            if (BitConverter.ToUInt16(datosEntrenador, posicion) == 0)
                continue;

            if (posicionEncontrada >= 0)
                return -1;

            posicionEncontrada = posicion;
        }

        return posicionEncontrada;
    }

    private static bool SeleccionadoParaRetirarObjeto(int id)
    {
        // Mezcla avalancha de 32 bits: distribuye los ID sin depender del orden ni del estado global del generador.
        unchecked
        {
            uint valor = (uint)id + 0x9E3779B9u;
            valor ^= valor >> 16;
            valor *= 0x85EBCA6Bu;
            valor ^= valor >> 13;
            valor *= 0xC2B2AE35u;
            valor ^= valor >> 16;
            return valor % 100 < ProbabilidadRetirarObjeto;
        }
    }

    private static void SustituirSwellowPorLugia(
        byte[][] datos, byte[][] equipos, ResultadoChetarRival resultado)
    {
        ValidarListaRivalesConLugia();

        foreach (int id in IdsRivalesConLugia)
        {
            if ((uint)id >= (uint)datos.Length || (uint)id >= (uint)equipos.Length)
                throw new InvalidDataException($"No existe el entrenador {id} requerido para sustituir a Swellow.");

            byte[] datosEntrenador = datos[id];
            byte[] equipoEntrenador = equipos[id];
            int cantidad = datosEntrenador[PosicionCantidadPokemonORAS];
            int formato = BitConverter.ToUInt16(datosEntrenador, 0);
            if (cantidad != 6 || formato != 3 ||
                equipoEntrenador.Length != cantidad * LongitudPokemonRivalCompleto)
            {
                throw new InvalidDataException(
                    $"El equipo del entrenador {id} no permite sustituir de forma segura su primer Pokémon.");
            }

            ushort especieActual = BitConverter.ToUInt16(equipoEntrenador, PosicionEspeciePokemon);
            if (especieActual == IdLugia)
            {
                if (!equipoEntrenador.AsSpan(0, LongitudPokemonRivalCompleto).SequenceEqual(DatosLugiaRival))
                {
                    throw new InvalidDataException(
                        $"El entrenador {id} ya tiene un Lugia, pero no coincide con la configuración acordada.");
                }

                continue;
            }

            if (especieActual != IdSwellow)
            {
                throw new InvalidDataException(
                    $"El primer Pokémon del entrenador {id} no es Swellow ni el Lugia ya aplicado ({especieActual}).");
            }

            Buffer.BlockCopy(DatosLugiaRival, 0, equipoEntrenador, 0, DatosLugiaRival.Length);
            resultado.LugiaSustituidos++;
        }
    }

    private static void ValidarSustitucionesLugia(
        byte[][] datosAnteriores, byte[][] equiposAnteriores, byte[][] datosModificados, byte[][] equiposModificados)
    {
        if (datosAnteriores.Length != datosModificados.Length ||
            equiposAnteriores.Length != equiposModificados.Length ||
            datosModificados.Length != equiposModificados.Length)
        {
            throw new InvalidDataException("La sustitución de Swellow cambió la cantidad de entrenadores.");
        }

        for (int id = 1; id < datosAnteriores.Length; id++)
        {
            bool esObjetivo = EsRivalConLugia(id);
            if (!datosAnteriores[id].AsSpan().SequenceEqual(datosModificados[id]))
                throw new InvalidDataException($"La sustitución de Swellow modificó los datos del entrenador {id}.");

            if (!esObjetivo)
            {
                if (!equiposAnteriores[id].AsSpan().SequenceEqual(equiposModificados[id]))
                {
                    throw new InvalidDataException(
                        $"La sustitución de Swellow modificó inesperadamente el equipo del entrenador {id}.");
                }

                continue;
            }

            byte[] equipoEsperado = (byte[])equiposAnteriores[id].Clone();
            Buffer.BlockCopy(DatosLugiaRival, 0, equipoEsperado, 0, DatosLugiaRival.Length);
            if (!equipoEsperado.AsSpan().SequenceEqual(equiposModificados[id]))
            {
                throw new InvalidDataException(
                    $"La sustitución de Swellow por Lugia modificó otros Pokémon del entrenador {id}.");
            }

            if (!equiposModificados[id].AsSpan(0, LongitudPokemonRivalCompleto).SequenceEqual(DatosLugiaRival))
                throw new InvalidDataException($"No se sustituyó correctamente a Swellow en el entrenador {id}.");
        }
    }

    private static void ValidarListaRivalesConLugia()
    {
        const int cantidadEsperada = 6;
        if (IdsRivalesConLugia.Length != cantidadEsperada)
            throw new InvalidDataException($"La lista de Lugia debe contener {cantidadEsperada} entrenadores.");

        for (int i = 0; i < IdsRivalesConLugia.Length; i++)
        {
            for (int j = i + 1; j < IdsRivalesConLugia.Length; j++)
            {
                if (IdsRivalesConLugia[i] == IdsRivalesConLugia[j])
                    throw new InvalidDataException($"El entrenador {IdsRivalesConLugia[i]} está repetido en la lista de Lugia.");
            }
        }
    }

    private static bool EsRivalConLugia(int id) => Array.IndexOf(IdsRivalesConLugia, id) >= 0;

    private static void SustituirRaichuPorRaikou(
        byte[][] datos, byte[][] equipos, ResultadoChetarRival resultado)
    {
        ValidarListaRivalesConRaikou();

        foreach (int id in IdsRivalesConRaikou)
        {
            if ((uint)id >= (uint)datos.Length || (uint)id >= (uint)equipos.Length)
                throw new InvalidDataException($"No existe el entrenador {id} requerido para sustituir a Raichu.");

            byte[] datosEntrenador = datos[id];
            byte[] equipoEntrenador = equipos[id];
            int cantidad = datosEntrenador[PosicionCantidadPokemonORAS];
            int formato = BitConverter.ToUInt16(datosEntrenador, 0);
            if (cantidad < 2 || formato != 3 ||
                equipoEntrenador.Length != cantidad * LongitudPokemonRivalCompleto)
            {
                throw new InvalidDataException(
                    $"El equipo del entrenador {id} no permite sustituir de forma segura su segundo Pokémon.");
            }

            int posicionEspecie = LongitudPokemonRivalCompleto + PosicionEspeciePokemon;
            ushort especieActual = BitConverter.ToUInt16(equipoEntrenador, posicionEspecie);
            if (especieActual != IdRaichu && especieActual != IdRaikou)
            {
                throw new InvalidDataException(
                    $"El segundo Pokémon del entrenador {id} no es Raichu ni el Raikou ya aplicado ({especieActual}).");
            }

            if (especieActual == IdRaikou)
                continue;

            equipoEntrenador[posicionEspecie] = (byte)IdRaikou;
            equipoEntrenador[posicionEspecie + 1] = (byte)(IdRaikou >> 8);
            resultado.RaikouSustituidos++;
        }
    }

    private static void ValidarSustitucionesRaikou(
        byte[][] datosAnteriores, byte[][] equiposAnteriores, byte[][] datosModificados, byte[][] equiposModificados)
    {
        if (datosAnteriores.Length != datosModificados.Length ||
            equiposAnteriores.Length != equiposModificados.Length ||
            datosModificados.Length != equiposModificados.Length)
        {
            throw new InvalidDataException("La sustitución de Raichu cambió la cantidad de entrenadores.");
        }

        int posicionEspecie = LongitudPokemonRivalCompleto + PosicionEspeciePokemon;
        for (int id = 1; id < datosAnteriores.Length; id++)
        {
            bool esObjetivo = EsRivalConRaikou(id);
            if (!datosAnteriores[id].AsSpan().SequenceEqual(datosModificados[id]))
                throw new InvalidDataException($"La sustitución de Raichu modificó los datos del entrenador {id}.");

            if (!esObjetivo)
            {
                if (!equiposAnteriores[id].AsSpan().SequenceEqual(equiposModificados[id]))
                {
                    throw new InvalidDataException(
                        $"La sustitución de Raichu modificó inesperadamente el equipo del entrenador {id}.");
                }

                continue;
            }

            byte[] equipoEsperado = (byte[])equiposAnteriores[id].Clone();
            equipoEsperado[posicionEspecie] = (byte)IdRaikou;
            equipoEsperado[posicionEspecie + 1] = (byte)(IdRaikou >> 8);
            if (!equipoEsperado.AsSpan().SequenceEqual(equiposModificados[id]))
            {
                throw new InvalidDataException(
                    $"La sustitución de Raichu por Raikou modificó otros valores del entrenador {id}.");
            }

            if (BitConverter.ToUInt16(equiposModificados[id], posicionEspecie) != IdRaikou)
                throw new InvalidDataException($"No se sustituyó correctamente a Raichu en el entrenador {id}.");
        }
    }

    private static void ValidarListaRivalesConRaikou()
    {
        const int cantidadEsperada = 12;
        if (IdsRivalesConRaikou.Length != cantidadEsperada)
            throw new InvalidDataException($"La lista de Raikou debe contener {cantidadEsperada} entrenadores.");

        for (int i = 0; i < IdsRivalesConRaikou.Length; i++)
        {
            for (int j = i + 1; j < IdsRivalesConRaikou.Length; j++)
            {
                if (IdsRivalesConRaikou[i] == IdsRivalesConRaikou[j])
                    throw new InvalidDataException($"El entrenador {IdsRivalesConRaikou[i]} está repetido en la lista de Raikou.");
            }
        }
    }

    private static bool EsRivalConRaikou(int id) => Array.IndexOf(IdsRivalesConRaikou, id) >= 0;

    private static void ConfigurarMegaevolucionesYAlakazam(
        byte[][] datos, byte[][] equipos, ResultadoChetarRival resultado)
    {
        ValidarListaRivalesMega();

        foreach ((int id, ushort especieQuinto, ushort megapiedra) in RivalesMega)
        {
            if ((uint)id >= (uint)datos.Length || (uint)id >= (uint)equipos.Length)
                throw new InvalidDataException($"No existe el entrenador {id} requerido para la configuración de megaevolución.");

            byte[] datosEntrenador = datos[id];
            byte[] equipoEntrenador = equipos[id];
            int cantidad = datosEntrenador[PosicionCantidadPokemonORAS];
            if (cantidad is not (5 or 6))
                throw new InvalidDataException($"El entrenador {id} tiene {cantidad} Pokémon; se esperaban cinco o seis.");

            int formato = BitConverter.ToUInt16(datosEntrenador, 0);
            if (formato != 3 || equipoEntrenador.Length != cantidad * LongitudPokemonRivalCompleto)
            {
                throw new InvalidDataException(
                    $"El equipo del entrenador {id} no tiene el formato completo de objetos y movimientos esperado.");
            }

            int inicioQuinto = 4 * LongitudPokemonRivalCompleto;
            ushort especieActual = BitConverter.ToUInt16(equipoEntrenador, inicioQuinto + PosicionEspeciePokemon);
            if (especieActual != especieQuinto)
            {
                throw new InvalidDataException(
                    $"El quinto Pokémon del entrenador {id} no es la especie esperada ({especieQuinto}).");
            }

            ushort objetoActual = BitConverter.ToUInt16(equipoEntrenador, inicioQuinto + PosicionObjetoPokemon);
            if (objetoActual != IdBayaZidra && objetoActual != megapiedra)
            {
                throw new InvalidDataException(
                    $"El quinto Pokémon del entrenador {id} tiene un objeto inesperado ({objetoActual}).");
            }

            if (cantidad == 6)
            {
                ReadOnlySpan<byte> sexto = equipoEntrenador.AsSpan(5 * LongitudPokemonRivalCompleto);
                bool alakazamReconocido = sexto.SequenceEqual(DatosAlakazamRival) ||
                    sexto.SequenceEqual(DatosAlakazamRivalAnterior);
                if (!alakazamReconocido)
                {
                    throw new InvalidDataException(
                        $"El entrenador {id} ya tiene seis Pokémon, pero el sexto no es el Alakazam acordado.");
                }
            }

            byte[] equipoAmpliado = new byte[6 * LongitudPokemonRivalCompleto];
            Buffer.BlockCopy(equipoEntrenador, 0, equipoAmpliado, 0, 5 * LongitudPokemonRivalCompleto);
            equipoAmpliado[inicioQuinto + PosicionObjetoPokemon] = (byte)megapiedra;
            equipoAmpliado[inicioQuinto + PosicionObjetoPokemon + 1] = (byte)(megapiedra >> 8);
            Buffer.BlockCopy(DatosAlakazamRival, 0, equipoAmpliado,
                5 * LongitudPokemonRivalCompleto, DatosAlakazamRival.Length);

            if (objetoActual != megapiedra)
                resultado.MegapiedrasAsignadas++;
            if (cantidad == 5)
                resultado.AlakazamAgregados++;

            datosEntrenador[PosicionCantidadPokemonORAS] = 6;
            equipos[id] = equipoAmpliado;
        }
    }

    private static void ValidarMegaevolucionesYAlakazam(
        byte[][] datosAnteriores, byte[][] equiposAnteriores, byte[][] datosModificados, byte[][] equiposModificados)
    {
        if (datosAnteriores.Length != datosModificados.Length ||
            equiposAnteriores.Length != equiposModificados.Length ||
            datosModificados.Length != equiposModificados.Length)
        {
            throw new InvalidDataException("La configuración de megaevoluciones cambió la cantidad de entrenadores.");
        }

        for (int id = 1; id < datosAnteriores.Length; id++)
        {
            bool esObjetivo = IntentarObtenerRivalMega(id, out _, out ushort megapiedra);
            if (!esObjetivo)
            {
                if (!datosAnteriores[id].AsSpan().SequenceEqual(datosModificados[id]) ||
                    !equiposAnteriores[id].AsSpan().SequenceEqual(equiposModificados[id]))
                {
                    throw new InvalidDataException(
                        $"La configuración de megaevoluciones modificó inesperadamente al entrenador {id}.");
                }

                continue;
            }

            byte[] datosEsperados = (byte[])datosAnteriores[id].Clone();
            datosEsperados[PosicionCantidadPokemonORAS] = 6;

            byte[] equipoEsperado = new byte[6 * LongitudPokemonRivalCompleto];
            Buffer.BlockCopy(equiposAnteriores[id], 0, equipoEsperado, 0, 5 * LongitudPokemonRivalCompleto);
            int posicionObjetoQuinto = 4 * LongitudPokemonRivalCompleto + PosicionObjetoPokemon;
            equipoEsperado[posicionObjetoQuinto] = (byte)megapiedra;
            equipoEsperado[posicionObjetoQuinto + 1] = (byte)(megapiedra >> 8);
            Buffer.BlockCopy(DatosAlakazamRival, 0, equipoEsperado,
                5 * LongitudPokemonRivalCompleto, DatosAlakazamRival.Length);

            if (!datosEsperados.AsSpan().SequenceEqual(datosModificados[id]) ||
                !equipoEsperado.AsSpan().SequenceEqual(equiposModificados[id]))
            {
                throw new InvalidDataException(
                    $"No se configuraron correctamente la megapiedra y Alakazam del entrenador {id}.");
            }

            byte[] equipoFinal = equiposModificados[id];
            ushort objetoFinal = BitConverter.ToUInt16(equipoFinal, posicionObjetoQuinto);
            bool resultadoCorrecto = datosModificados[id][PosicionCantidadPokemonORAS] == 6 &&
                equipoFinal.Length == 6 * LongitudPokemonRivalCompleto &&
                objetoFinal == megapiedra &&
                equipoFinal.AsSpan(5 * LongitudPokemonRivalCompleto).SequenceEqual(DatosAlakazamRival);
            if (!resultadoCorrecto)
                throw new InvalidDataException($"La validación final de megaevolución falló para el entrenador {id}.");
        }
    }

    private static void ValidarListaRivalesMega()
    {
        const int cantidadEsperada = 6;
        if (RivalesMega.Length != cantidadEsperada)
            throw new InvalidDataException($"La lista de megaevoluciones debe contener {cantidadEsperada} entrenadores.");

        for (int i = 0; i < RivalesMega.Length; i++)
        {
            (int id, ushort especieQuinto, ushort megapiedra) = RivalesMega[i];
            if (id <= 0 || especieQuinto == 0 || megapiedra == 0)
                throw new InvalidDataException("La lista de megaevoluciones contiene una configuración incompleta.");

            for (int j = i + 1; j < RivalesMega.Length; j++)
            {
                if (id == RivalesMega[j].Id)
                    throw new InvalidDataException($"El entrenador {id} está repetido en la lista de megaevoluciones.");
            }
        }
    }

    private static bool IntentarObtenerRivalMega(int id, out ushort especieQuinto, out ushort megapiedra)
    {
        foreach ((int idConfigurado, ushort especie, ushort piedra) in RivalesMega)
        {
            if (idConfigurado != id)
                continue;

            especieQuinto = especie;
            megapiedra = piedra;
            return true;
        }

        especieQuinto = 0;
        megapiedra = 0;
        return false;
    }

    private static void AgregarRhydonAEntrenadores(
        byte[][] datos, byte[][] equipos, ResultadoChetarRival resultado)
    {
        ValidarListaRivalesConRhydon();

        foreach (int id in IdsRivalesConRhydon)
        {
            if ((uint)id >= (uint)datos.Length || (uint)id >= (uint)equipos.Length)
                throw new InvalidDataException($"No existe el entrenador {id} requerido para agregar a Rhydon.");

            byte[] datosEntrenador = datos[id];
            byte[] equipoEntrenador = equipos[id];
            int cantidad = datosEntrenador[PosicionCantidadPokemonORAS];

            if (cantidad == 5)
            {
                bool longitudCorrecta = equipoEntrenador.Length == 5 * LongitudPokemonRivalCompleto;
                bool rhydonCorrecto = longitudCorrecta &&
                    equipoEntrenador.AsSpan(4 * LongitudPokemonRivalCompleto).SequenceEqual(DatosRhydonRival);
                if (!rhydonCorrecto)
                {
                    throw new InvalidDataException(
                        $"El entrenador {id} ya tiene cinco Pokémon, pero el quinto no es el Rhydon acordado.");
                }

                continue;
            }

            if (cantidad != 4)
                throw new InvalidDataException($"El entrenador {id} tiene {cantidad} Pokémon; se esperaban cuatro o cinco.");

            int formato = BitConverter.ToUInt16(datosEntrenador, 0);
            if (formato != 3 || equipoEntrenador.Length != 4 * LongitudPokemonRivalCompleto)
            {
                throw new InvalidDataException(
                    $"El equipo del entrenador {id} no tiene el formato completo de objetos y movimientos esperado.");
            }

            byte[] equipoAmpliado = new byte[5 * LongitudPokemonRivalCompleto];
            Buffer.BlockCopy(equipoEntrenador, 0, equipoAmpliado, 0, equipoEntrenador.Length);
            Buffer.BlockCopy(DatosRhydonRival, 0, equipoAmpliado, equipoEntrenador.Length, DatosRhydonRival.Length);

            datosEntrenador[PosicionCantidadPokemonORAS] = 5;
            equipos[id] = equipoAmpliado;
            resultado.RhydonAgregados++;
        }
    }

    private static void ValidarRhydonAgregados(
        byte[][] datosAnteriores, byte[][] equiposAnteriores, byte[][] datosModificados, byte[][] equiposModificados)
    {
        if (datosAnteriores.Length != datosModificados.Length ||
            equiposAnteriores.Length != equiposModificados.Length ||
            datosModificados.Length != equiposModificados.Length)
        {
            throw new InvalidDataException("La incorporación de Rhydon cambió la cantidad de entrenadores.");
        }

        for (int id = 1; id < datosAnteriores.Length; id++)
        {
            bool esObjetivo = EsRivalConRhydon(id);
            if (!esObjetivo)
            {
                if (!datosAnteriores[id].AsSpan().SequenceEqual(datosModificados[id]) ||
                    !equiposAnteriores[id].AsSpan().SequenceEqual(equiposModificados[id]))
                {
                    throw new InvalidDataException(
                        $"La incorporación de Rhydon modificó inesperadamente al entrenador {id}.");
                }

                continue;
            }

            byte[] datosEsperados = (byte[])datosAnteriores[id].Clone();
            byte[] equipoEsperado;
            int cantidadAnterior = datosAnteriores[id][PosicionCantidadPokemonORAS];
            if (cantidadAnterior == 4)
            {
                datosEsperados[PosicionCantidadPokemonORAS] = 5;
                equipoEsperado = new byte[5 * LongitudPokemonRivalCompleto];
                Buffer.BlockCopy(equiposAnteriores[id], 0, equipoEsperado, 0, equiposAnteriores[id].Length);
                Buffer.BlockCopy(DatosRhydonRival, 0, equipoEsperado,
                    equiposAnteriores[id].Length, DatosRhydonRival.Length);
            }
            else
            {
                equipoEsperado = equiposAnteriores[id];
            }

            if (!datosEsperados.AsSpan().SequenceEqual(datosModificados[id]) ||
                !equipoEsperado.AsSpan().SequenceEqual(equiposModificados[id]))
            {
                throw new InvalidDataException($"No se agregó correctamente a Rhydon al entrenador {id}.");
            }

            byte[] equipoFinal = equiposModificados[id];
            bool resultadoCorrecto = datosModificados[id][PosicionCantidadPokemonORAS] == 5 &&
                equipoFinal.Length == 5 * LongitudPokemonRivalCompleto &&
                equipoFinal.AsSpan(4 * LongitudPokemonRivalCompleto).SequenceEqual(DatosRhydonRival);
            if (!resultadoCorrecto)
                throw new InvalidDataException($"La validación final de Rhydon falló para el entrenador {id}.");
        }
    }

    private static void ValidarListaRivalesConRhydon()
    {
        const int cantidadEsperada = 6;
        if (IdsRivalesConRhydon.Length != cantidadEsperada)
            throw new InvalidDataException($"La lista de Rhydon debe contener {cantidadEsperada} entrenadores.");

        for (int i = 0; i < IdsRivalesConRhydon.Length; i++)
        {
            for (int j = i + 1; j < IdsRivalesConRhydon.Length; j++)
            {
                if (IdsRivalesConRhydon[i] == IdsRivalesConRhydon[j])
                    throw new InvalidDataException($"El entrenador {IdsRivalesConRhydon[i]} está repetido en la lista de Rhydon.");
            }
        }
    }

    private static bool EsRivalConRhydon(int id) => Array.IndexOf(IdsRivalesConRhydon, id) >= 0;

    private static void ConvertirRivalesEnDobles(byte[][] datos, ResultadoChetarRival resultado)
    {
        ValidarListaRivalesDobles();

        foreach (int id in IdsRivalesDobles)
        {
            if ((uint)id >= (uint)datos.Length)
                throw new InvalidDataException($"No existe el entrenador {id} requerido para los combates dobles.");

            byte[] entrenador = datos[id];
            byte tipoAnterior = entrenador[PosicionTipoCombateORAS];
            byte iaAnterior = entrenador[PosicionIAORAS];

            // Acepta tanto el estado original como el resultado de una ejecución anterior.
            bool estadoOriginal = tipoAnterior == 0 && iaAnterior == IABaseMaxima;
            bool estadoAplicado = tipoAnterior == 1 && iaAnterior == IADobleMaxima;
            if (!estadoOriginal && !estadoAplicado)
            {
                throw new InvalidDataException(
                    $"El entrenador {id} tiene un estado inesperado antes de convertirlo en doble " +
                    $"(BattleType={tipoAnterior}, AI={iaAnterior}).");
            }

            if (tipoAnterior != 1)
                resultado.CombatesDobles++;

            entrenador[PosicionTipoCombateORAS] = 1;
            entrenador[PosicionIAORAS] = IADobleMaxima;
        }
    }

    private static void ValidarRivalesDobles(byte[][] anteriores, byte[][] modificados)
    {
        if (anteriores.Length != modificados.Length)
            throw new InvalidDataException("La conversión a dobles cambió la cantidad de entrenadores.");

        for (int id = 1; id < anteriores.Length; id++)
        {
            bool esObjetivo = EsRivalDoble(id);
            byte[] anterior = anteriores[id];
            byte[] modificado = modificados[id];

            if (anterior.Length != modificado.Length)
                throw new InvalidDataException($"La conversión a dobles cambió el tamaño del entrenador {id}.");

            for (int posicion = 0; posicion < anterior.Length; posicion++)
            {
                if (anterior[posicion] == modificado[posicion])
                    continue;

                bool cambioPermitido = esObjetivo &&
                    posicion is PosicionTipoCombateORAS or PosicionIAORAS;
                if (!cambioPermitido)
                {
                    throw new InvalidDataException(
                        $"La conversión a dobles modificó inesperadamente el byte {posicion} del entrenador {id}.");
                }
            }

            if (!esObjetivo)
                continue;

            if (modificado[PosicionTipoCombateORAS] != 1 || modificado[PosicionIAORAS] != IADobleMaxima)
                throw new InvalidDataException($"No se convirtió correctamente en doble al entrenador {id}.");
        }
    }

    private static void ValidarListaRivalesDobles()
    {
        const int cantidadEsperada = 24;
        if (IdsRivalesDobles.Length != cantidadEsperada)
            throw new InvalidDataException($"La lista de combates dobles debe contener {cantidadEsperada} entrenadores.");

        for (int i = 0; i < IdsRivalesDobles.Length; i++)
        {
            for (int j = i + 1; j < IdsRivalesDobles.Length; j++)
            {
                if (IdsRivalesDobles[i] == IdsRivalesDobles[j])
                    throw new InvalidDataException($"El entrenador {IdsRivalesDobles[i]} está repetido en la lista de dobles.");
            }
        }
    }

    private static bool EsRivalDoble(int id) => Array.IndexOf(IdsRivalesDobles, id) >= 0;

    private static void MaximizarIAEntrenadores(byte[][] datos, ResultadoChetarRival resultado)
    {
        // La entrada 0 es de relleno; los entrenadores utilizables comienzan en 1.
        for (int id = 1; id < datos.Length; id++)
        {
            byte[] entrenador = datos[id];
            byte tipoCombate = entrenador[PosicionTipoCombateORAS];
            byte iaAnterior = entrenador[PosicionIAORAS];
            byte iaNueva = (byte)(iaAnterior | IABaseMaxima);

            if (tipoCombate is 1 or 2 or 4)
                iaNueva |= IAMultiple;

            if (iaNueva == iaAnterior)
                continue;

            entrenador[PosicionIAORAS] = iaNueva;
            resultado.IAActualizadas++;
        }
    }

    private static void ValidarIAMaximizada(byte[][] originales, byte[][] modificados)
    {
        if (originales.Length != modificados.Length)
            throw new InvalidDataException("No se puede validar la IA porque cambió la cantidad de entrenadores.");

        for (int id = 1; id < originales.Length; id++)
        {
            byte iaAnterior = originales[id][PosicionIAORAS];
            byte iaNueva = modificados[id][PosicionIAORAS];
            byte tipoCombate = modificados[id][PosicionTipoCombateORAS];

            if ((iaNueva & IABaseMaxima) != IABaseMaxima)
                throw new InvalidDataException($"No se maximizó correctamente la IA del entrenador {id}.");
            if (tipoCombate is 1 or 2 or 4 && (iaNueva & IAMultiple) == 0)
                throw new InvalidDataException($"El entrenador múltiple {id} no conserva la IA específica de su formato.");
            if ((iaNueva & iaAnterior) != iaAnterior)
                throw new InvalidDataException($"Al maximizar la IA del entrenador {id} se perdió un bit original.");
        }
    }

    private static byte[][] ClonarDatosEntrenadores(byte[][] origen)
    {
        byte[][] copia = new byte[origen.Length][];
        for (int i = 0; i < origen.Length; i++)
            copia[i] = (byte[])origen[i].Clone();
        return copia;
    }

    private static void ValidarArchivosEntrenadores(byte[][] datos, byte[][] equipos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ArgumentNullException.ThrowIfNull(equipos);

        if (datos.Length != equipos.Length)
            throw new InvalidDataException("Los datos y los equipos no tienen la misma cantidad de entradas.");
        if (datos.Length <= UltimoEntrenadorRequerido)
            throw new InvalidDataException($"Falta el entrenador {UltimoEntrenadorRequerido}; solo hay {datos.Length} entradas.");

        int longitudMinimaDatos = Main.Config.ORAS ? 24 : 20;
        int posicionCantidad = Main.Config.ORAS ? 7 : 3;

        // La entrada 0 es de relleno; los entrenadores utilizables comienzan en 1.
        for (int id = 1; id < datos.Length; id++)
        {
            byte[] datosEntrenador = datos[id]
                ?? throw new InvalidDataException($"Los datos del entrenador {id} son nulos.");
            byte[] equipoEntrenador = equipos[id]
                ?? throw new InvalidDataException($"El equipo del entrenador {id} es nulo.");

            if (datosEntrenador.Length < longitudMinimaDatos)
                throw new InvalidDataException($"Los datos del entrenador {id} están incompletos.");

            int cantidad = datosEntrenador[posicionCantidad];
            if (cantidad is < 1 or > 6)
                throw new InvalidDataException($"El entrenador {id} declara una cantidad inválida de Pokémon: {cantidad}.");

            int formato = Main.Config.ORAS
                ? BitConverter.ToUInt16(datosEntrenador, 0)
                : datosEntrenador[0];
            bool tieneObjetos = (formato & 2) != 0;
            bool tieneMovimientos = (formato & 1) != 0;
            int longitudPokemon = 8 + (tieneObjetos ? 2 : 0) + (tieneMovimientos ? 8 : 0);
            int longitudEsperada = cantidad * longitudPokemon;

            if (equipoEntrenador.Length != longitudEsperada)
            {
                throw new InvalidDataException(
                    $"El equipo del entrenador {id} ocupa {equipoEntrenador.Length} bytes; se esperaban {longitudEsperada}.");
            }
        }
    }

    private static bool DatosIguales(byte[][] primero, byte[][] segundo)
    {
        if (primero.Length != segundo.Length)
            return false;

        for (int i = 0; i < primero.Length; i++)
        {
            byte[] a = primero[i];
            byte[] b = segundo[i];
            if (ReferenceEquals(a, b) || a.AsSpan().SequenceEqual(b))
                continue;
            return false;
        }

        return true;
    }

    private sealed class ResultadoChetarRival
    {
        public int IAActualizadas { get; set; }
        public int CombatesDobles { get; set; }
        public int RhydonAgregados { get; set; }
        public int MegapiedrasAsignadas { get; set; }
        public int AlakazamAgregados { get; set; }
        public int RaikouSustituidos { get; set; }
        public int LugiaSustituidos { get; set; }
        public int ObjetosRetirados { get; set; }
    }
}
