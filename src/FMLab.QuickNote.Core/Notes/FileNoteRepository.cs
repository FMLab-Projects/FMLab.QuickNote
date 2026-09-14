using System.Globalization;
using System.Text;

namespace FMLab.QuickNote.Core.Notes;

/// <summary>
/// Stores each note as a single plain-text file: a small "Key: value" header
/// (Title/CreatedAt/UpdatedAt/CompletedAt), a "---" separator line, and the
/// raw note content as the rest of the file. No database engine involved —
/// notes are meant to be readable/editable outside the app if needed.
/// </summary>
public sealed class FileNoteRepository : INoteRepository
{
    private const string Extension = ".txt";

    private readonly string _directory;
    private readonly Func<DateTimeOffset> _clock;

    public FileNoteRepository(string directory, Func<DateTimeOffset>? clock = null)
    {
        _directory = directory;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        Directory.CreateDirectory(_directory);
    }

    public static string GetDefaultDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FMLab.QuickNote",
        "notes");

    public Note Create(string? title, string content)
    {
        var now = _clock();
        var note = new Note
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = null,
        };
        Save(note);
        return note;
    }

    public void Update(Note note)
    {
        note.UpdatedAt = _clock();
        Save(note);
    }

    public Note? GetById(Guid id) =>
        File.Exists(GetPath(id)) && TryParse(File.ReadAllText(GetPath(id)), id, out var note) ? note : null;

    public Note? GetActiveDraft() =>
        EnumerateNotes().Where(n => n.IsActive).OrderByDescending(n => n.UpdatedAt).FirstOrDefault();

    public IReadOnlyList<Note> GetHistory() =>
        EnumerateNotes().OrderByDescending(n => n.UpdatedAt).ToList();

    public void Complete(Guid id)
    {
        var note = GetById(id) ?? throw new InvalidOperationException($"Note '{id}' not found.");
        note.CompletedAt = _clock();
        Update(note);
    }

    public void Reactivate(Guid id)
    {
        var note = GetById(id) ?? throw new InvalidOperationException($"Note '{id}' not found.");
        note.CompletedAt = null;
        Update(note);
    }

    public void AddComment(Guid noteId, string text)
    {
        var trimmed = (text ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new ArgumentException("O comentário não pode ficar vazio.", nameof(text));
        }

        if (trimmed.Contains('\n') || trimmed.Contains('\r'))
        {
            throw new ArgumentException("O comentário não pode ter quebras de linha.", nameof(text));
        }

        if (trimmed.Length > Comment.MaxLength)
        {
            throw new ArgumentException(
                $"O comentário não pode passar de {Comment.MaxLength} caracteres.", nameof(text));
        }

        var note = GetById(noteId) ?? throw new InvalidOperationException($"Note '{noteId}' not found.");
        note.Comments = [.. note.Comments, new Comment(_clock(), trimmed)];
        Update(note);
    }

    private IEnumerable<Note> EnumerateNotes()
    {
        foreach (var path in Directory.EnumerateFiles(_directory, $"*{Extension}"))
        {
            if (Guid.TryParse(Path.GetFileNameWithoutExtension(path), out var id)
                && TryParse(File.ReadAllText(path), id, out var note))
            {
                yield return note;
            }
        }
    }

    private void Save(Note note) => File.WriteAllText(GetPath(note.Id), Serialize(note));

    private string GetPath(Guid id) => Path.Combine(_directory, $"{id:D}{Extension}");

    private static string Serialize(Note note)
    {
        var builder = new StringBuilder();
        builder.Append("Title: ").Append(note.Title).Append('\n');
        builder.Append("CreatedAt: ").Append(Format(note.CreatedAt)).Append('\n');
        builder.Append("UpdatedAt: ").Append(Format(note.UpdatedAt)).Append('\n');
        builder.Append("CompletedAt: ").Append(note.CompletedAt is { } c ? Format(c) : string.Empty).Append('\n');
        foreach (var comment in note.Comments)
        {
            builder.Append("Comment: ").Append(Format(comment.CreatedAt)).Append('|').Append(comment.Text).Append('\n');
        }
        builder.Append("---\n");
        builder.Append(note.Content);
        return builder.ToString();

        static string Format(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static bool TryParse(string text, Guid id, out Note note)
    {
        note = null!;
        using var reader = new StringReader(text);

        var title = ReadHeaderValue(reader.ReadLine(), "Title: ");
        var createdAtText = ReadHeaderValue(reader.ReadLine(), "CreatedAt: ");
        var updatedAtText = ReadHeaderValue(reader.ReadLine(), "UpdatedAt: ");
        var completedAtText = ReadHeaderValue(reader.ReadLine(), "CompletedAt: ");

        // Zero ou mais linhas "Comment: <timestamp>|<texto>" repetíveis antes do separador —
        // notas gravadas antes da Fase 15 simplesmente não têm nenhuma, e continuam válidas.
        var comments = new List<Comment>();
        string? line;
        while ((line = reader.ReadLine()) is not null && line != "---")
        {
            if (!TryParseCommentLine(line, out var comment))
            {
                return false;
            }

            comments.Add(comment);
        }

        if (line != "---" || createdAtText is null || updatedAtText is null
            || !TryParseTimestamp(createdAtText, out var createdAt)
            || !TryParseTimestamp(updatedAtText, out var updatedAt))
        {
            return false;
        }

        DateTimeOffset? completedAt = null;
        if (!string.IsNullOrEmpty(completedAtText))
        {
            if (!TryParseTimestamp(completedAtText, out var parsed))
            {
                return false;
            }

            completedAt = parsed;
        }

        note = new Note
        {
            Id = id,
            Title = string.IsNullOrEmpty(title) ? null : title,
            Content = reader.ReadToEnd(),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            CompletedAt = completedAt,
            Comments = comments.OrderBy(c => c.CreatedAt).ToList(),
        };
        return true;
    }

    private static bool TryParseCommentLine(string line, out Comment comment)
    {
        comment = null!;
        const string prefix = "Comment: ";
        if (!line.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var value = line[prefix.Length..];
        var separatorIndex = value.IndexOf('|');
        if (separatorIndex < 0 || !TryParseTimestamp(value[..separatorIndex], out var createdAt))
        {
            return false;
        }

        comment = new Comment(createdAt, value[(separatorIndex + 1)..]);
        return true;
    }

    private static string? ReadHeaderValue(string? line, string prefix) =>
        line is not null && line.StartsWith(prefix, StringComparison.Ordinal) ? line[prefix.Length..] : null;

    private static bool TryParseTimestamp(string value, out DateTimeOffset result) =>
        DateTimeOffset.TryParseExact(
            value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result);
}
