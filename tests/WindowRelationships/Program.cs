using WindowDeck.Models;
using WindowDeck.Services;

static WindowInfo Window(int handle, uint pid = 10, string title = "Main", bool minimized = false) =>
    new(handle, pid, "app", "Application", title, title, null, 1, minimized);

static void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine("PASS " + name);
}

WindowInfo first = Window(1);
WindowInfo second = Window(2, title: "Second main");
WindowInfo dialog = Window(3, title: "Tool needle");
WindowInfo nested = Window(4, title: "Nested");
Dictionary<nint, nint> owners = new() { [3] = 1, [4] = 3, [5] = 2, [6] = 99, [99] = 1 };
Dictionary<nint, uint> pids = new() { [1] = 10, [2] = 10, [3] = 10, [4] = 10, [5] = 10, [6] = 10, [99] = 10 };
IReadOnlyList<WindowInfo> Resolve(params WindowInfo[] windows) => WindowOwnershipResolver.Resolve(
    windows,
    handle => owners.GetValueOrDefault(handle),
    handle => pids.TryGetValue(handle, out uint pid) ? pid : null);

IReadOnlyList<WindowInfo> result = Resolve(first, second, dialog, nested, Window(5), Window(6));
Check(result.Single(w => w.Handle == 3).OwnerWindow == WindowIdentity.From(first), "dialog belongs to first main");
Check(result.Single(w => w.Handle == 4).OwnerWindow == WindowIdentity.From(first), "nested dialog flattened under main");
Check(result.Single(w => w.Handle == 5).OwnerWindow == WindowIdentity.From(second), "two mains in same process stay separate");
Check(result.Single(w => w.Handle == 6).OwnerWindow == WindowIdentity.From(first), "hidden intermediate owner traversed");
Check(result.Single(w => w.Handle == 2).OwnerWindow is null, "same process does not imply ownership");

owners[1] = 3;
Check(Resolve(first, dialog).All(w => w.OwnerWindow is null), "cyclic ownership stays standalone");
owners.Remove(1);
pids[99] = 20;
Check(Resolve(first, Window(6)).Last().OwnerWindow is null, "cross-process chain is not guessed");
pids.Remove(99);
Check(Resolve(first, Window(6)).Last().OwnerWindow is null, "destroyed intermediate stays standalone");
pids[1] = 20;
Check(Resolve(first, dialog).Last().OwnerWindow is null, "reused owner handle with different PID is rejected");
pids[1] = 10;

result = Resolve(first, second, dialog);
WindowInfo[] matches = WindowListPresentation.Create(result, "needle", true, true).SelectMany(g => g).ToArray();
Check(matches.Length == 2 && matches.Any(w => w.Handle == 1) && matches.Any(w => w.Handle == 3), "search includes matching child and owner only");
Check(WindowListPresentation.Create(result, "absent", true, true).Count == 0, "unmatched search remains empty");
Check(WindowListPresentation.Create(result, "needle", false, true).SelectMany(g => g).Count() == 2, "ownership context also works without app grouping");
result = Resolve(first with { IsMinimized = true }, dialog);
Check(WindowListPresentation.Create(result, "needle", true, false).SelectMany(g => g).Single().Handle == 3, "filtered owner does not hide matching child");
Check(WindowListPresentation.Create(result, "", true, true).SelectMany(g => g).Count() == 2, "unfiltered snapshot has no duplicates");
Console.WriteLine("All window relationship checks passed.");
