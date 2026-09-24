using WindowDeck.Interop;

namespace WindowDeck;

internal sealed class BufferedFlowLayoutPanel : FlowLayoutPanel
{
    public BufferedFlowLayoutPanel()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint,
            true);
        UpdateStyles();
    }

    public void BeginUpdate()
    {
        if (IsHandleCreated)
        {
            NativeMethods.SendMessage(Handle, NativeMethods.WmSetRedraw, 0, 0);
        }

        SuspendLayout();
    }

    public void EndUpdate()
    {
        ResumeLayout(performLayout: true);

        if (IsHandleCreated)
        {
            NativeMethods.SendMessage(Handle, NativeMethods.WmSetRedraw, 1, 0);
        }

        Invalidate(invalidateChildren: true);
        Update();
    }
}
