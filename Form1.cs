using System.ComponentModel;
using System.Diagnostics;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

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
        windowListView.BeginUpdate();

        try
        {
            windowListView.Items.Clear();

            foreach (WindowInfo window in windowEnumerator.Enumerate())
            {
                ListViewItem item = new(window.DisplayTitle);
                item.SubItems.Add(GetApplicationName(window.ProcessId));
                item.SubItems.Add(window.OriginalTitle);
                item.SubItems.Add(window.ProcessId.ToString());
                item.SubItems.Add($"0x{window.Handle:X}");
                windowListView.Items.Add(item);
            }

            statusLabel.Text = $"{windowListView.Items.Count} windows found";
        }
        catch (Win32Exception exception)
        {
            statusLabel.Text = "Window enumeration failed";
            MessageBox.Show(
                this,
                exception.Message,
                "WindowDeck",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            windowListView.EndUpdate();
        }
    }

    private static string GetApplicationName(uint processId)
    {
        try
        {
            using Process process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return "Unavailable";
        }
        catch (InvalidOperationException)
        {
            return "Unavailable";
        }
        catch (Win32Exception)
        {
            return "Unavailable";
        }
    }
}
