namespace WindowDeck
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null!;
        private ListView windowListView = null!;
        private ColumnHeader displayTitleColumn = null!;
        private ColumnHeader monitorColumn = null!;
        private Button refreshButton = null!;
        private Label statusLabel = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            windowListView = new ListView();
            displayTitleColumn = new ColumnHeader();
            monitorColumn = new ColumnHeader();
            refreshButton = new Button();
            statusLabel = new Label();
            SuspendLayout();
            //
            // windowListView
            //
            windowListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            windowListView.BorderStyle = BorderStyle.FixedSingle;
            windowListView.Columns.AddRange(new ColumnHeader[] { displayTitleColumn, monitorColumn });
            windowListView.FullRowSelect = true;
            windowListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            windowListView.HideSelection = false;
            windowListView.Location = new Point(16, 16);
            windowListView.MultiSelect = false;
            windowListView.Name = "windowListView";
            windowListView.Size = new Size(648, 685);
            windowListView.TabIndex = 0;
            windowListView.UseCompatibleStateImageBehavior = false;
            windowListView.View = View.Details;
            windowListView.MouseClick += WindowListView_MouseClick;
            //
            // displayTitleColumn
            //
            displayTitleColumn.Text = "Display title";
            displayTitleColumn.Width = 534;
            //
            // monitorColumn
            //
            monitorColumn.Text = "Monitor";
            monitorColumn.Width = 110;
            //
            // refreshButton
            //
            refreshButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            refreshButton.Location = new Point(570, 713);
            refreshButton.Name = "refreshButton";
            refreshButton.Size = new Size(94, 29);
            refreshButton.TabIndex = 2;
            refreshButton.Text = "Refresh";
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += RefreshButton_Click;
            //
            // statusLabel
            //
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.AutoEllipsis = true;
            statusLabel.Location = new Point(16, 718);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(540, 20);
            statusLabel.TabIndex = 3;
            statusLabel.Text = "Finding windows...";
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(680, 754);
            Controls.Add(refreshButton);
            Controls.Add(statusLabel);
            Controls.Add(windowListView);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(600, 400);
            Name = "Form1";
            StartPosition = FormStartPosition.Manual;
            Text = "WindowDeck";
            TopMost = true;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
