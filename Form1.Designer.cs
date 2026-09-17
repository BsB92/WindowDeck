namespace WindowDeck
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null!;
        private TextBox searchTextBox = null!;
        private FlowLayoutPanel windowListPanel = null!;
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
            searchTextBox = new TextBox();
            windowListPanel = new FlowLayoutPanel();
            refreshButton = new Button();
            statusLabel = new Label();
            SuspendLayout();
            //
            // searchTextBox
            //
            searchTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            searchTextBox.Location = new Point(16, 16);
            searchTextBox.Name = "searchTextBox";
            searchTextBox.PlaceholderText = "Search windows";
            searchTextBox.Size = new Size(648, 27);
            searchTextBox.TabIndex = 0;
            searchTextBox.TextChanged += SearchTextBox_TextChanged;
            //
            // windowListPanel
            //
            windowListPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            windowListPanel.AutoScroll = true;
            windowListPanel.BorderStyle = BorderStyle.FixedSingle;
            windowListPanel.FlowDirection = FlowDirection.TopDown;
            windowListPanel.Location = new Point(16, 51);
            windowListPanel.Name = "windowListPanel";
            windowListPanel.Size = new Size(648, 650);
            windowListPanel.TabIndex = 1;
            windowListPanel.WrapContents = false;
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
            Controls.Add(windowListPanel);
            Controls.Add(searchTextBox);
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
