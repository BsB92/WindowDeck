using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck
{
    public partial class Form1 : Form
    {
        private readonly WindowEnumerator windowEnumerator = new();

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshWindowList();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshWindowList();
        }

        private void RefreshWindowList()
        {
            IReadOnlyList<WindowInfo> windows;

            try
            {
                windows = windowEnumerator.Enumerate(Handle);
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                MessageBox.Show(
                    this,
                    exception.Message,
                    "Window enumeration failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            windowListView.BeginUpdate();
            try
            {
                windowListView.Items.Clear();

                foreach (WindowInfo window in windows)
                {
                    ListViewItem item = new(window.DisplayTitle);
                    item.SubItems.Add(window.ProcessName);
                    item.SubItems.Add(window.OriginalTitle);
                    item.SubItems.Add(window.ProcessId.ToString());
                    windowListView.Items.Add(item);
                }

                resultCountLabel.Text = $"Windows: {windows.Count}";
            }
            finally
            {
                windowListView.EndUpdate();
            }
        }
    }
}
