using System.ComponentModel;
using System.Diagnostics;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

public partial class Form1 : Form
{
    private readonly WindowEnumerator windowEnumerator = new();
    private readonly WindowActivator windowActivator = new();

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

    private void ActivateSelectedButton_Click(object sender, EventArgs e)
    {
        if (windowListView.SelectedItems.Count != 1
            || windowListView.SelectedItems[0].Tag is not WindowInfo window)
        {
            statusLabel.Text = "Select one window to activate";
            return;
        }

        WindowActivationResult result = windowActivator.Activate(window);
        switch (result)
        {
            case WindowActivationResult.Activated:
                statusLabel.Text = $"Activated: {window.DisplayTitle}";
                break;
            case WindowActivationResult.WindowUnavailable:
                ShowActivationFailure(
                    "The selected window is no longer available. Refresh the list and try again.");
                break;
            case WindowActivationResult.RestorationFailed:
                ShowActivationFailure("The selected window could not be restored.");
                break;
            case WindowActivationResult.ForegroundActivationFailed:
                ShowActivationFailure(
                    "Windows did not allow or complete activation of the selected window.");
                break;
        }
    }

    private void ShowActivationFailure(string message)
    {
        statusLabel.Text = message;
        MessageBox.Show(
            this,
            message,
            "WindowDeck",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void RefreshWindowList()
    {
        windowListView.BeginUpdate();

        try
        {
            windowListView.Items.Clear();

            foreach (WindowInfo window in windowEnumerator.Enumerate())
            {
                ListViewItem item = new(window.DisplayTitle)
                {
                    Tag = window
                };
                item.SubItems.Add(GetApplicationName(window.ProcessId));
                item.SubItems.Add(window.OriginalTitle);
                item.SubItems.Add(window.ProcessId.ToString());
                item.SubItems.Add($"0x{window.Handle:X}");
                item.SubItems.Add(window.MonitorNumber?.ToString() ?? "?");
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
