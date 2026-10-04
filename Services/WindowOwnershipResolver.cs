using WindowDeck.Models;

namespace WindowDeck.Services;

internal static class WindowOwnershipResolver
{
    public static IReadOnlyList<WindowInfo> Resolve(
        IReadOnlyList<WindowInfo> windows,
        Func<nint, nint> getOwner,
        Func<nint, uint?> getLiveProcessId)
    {
        Dictionary<nint, WindowInfo> byHandle = windows.ToDictionary(window => window.Handle);
        List<WindowInfo> resolved = [];

        foreach (WindowInfo window in windows)
        {
            // Follow actual ownership, including untitled/hidden intermediate owners.
            // Never infer an owner from a matching application name or process alone.
            HashSet<nint> visited = [window.Handle];
            nint ownerHandle = getOwner(window.Handle);
            WindowInfo? rootOwner = null;
            bool invalidChain = false;
            for (int depth = 0; ownerHandle != 0; depth++)
            {
                uint? ownerProcessId = getLiveProcessId(ownerHandle);
                if (depth >= 64 || !visited.Add(ownerHandle) || !ownerProcessId.HasValue)
                {
                    invalidChain = true;
                    break;
                }

                if (ownerProcessId != window.ProcessId)
                {
                    break;
                }

                if (byHandle.TryGetValue(ownerHandle, out WindowInfo? owner)
                    && owner.ProcessId == ownerProcessId)
                {
                    rootOwner = owner;
                }

                ownerHandle = getOwner(ownerHandle);
            }

            resolved.Add(!invalidChain && rootOwner is not null
                ? window with
                {
                    OwnerWindow = WindowIdentity.From(rootOwner),
                    ApplicationId = rootOwner.ApplicationId,
                    ApplicationName = rootOwner.ApplicationName
                }
                : window);
        }

        return resolved;
    }
}
