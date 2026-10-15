using System.Drawing.Drawing2D;

namespace WindowDeck;

// Both states are drawn as matching vector strokes, without an emoji or badge.
internal sealed class SavedWindowStatusIcon : Control
{
    private bool isReady;

    public SavedWindowStatusIcon()
    {
        Size = new Size(24, 28);
        Margin = new Padding(2, 2, 4, 0);
        AccessibleRole = AccessibleRole.StaticText;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
    }

    public bool IsReady
    {
        get => isReady;
        set
        {
            if (isReady == value) return;
            isReady = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color stroke = isReady
            ? Color.FromArgb(27, 150, 83)
            : Color.FromArgb(211, 58, 58);

        using Pen pen = new(stroke, 2.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        float middleX = ClientSize.Width / 2f;
        float middleY = ClientSize.Height / 2f;
        if (isReady)
        {
            e.Graphics.DrawLines(pen,
            [
                new PointF(middleX - 7f, middleY),
                new PointF(middleX - 2f, middleY + 5f),
                new PointF(middleX + 8f, middleY - 6f)
            ]);
        }
        else
        {
            e.Graphics.DrawLine(pen,
                middleX - 5f, middleY - 5f,
                middleX + 5f, middleY + 5f);
            e.Graphics.DrawLine(pen,
                middleX + 5f, middleY - 5f,
                middleX - 5f, middleY + 5f);
        }
    }
}
