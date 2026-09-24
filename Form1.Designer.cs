namespace WindowDeck
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null!;
        private TextBox searchTextBox = null!;
        private TableLayoutPanel topBar = null!;
        private BufferedFlowLayoutPanel windowListPanel = null!;
        private Panel bottomBar = null!;
        private Button presentationModeButton = null!;
        private Button helpButton = null!;
        private Button settingsButton = null!;
        private ToolTip toolTip = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                applicationIconProvider.Dispose();
                Icon?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            topBar = new TableLayoutPanel();
            searchTextBox = new TextBox();
            windowListPanel = new BufferedFlowLayoutPanel();
            bottomBar = new Panel();
            presentationModeButton = new Button();
            helpButton = new Button();
            settingsButton = new Button();
            toolTip = new ToolTip(components);
            topBar.SuspendLayout();
            bottomBar.SuspendLayout();
            SuspendLayout();
            //
            // searchTextBox
            //
            searchTextBox.Dock = DockStyle.Fill;
            searchTextBox.Location = new Point(0, 4);
            searchTextBox.Margin = new Padding(0, 4, 8, 4);
            searchTextBox.Name = "searchTextBox";
            searchTextBox.BorderStyle = BorderStyle.FixedSingle;
            searchTextBox.Size = new Size(568, 27);
            searchTextBox.TabIndex = 0;
            searchTextBox.TextChanged += SearchTextBox_TextChanged;
            //
            // topBar
            //
            topBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            topBar.ColumnCount = 3;
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36F));
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36F));
            topBar.Controls.Add(searchTextBox, 0, 0);
            topBar.Controls.Add(helpButton, 1, 0);
            topBar.Controls.Add(settingsButton, 2, 0);
            topBar.Location = new Point(16, 12);
            topBar.Name = "topBar";
            topBar.RowCount = 1;
            topBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            topBar.Size = new Size(648, 36);
            topBar.TabIndex = 0;
            //
            // windowListPanel
            //
            windowListPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            windowListPanel.AutoScroll = true;
            windowListPanel.BorderStyle = BorderStyle.None;
            windowListPanel.FlowDirection = FlowDirection.TopDown;
            windowListPanel.Location = new Point(16, 56);
            windowListPanel.Name = "windowListPanel";
            windowListPanel.Size = new Size(648, 636);
            windowListPanel.TabIndex = 1;
            windowListPanel.WrapContents = false;
            //
            // bottomBar
            //
            bottomBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            bottomBar.Controls.Add(presentationModeButton);
            bottomBar.Location = new Point(16, 700);
            bottomBar.Name = "bottomBar";
            bottomBar.Size = new Size(648, 38);
            bottomBar.TabIndex = 2;
            //
            // bottom bar buttons
            //
            presentationModeButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            presentationModeButton.FlatStyle = FlatStyle.Flat;
            presentationModeButton.Location = new Point(0, 4);
            presentationModeButton.Size = new Size(180, 30);
            helpButton.Anchor = AnchorStyles.None;
            helpButton.FlatStyle = FlatStyle.Flat;
            helpButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            helpButton.Margin = new Padding(2);
            helpButton.Size = new Size(32, 32);
            settingsButton.Anchor = AnchorStyles.None;
            settingsButton.FlatStyle = FlatStyle.Flat;
            settingsButton.Font = new Font("Segoe UI Symbol", 11F, FontStyle.Regular);
            settingsButton.Margin = new Padding(2);
            settingsButton.Size = new Size(32, 32);
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(680, 754);
            Controls.Add(bottomBar);
            Controls.Add(windowListPanel);
            Controls.Add(topBar);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(600, 400);
            Name = "Form1";
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            topBar.ResumeLayout(false);
            topBar.PerformLayout();
            bottomBar.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
