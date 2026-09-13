namespace pk3DS.WinForms;

partial class ItemEditor6
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.Grid = new System.Windows.Forms.PropertyGrid();
        this.L_Index = new System.Windows.Forms.Label();
        this.RTB = new System.Windows.Forms.RichTextBox();
        this.L_Item = new System.Windows.Forms.Label();
        this.CB_Item = new System.Windows.Forms.ComboBox();
        this.B_Table = new System.Windows.Forms.Button();
        this.B_BanBattleItems = new System.Windows.Forms.Button(); // Crea el botón que aplica el bloqueo de la lista.
        this.SuspendLayout();
        //
        // Grid
        //
        this.Grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                                                  | System.Windows.Forms.AnchorStyles.Left)
                                                                 | System.Windows.Forms.AnchorStyles.Right)));
        this.Grid.LineColor = System.Drawing.SystemColors.ControlDark;
        this.Grid.Location = new System.Drawing.Point(12, 124); // Baja la cuadrícula para dejar una fila libre a los botones.
        this.Grid.Name = "Grid";
        this.Grid.Size = new System.Drawing.Size(316, 250); // Conserva el borde inferior de la cuadrícula en la misma posición.
        this.Grid.TabIndex = 52;
        //
        // L_Index
        //
        this.L_Index.AutoSize = true;
        this.L_Index.Location = new System.Drawing.Point(221, 14);
        this.L_Index.Name = "L_Index";
        this.L_Index.Size = new System.Drawing.Size(39, 13);
        this.L_Index.TabIndex = 51;
        this.L_Index.Text = "Index: ";
        //
        // RTB
        //
        this.RTB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                                                                | System.Windows.Forms.AnchorStyles.Right)));
        this.RTB.Location = new System.Drawing.Point(11, 37);
        this.RTB.Name = "RTB";
        this.RTB.ReadOnly = true;
        this.RTB.Size = new System.Drawing.Size(316, 51);
        this.RTB.TabIndex = 50;
        this.RTB.Text = "";
        //
        // L_Item
        //
        this.L_Item.Location = new System.Drawing.Point(12, 10);
        this.L_Item.Name = "L_Item";
        this.L_Item.Size = new System.Drawing.Size(51, 21);
        this.L_Item.TabIndex = 49;
        this.L_Item.Text = "Item:";
        this.L_Item.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        //
        // CB_Item
        //
        this.CB_Item.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
        this.CB_Item.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
        this.CB_Item.DropDownWidth = 120;
        this.CB_Item.FormattingEnabled = true;
        this.CB_Item.Location = new System.Drawing.Point(71, 10);
        this.CB_Item.Name = "CB_Item";
        this.CB_Item.Size = new System.Drawing.Size(144, 21);
        this.CB_Item.TabIndex = 48;
        this.CB_Item.SelectedIndexChanged += new System.EventHandler(this.ChangeEntry);
        //
        // B_Table
        //
        this.B_Table.Location = new System.Drawing.Point(253, 94);
        this.B_Table.Name = "B_Table";
        this.B_Table.Size = new System.Drawing.Size(75, 23);
        this.B_Table.TabIndex = 53;
        this.B_Table.Text = "Export Table";
        this.B_Table.UseVisualStyleBackColor = true;
        this.B_Table.Click += new System.EventHandler(this.B_Table_Click);
        this.B_BanBattleItems.Enabled = false; // Setup lo activará cuando se haya cargado un juego ORAS.
        this.B_BanBattleItems.Location = new System.Drawing.Point(12, 94); // Sitúa el botón a la izquierda de «Export Table».
        this.B_BanBattleItems.Name = "B_BanBattleItems"; // Asigna un nombre para identificarlo desde el código.
        this.B_BanBattleItems.Size = new System.Drawing.Size(230, 23); // Le da espacio suficiente al texto del botón.
        this.B_BanBattleItems.TabIndex = 54; // Define su posición en el orden de navegación con Tab.
        this.B_BanBattleItems.Text = "Bloqueo en combate (pendiente)"; // Evita presentar como funcional un bloqueo que el juego no aplica.
        this.B_BanBattleItems.UseVisualStyleBackColor = true; // Mantiene el aspecto normal de los botones de Windows.
        this.B_BanBattleItems.Click += new System.EventHandler(this.B_BanBattleItems_Click); // Muestra el estado de la función pendiente.
        //
        // ItemEditor6
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(339, 381);
        this.Controls.Add(this.B_Table);
        this.Controls.Add(this.B_BanBattleItems); // Incorpora el nuevo botón a la ventana.
        this.Controls.Add(this.Grid);
        this.Controls.Add(this.L_Index);
        this.Controls.Add(this.RTB);
        this.Controls.Add(this.L_Item);
        this.Controls.Add(this.CB_Item);
        this.MaximizeBox = false;
        this.MaximumSize = new System.Drawing.Size(355, 1000);
        this.MinimumSize = new System.Drawing.Size(355, 420);
        this.Name = "ItemEditor6";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Item Editor";
        this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.IsFormClosing);
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.PropertyGrid Grid;
    private System.Windows.Forms.Label L_Index;
    private System.Windows.Forms.RichTextBox RTB;
    private System.Windows.Forms.Label L_Item;
    private System.Windows.Forms.ComboBox CB_Item;
    private System.Windows.Forms.Button B_Table;
    private System.Windows.Forms.Button B_BanBattleItems; // Guarda la referencia al botón para activar su acción desde el código.
}
