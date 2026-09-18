using WindowDeck.Interop;

namespace WindowDeck.Services;

internal sealed class GlobalHotkeyManager : NativeWindow, IDisposable
{
    private const int FirstHotkeyId = 1;
    private const int SecondHotkeyId = 2;
    private static readonly nint MessageOnlyWindowParent = new(-3);

    private bool disposed;
    private int activeHotkeyId;
    private uint activeModifiers;
    private uint activeVirtualKey;

    public GlobalHotkeyManager(uint modifiers, uint virtualKey)
    {
        CreateHandle(new CreateParams
        {
            Caption = "WindowDeck hotkey receiver",
            Parent = MessageOnlyWindowParent
        });

        IsRegistered = NativeMethods.RegisterHotKey(
            Handle,
            FirstHotkeyId,
            modifiers,
            virtualKey);
        if (IsRegistered)
        {
            activeHotkeyId = FirstHotkeyId;
            activeModifiers = modifiers;
            activeVirtualKey = virtualKey;
        }
    }

    public event EventHandler? HotkeyPressed;

    public bool IsRegistered { get; private set; }

    public bool TryChange(uint modifiers, uint virtualKey)
    {
        if (disposed || modifiers == 0 || virtualKey == 0)
        {
            return false;
        }

        if (IsRegistered && modifiers == activeModifiers && virtualKey == activeVirtualKey)
        {
            return true;
        }

        int candidateId = activeHotkeyId == FirstHotkeyId ? SecondHotkeyId : FirstHotkeyId;
        if (!NativeMethods.RegisterHotKey(Handle, candidateId, modifiers, virtualKey))
        {
            return false;
        }

        if (IsRegistered)
        {
            NativeMethods.UnregisterHotKey(Handle, activeHotkeyId);
        }

        activeHotkeyId = candidateId;
        activeModifiers = modifiers;
        activeVirtualKey = virtualKey;
        IsRegistered = true;
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (IsRegistered)
        {
            NativeMethods.UnregisterHotKey(Handle, activeHotkeyId);
        }

        DestroyHandle();
    }

    protected override void WndProc(ref Message message)
    {
        if (!disposed
            && message.Msg == NativeMethods.WmHotkey
            && message.WParam == activeHotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref message);
    }
}
