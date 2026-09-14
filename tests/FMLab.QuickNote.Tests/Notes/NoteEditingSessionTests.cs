using FMLab.QuickNote.Core.Notes;

namespace FMLab.QuickNote.Tests.Notes;

public sealed class NoteEditingSessionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FMLab.QuickNote.Tests", Guid.NewGuid().ToString("N"));
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private FileNoteRepository CreateRepository() => new(_directory, () => _now);
    private NoteEditingSession CreateSession(out FileNoteRepository repository)
    {
        repository = CreateRepository();
        return new NoteEditingSession(repository);
    }

    [Fact]
    public void SaveIfNeeded_does_nothing_when_title_and_content_are_blank()
    {
        var session = CreateSession(out var repository);

        var saved = session.SaveIfNeeded(null, "   ");

        Assert.False(saved);
        Assert.Null(session.CurrentNoteId);
        Assert.Empty(repository.GetHistory());
    }

    [Fact]
    public void SaveIfNeeded_creates_note_on_first_save_with_content()
    {
        var session = CreateSession(out var repository);

        var saved = session.SaveIfNeeded("Titulo", "corpo");

        Assert.True(saved);
        Assert.NotNull(session.CurrentNoteId);
        var stored = repository.GetById(session.CurrentNoteId!.Value);
        Assert.Equal("Titulo", stored!.Title);
        Assert.Equal("corpo", stored.Content);
    }

    [Fact]
    public void SaveIfNeeded_creates_note_when_only_title_is_present()
    {
        var session = CreateSession(out var repository);

        session.SaveIfNeeded("Só titulo", "");

        Assert.Single(repository.GetHistory());
    }

    [Fact]
    public void SaveIfNeeded_updates_same_note_on_subsequent_calls()
    {
        var session = CreateSession(out var repository);
        session.SaveIfNeeded("Titulo", "primeira versao");
        var id = session.CurrentNoteId;

        session.SaveIfNeeded("Titulo", "segunda versao");

        Assert.Equal(id, session.CurrentNoteId);
        Assert.Single(repository.GetHistory());
        Assert.Equal("segunda versao", repository.GetById(id!.Value)!.Content);
    }

    [Fact]
    public void LoadBlank_makes_next_save_create_a_new_note()
    {
        var session = CreateSession(out var repository);
        session.SaveIfNeeded("Primeira", "conteudo 1");

        session.LoadBlank();
        session.SaveIfNeeded("Segunda", "conteudo 2");

        Assert.Equal(2, repository.GetHistory().Count);
    }

    [Fact]
    public void LoadPreviousDraft_returns_null_and_stays_blank_when_no_draft_exists()
    {
        var session = CreateSession(out _);

        var draft = session.LoadPreviousDraft();

        Assert.Null(draft);
        Assert.Null(session.CurrentNoteId);
    }

    [Fact]
    public void LoadPreviousDraft_loads_most_recently_updated_active_note()
    {
        var repository = CreateRepository();
        var older = repository.Create("Antiga", "a");
        _now = _now.AddMinutes(1);
        var newer = repository.Create("Recente", "b");
        var session = new NoteEditingSession(repository);

        var draft = session.LoadPreviousDraft();

        Assert.Equal(newer.Id, draft!.Id);
        Assert.Equal(newer.Id, session.CurrentNoteId);
        _ = older;
    }

    [Fact]
    public void LoadPreviousDraft_then_SaveIfNeeded_updates_the_loaded_draft()
    {
        var repository = CreateRepository();
        var existing = repository.Create("Rascunho", "conteudo original");
        var session = new NoteEditingSession(repository);
        session.LoadPreviousDraft();

        session.SaveIfNeeded("Rascunho", "conteudo editado");

        Assert.Single(repository.GetHistory());
        Assert.Equal("conteudo editado", repository.GetById(existing.Id)!.Content);
    }

    [Fact]
    public void CompleteCurrent_marks_note_completed_and_resets_session_to_blank()
    {
        var session = CreateSession(out var repository);
        session.SaveIfNeeded("Titulo", "corpo");
        var id = session.CurrentNoteId!.Value;

        session.CompleteCurrent("Titulo", "corpo");

        Assert.Null(session.CurrentNoteId);
        var completed = repository.GetById(id);
        Assert.NotNull(completed!.CompletedAt);
        Assert.False(completed.IsActive);
    }

    [Fact]
    public void CompleteCurrent_saves_unsaved_content_before_completing()
    {
        var session = CreateSession(out var repository);

        session.CompleteCurrent("Titulo", "nunca salva antes");

        var history = repository.GetHistory();
        Assert.Single(history);
        Assert.NotNull(history[0].CompletedAt);
    }

    [Fact]
    public void CompleteCurrent_with_nothing_to_save_and_no_current_note_is_a_noop()
    {
        var session = CreateSession(out var repository);

        session.CompleteCurrent(null, "");

        Assert.Empty(repository.GetHistory());
        Assert.Null(session.CurrentNoteId);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
