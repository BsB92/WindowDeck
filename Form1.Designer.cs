namespace WindowDeck
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null!;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            refreshButton = new Button();
            resultCountLabel = new Label();
            windowListView = new ListView();
            displayTitleColumn = new ColumnHeader();
            applicationColumn = new ColumnHeader();
            originalTitleColumn = new ColumnHeader();
            processIdColumn = new ColumnHeader();
            SuspendLayout();
            //
            // refreshButton
            //
            refreshButton.Location = new Point(12, 12);
            refreshButton.Name = "refreshButton";
            refreshButton.Size = new Size(94, 29);
            refreshButton.TabIndex = 0;
            refreshButton.Text = "Refresh";
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += RefreshButton_Click;
            //
            // resultCountLabel
            //
            resultCountLabel.AutoSize = true;
            resultCountLabel.Location = new Point(122, 17);
            resultCountLabel.Name = "resultCountLabel";
            resultCountLabel.Size = new Size(84, 20);
            resultCountLabel.TabIndex = 1;
            resultCountLabel.Text = "Windows: 0";
            //
            // windowListView
            //
            windowListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            windowListView.Columns.AddRange(new ColumnHeader[] { displayTitleColumn, applicationColumn, originalTitleColumn, processIdColumn });
            windowListView.FullRowSelect = true;
            windowListView.GridLines = true;
            windowListView.Location = new Point(12, 50);
            windowListView.MultiSelect = false;
            windowListView.Name = "windowListView";
            windowListView.Size = new Size(960, 499);
            windowListView.TabIndex = 2;
            windowListView.UseCompatibleStateImageBehavior = false;
            windowListView.View = View.Details;
            //
            // displayTitleColumn
            //
            displayTitleColumn.Text = "Display title";
            displayTitleColumn.Width = 300;
            //
            // applicationColumn
            //
            applicationColumn.Text = "Application";
            applicationColumn.Width = 130;
            //
            // originalTitleColumn
            //
            originalTitleColumn.Text = "Original title";
            originalTitleColumn.Width = 400;
            //
            // processIdColumn
            //
            processIdColumn.Text = "Process ID";
            processIdColumn.Width = 90;
            //
            // Form1
            //
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 561);
            Controls.Add(windowListView);
            Controls.Add(resultCountLabel);
            Controls.Add(refreshButton);
            MinimumSize = new Size(700, 400);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WindowDeck — Stage 2 diagnostics";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button refreshButton;
        private Label resultCountLabel;
        private ListView windowListView;
        private ColumnHeader displayTitleColumn;
        private ColumnHeader applicationColumn;
        private ColumnHeader originalTitleColumn;
        private ColumnHeader processIdColumn;
    }
}
