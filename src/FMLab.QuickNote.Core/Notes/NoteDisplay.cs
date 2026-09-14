namespace FMLab.QuickNote.Core.Notes;

/// <summary>Texto de preview usado na lista de histórico quando a nota não tem título.</summary>
public static class NoteDisplay
{
    public static string GetDisplayTitle(Note note, int maxLength = 60)
    {
        if (!string.IsNullOrWhiteSpace(note.Title))
        {
            return note.Title;
        }

        var firstLine = note.Content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        return firstLine.Length > maxLength ? firstLine[..maxLength].TrimEnd() + "…" : firstLine;
    }
}
