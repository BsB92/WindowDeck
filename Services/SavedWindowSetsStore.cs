using System.Text.Json;
using System.Text.Json.Serialization;
using WindowDeck.Models;

namespace WindowDeck.Services;

internal sealed class SavedWindowSetsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowDeck", "window-sets.json");

    public List<SavedWindowSet> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        SavedWindowSetsDocument? document = JsonSerializer.Deserialize<SavedWindowSetsDocument>(
            File.ReadAllText(FilePath), Options);
        if (document is null || document.Version != 1 || document.Sets is null
            || document.Sets.Count > 1000
            || document.Sets.Any(set => set is null
                || set.Id == Guid.Empty
                || string.IsNullOrWhiteSpace(set.Name)
                || set.Windows is null
                || set.Windows.Count > 2000
                || set.Windows.Any(window => window is null
                    || !Enum.IsDefined(window.Kind)
                    || window.ApplicationName is null
                    || window.WindowTitle is null))
            || document.Sets.Select(set => set.Id).Distinct().Count() != document.Sets.Count)
        {
            throw new InvalidDataException("The saved window sets file is invalid or uses an unsupported version.");
        }

        return document.Sets;
    }

    public void Save(IReadOnlyList<SavedWindowSet> sets)
    {
        string directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        string temporary = FilePath + ".tmp";

        try
        {
            string json = JsonSerializer.Serialize(
                new SavedWindowSetsDocument { Sets = sets.ToList() }, Options);
            File.WriteAllText(temporary, json);
            File.Move(temporary, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
