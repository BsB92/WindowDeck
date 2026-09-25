namespace WindowDeck;

internal sealed class ScreenIdentificationForm : Form
{
    private const int WsExNoActivate = 0x08000000;

    public ScreenIdentificationForm(int screenNumber, Rectangle workArea)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ClientSize = new Size(180, 120);
        BackColor = SystemColors.Highlight;
        ForeColor = SystemColors.HighlightText;

        Label numberLabel = new()
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 44F, FontStyle.Bold),
            ForeColor = ForeColor,
            Text = screenNumber.ToString(),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(numberLabel);

        Location = new Point(
            workArea.Left + Math.Max(0, (workArea.Width - Width) / 2),
            workArea.Top + Math.Max(0, (workArea.Height - Height) / 2));
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= WsExNoActivate;
            return parameters;
        }
    }
}
