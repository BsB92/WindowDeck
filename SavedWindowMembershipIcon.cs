using System.Drawing.Drawing2D;

namespace WindowDeck;

// A vector floppy-disk symbol marks an entry that is ALREADY a member of
// the selected saved set. It is independent of the green/red restore status.
internal sealed class SavedWindowMembershipIcon : Control
{
    public SavedWindowMembershipIcon(Color accent)
    {
        ForeColor = accent;
        Size = new Size(22, 32);
        Margin = new Padding(0, 0, 4, 0);
        AccessibleRole = AccessibleRole.StaticText;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        float left = (ClientSize.Width - 16f) / 2f;
        float top = (ClientSize.Height - 16f) / 2f;

        using Pen pen = new(ForeColor, 1.65f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using SolidBrush brush = new(ForeColor);
        using GraphicsPath outline = new();
        outline.AddLines(
        [
            new PointF(left + 1f, top + 1f),
            new PointF(left + 11.4f, top + 1f),
            new PointF(left + 15f, top + 4.5f),
            new PointF(left + 15f, top + 15f),
            new PointF(left + 1f, top + 15f)
        ]);
        outline.CloseFigure();
        e.Graphics.DrawPath(pen, outline);

        // Upper disk slot and the lower label area form one crisp,
        // monochrome symbol in both the light and dark themes.
        e.Graphics.DrawLine(pen, left + 4f, top + 1.6f, left + 4f, top + 6f);
        e.Graphics.DrawLine(pen, left + 11f, top + 1.6f, left + 11f, top + 6f);
        e.Graphics.DrawLine(pen, left + 4f, top + 6f, left + 11f, top + 6f);
        e.Graphics.DrawRectangle(pen, left + 3.5f, top + 9f, 9f, 6f);
        e.Graphics.FillRectangle(brush, left + 9.1f, top + 2.3f, 1.4f, 2.5f);
    }
}
