namespace WindowDeck
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null!;
        private ListView windowListView = null!;
        private ColumnHeader displayTitleColumn = null!;
        private ColumnHeader applicationColumn = null!;
        private ColumnHeader originalTitleColumn = null!;
        private ColumnHeader handleColumn = null!;
        private ColumnHeader processIdColumn = null!;
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
            applicationColumn = new ColumnHeader();
            originalTitleColumn = new ColumnHeader();
            handleColumn = new ColumnHeader();
            processIdColumn = new ColumnHeader();
            refreshButton = new Button();
            statusLabel = new Label();
            SuspendLayout();
            //
            // windowListView
            //
            windowListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            windowListView.Columns.AddRange(new ColumnHeader[] { displayTitleColumn, applicationColumn, originalTitleColumn, processIdColumn, handleColumn });
            windowListView.FullRowSelect = true;
            windowListView.Location = new Point(12, 12);
            windowListView.MultiSelect = false;
            windowListView.Name = "windowListView";
            windowListView.Size = new Size(776, 385);
            windowListView.TabIndex = 0;
            windowListView.UseCompatibleStateImageBehavior = false;
            windowListView.View = View.Details;
            //
            // displayTitleColumn
            //
            displayTitleColumn.Text = "Display title";
            displayTitleColumn.Width = 260;
            //
            // applicationColumn
            //
            applicationColumn.Text = "Application";
            applicationColumn.Width = 140;
            //
            // originalTitleColumn
            //
            originalTitleColumn.Text = "Original title";
            originalTitleColumn.Width = 260;
            //
            // handleColumn
            //
            handleColumn.Text = "Handle";
            handleColumn.Width = 120;
            //
            // processIdColumn
            //
            processIdColumn.Text = "Process ID";
            processIdColumn.Width = 90;
            //
            // refreshButton
            //
            refreshButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            refreshButton.Location = new Point(694, 411);
            refreshButton.Name = "refreshButton";
            refreshButton.Size = new Size(94, 29);
            refreshButton.TabIndex = 1;
            refreshButton.Text = "Refresh";
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += RefreshButton_Click;
            //
            // statusLabel
            //
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(12, 416);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(123, 20);
            statusLabel.TabIndex = 2;
            statusLabel.Text = "Finding windows...";
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 452);
            Controls.Add(statusLabel);
            Controls.Add(refreshButton);
            Controls.Add(windowListView);
            MinimumSize = new Size(600, 350);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WindowDeck — Stage 2";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
