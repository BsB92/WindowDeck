namespace WindowDeck
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null!;
        private TextBox searchTextBox = null!;
        private FlowLayoutPanel windowListPanel = null!;

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
            searchTextBox = new TextBox();
            windowListPanel = new FlowLayoutPanel();
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
            windowListPanel.Size = new Size(648, 687);
            windowListPanel.TabIndex = 1;
            windowListPanel.WrapContents = false;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(680, 754);
            Controls.Add(windowListPanel);
            Controls.Add(searchTextBox);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(600, 400);
            Name = "Form1";
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
