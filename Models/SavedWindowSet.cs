namespace WindowDeck.Models;

internal enum SavedWindowKind
{
    Unsupported,
    ExplorerFolder,
    ExcelWorkbook,
    WordDocument,
    PowerPointPresentation,
    DocumentFile,
    Application
}

internal sealed class SavedWindowEntry
{
    public string ApplicationName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public SavedWindowKind Kind { get; set; }
    public string? ResourcePath { get; set; }
    public string? ExecutablePath { get; set; }
}

internal sealed class SavedWindowSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    public List<SavedWindowEntry> Windows { get; set; } = [];
}

internal sealed class SavedWindowSetsDocument
{
    public int Version { get; set; } = 1;
    public List<SavedWindowSet> Sets { get; set; } = [];
}
