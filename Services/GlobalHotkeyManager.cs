using WindowDeck.Interop;

namespace WindowDeck.Services;

internal sealed class GlobalHotkeyManager : NativeWindow, IDisposable
{
    private const int WindowDeckHotkeyId = 1;
    private static readonly nint MessageOnlyWindowParent = new(-3);

    private bool disposed;

    public GlobalHotkeyManager()
    {
        CreateHandle(new CreateParams
        {
            Caption = "WindowDeck hotkey receiver",
            Parent = MessageOnlyWindowParent
        });

        IsRegistered = NativeMethods.RegisterHotKey(
            Handle,
            WindowDeckHotkeyId,
            NativeMethods.ModWin,
            NativeMethods.VkOem3);
    }

    public event EventHandler? HotkeyPressed;

    public bool IsRegistered { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (IsRegistered)
        {
            NativeMethods.UnregisterHotKey(Handle, WindowDeckHotkeyId);
        }

        DestroyHandle();
    }

    protected override void WndProc(ref Message message)
    {
        if (!disposed
            && message.Msg == NativeMethods.WmHotkey
            && message.WParam == WindowDeckHotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref message);
    }
}
