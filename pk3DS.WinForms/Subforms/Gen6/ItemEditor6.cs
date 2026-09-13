using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using pk3DS.Core;
using pk3DS.Core.Structures;

namespace pk3DS.WinForms;

public partial class ItemEditor6 : Form
{
    public ItemEditor6(byte[][] infiles)
    {
        files = infiles;
        itemlist[0] = "";

        InitializeComponent();
        Setup();
    }

    private readonly byte[][] files;
    private static readonly int[] BattleBlockedItemIds = // Guarda los identificadores que procesa el botón de bloqueo.
    [ 
        .. Enumerable.Range(17, 21), // Incluye todos los identificadores desde el 17 hasta el 37.
        42, 43, 44, 54, 65, 66, 67, 134, 591, 708, // Incluye los identificadores aislados de tu lista.
        .. Enumerable.Range(149, 64), // Incluye todas las bayas con identificadores desde el 149 hasta el 212.
        686, 687, 688, // Incluye las tres bayas adicionales de tu lista.
    ]; 
    private readonly string[] itemlist = Main.Config.GetText(TextName.ItemNames);
    private readonly string[] itemflavor = Main.Config.GetText(TextName.ItemFlavor);

    private void Setup()
    {
        CB_Item.Items.AddRange(itemlist);
        CB_Item.SelectedIndex = 1;
        B_BanBattleItems.Enabled = Main.Config.ORAS; // Activa el botón cuando el juego cargado pertenece a ORAS.
    }

    private int entry = -1;

    private void ChangeEntry(object sender, EventArgs e)
    {
        SetEntry();
        entry = CB_Item.SelectedIndex;
        L_Index.Text = "Index: " + entry.ToString("000");
        GetEntry();
    }

    private void GetEntry()
    {
        if (entry < 1) return;
        Grid.SelectedObject = new Item(files[entry]);

        RTB.Text = itemflavor[entry].Replace("\\n", Environment.NewLine);
    }

    private void SetEntry()
    {
        if (entry < 1) return;
        files[entry] = ((Item)Grid.SelectedObject).Write();
    }

    private void IsFormClosing(object sender, FormClosingEventArgs e)
    {
        SetEntry();
    }

    private void B_BanBattleItems_Click(object sender, EventArgs e) // Atiende la pulsación del botón.
    { // Mantiene visible el estado del botón hasta disponer de un método que funcione en el juego.
        WinFormsUtil.Alert("Bloqueo pendiente: cambiar los datos de los objetos no impide usarlos en combate en ORAS."); // Explica el resultado de la prueba sin modificar ningún objeto.
    } // Termina la acción sin guardar cambios engañosos.

    public static int GetItemMapOffset()
    {
        if (Main.ExeFSPath == null) { WinFormsUtil.Alert("No exeFS code to load."); return -1; }
        string[] exefsFiles = Directory.GetFiles(Main.ExeFSPath);
        if (!File.Exists(exefsFiles[0]) || !Path.GetFileNameWithoutExtension(exefsFiles[0]).Contains("code")) { WinFormsUtil.Alert("No .code.bin detected."); return -1; }
        byte[] data = File.ReadAllBytes(exefsFiles[0]);

        byte[] reference = Main.Config.ORAS
            ? [0x92, 0x0A, 0x06, 0x3F, 0x75, 0x02] // ORAS (vanilla @ 47C640)
            : [0x92, 0x0A, 0x06, 0x3F, 0x41, 0x02]; // XY (vanilla @ 43DB74)

        return Util.IndexOfBytes(data, reference, 0x400000, 0) - 2 + reference.Length;
    }

    private void B_Table_Click(object sender, EventArgs e)
    {
        var items = files.Select(z => new Item(z));
        Clipboard.SetText(TableUtil.GetTable(items, itemlist));
        System.Media.SystemSounds.Asterisk.Play();
    }
}
