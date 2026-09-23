namespace WindowDeck
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null!;
        private TextBox searchTextBox = null!;
        private FlowLayoutPanel windowListPanel = null!;
        private TableLayoutPanel bottomBar = null!;
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
            searchTextBox = new TextBox();
            windowListPanel = new FlowLayoutPanel();
            bottomBar = new TableLayoutPanel();
            presentationModeButton = new Button();
            helpButton = new Button();
            settingsButton = new Button();
            toolTip = new ToolTip(components);
            bottomBar.SuspendLayout();
            SuspendLayout();
            //
            // searchTextBox
            //
            searchTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            searchTextBox.Location = new Point(16, 16);
            searchTextBox.Name = "searchTextBox";
            searchTextBox.BorderStyle = BorderStyle.FixedSingle;
            searchTextBox.Size = new Size(648, 27);
            searchTextBox.TabIndex = 0;
            searchTextBox.TextChanged += SearchTextBox_TextChanged;
            //
            // windowListPanel
            //
            windowListPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            windowListPanel.AutoScroll = true;
            windowListPanel.BorderStyle = BorderStyle.None;
            windowListPanel.FlowDirection = FlowDirection.TopDown;
            windowListPanel.Location = new Point(16, 51);
            windowListPanel.Name = "windowListPanel";
            windowListPanel.Size = new Size(648, 641);
            windowListPanel.TabIndex = 1;
            windowListPanel.WrapContents = false;
            //
            // bottomBar
            //
            bottomBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            bottomBar.ColumnCount = 3;
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            bottomBar.Controls.Add(presentationModeButton, 0, 0);
            bottomBar.Controls.Add(helpButton, 1, 0);
            bottomBar.Controls.Add(settingsButton, 2, 0);
            bottomBar.Location = new Point(16, 700);
            bottomBar.Name = "bottomBar";
            bottomBar.RowCount = 1;
            bottomBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            bottomBar.Size = new Size(648, 38);
            bottomBar.TabIndex = 2;
            //
            // bottom bar buttons
            //
            presentationModeButton.Dock = DockStyle.Fill;
            presentationModeButton.Enabled = false;
            presentationModeButton.FlatStyle = FlatStyle.Flat;
            presentationModeButton.Margin = Padding.Empty;
            helpButton.Dock = DockStyle.Fill;
            helpButton.FlatStyle = FlatStyle.Flat;
            helpButton.Margin = Padding.Empty;
            settingsButton.Dock = DockStyle.Fill;
            settingsButton.FlatStyle = FlatStyle.Flat;
            settingsButton.Margin = Padding.Empty;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(680, 754);
            Controls.Add(bottomBar);
            Controls.Add(windowListPanel);
            Controls.Add(searchTextBox);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(600, 400);
            Name = "Form1";
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            bottomBar.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
