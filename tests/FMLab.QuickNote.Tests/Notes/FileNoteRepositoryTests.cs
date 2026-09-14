using FMLab.QuickNote.Core.Notes;

namespace FMLab.QuickNote.Tests.Notes;

public sealed class FileNoteRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FMLab.QuickNote.Tests", Guid.NewGuid().ToString("N"));
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private FileNoteRepository CreateRepository() => new(_directory, () => _now);

    [Fact]
    public void Create_persists_note_retrievable_by_id()
    {
        var repository = CreateRepository();

        var created = repository.Create("Title", "Line one\nLine two");

        var loaded = repository.GetById(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Title", loaded!.Title);
        Assert.Equal("Line one\nLine two", loaded.Content);
        Assert.Equal(created.CreatedAt, loaded.CreatedAt);
        Assert.Equal(created.UpdatedAt, loaded.UpdatedAt);
        Assert.Null(loaded.CompletedAt);
        Assert.True(loaded.IsActive);
    }

    [Fact]
    public void Create_without_title_round_trips_as_null()
    {
        var repository = CreateRepository();

        var created = repository.Create(null, "content");

        Assert.Null(repository.GetById(created.Id)!.Title);
    }

    [Fact]
    public void GetById_returns_null_when_note_does_not_exist()
    {
        var repository = CreateRepository();

        Assert.Null(repository.GetById(Guid.NewGuid()));
    }

    [Fact]
    public void Update_persists_changes_and_bumps_updated_at()
    {
        var repository = CreateRepository();
        var note = repository.Create("Title", "original");

        _now = _now.AddMinutes(5);
        note.Content = "changed";
        repository.Update(note);

        var loaded = repository.GetById(note.Id)!;
        Assert.Equal("changed", loaded.Content);
        Assert.Equal(_now, loaded.UpdatedAt);
    }

    [Fact]
    public void GetActiveDraft_returns_most_recently_updated_active_note()
    {
        var repository = CreateRepository();
        var older = repository.Create(null, "older");
        _now = _now.AddMinutes(1);
        var newer = repository.Create(null, "newer");

        var draft = repository.GetActiveDraft();

        Assert.NotNull(draft);
        Assert.Equal(newer.Id, draft!.Id);
        _ = older;
    }

    [Fact]
    public void GetActiveDraft_ignores_completed_notes()
    {
        var repository = CreateRepository();
        var active = repository.Create(null, "active");
        _now = _now.AddMinutes(1);
        var completed = repository.Create(null, "completed");
        repository.Complete(completed.Id);

        var draft = repository.GetActiveDraft();

        Assert.NotNull(draft);
        Assert.Equal(active.Id, draft!.Id);
    }

    [Fact]
    public void GetActiveDraft_returns_null_when_no_active_notes_exist()
    {
        var repository = CreateRepository();

        Assert.Null(repository.GetActiveDraft());
    }

    [Fact]
    public void Complete_marks_note_completed_and_stamps_completed_at()
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");

        _now = _now.AddMinutes(1);
        repository.Complete(note.Id);

        var loaded = repository.GetById(note.Id)!;
        Assert.Equal(_now, loaded.CompletedAt);
        Assert.False(loaded.IsActive);
    }

    [Fact]
    public void Complete_throws_when_note_does_not_exist()
    {
        var repository = CreateRepository();

        Assert.Throws<InvalidOperationException>(() => repository.Complete(Guid.NewGuid()));
    }

    [Fact]
    public void GetHistory_returns_active_and_completed_notes_ordered_by_updated_at_descending()
    {
        var repository = CreateRepository();
        var first = repository.Create(null, "first");
        _now = _now.AddMinutes(1);
        var second = repository.Create(null, "second");
        repository.Complete(second.Id);
        _now = _now.AddMinutes(1);
        var third = repository.Create(null, "third");

        var history = repository.GetHistory();

        Assert.Equal(new[] { third.Id, second.Id, first.Id }, history.Select(n => n.Id));
    }

    [Fact]
    public void Reactivate_clears_completed_at_and_note_becomes_active_again()
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");
        repository.Complete(note.Id);

        _now = _now.AddMinutes(1);
        repository.Reactivate(note.Id);

        var loaded = repository.GetById(note.Id)!;
        Assert.Null(loaded.CompletedAt);
        Assert.True(loaded.IsActive);
        Assert.Equal(_now, loaded.UpdatedAt);
    }

    [Fact]
    public void Reactivate_throws_when_note_does_not_exist()
    {
        var repository = CreateRepository();

        Assert.Throws<InvalidOperationException>(() => repository.Reactivate(Guid.NewGuid()));
    }

    [Fact]
    public void New_note_round_trips_with_zero_comments()
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");

        Assert.Empty(repository.GetById(note.Id)!.Comments);
    }

    [Fact]
    public void AddComment_persists_and_round_trips_a_single_comment()
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");

        _now = _now.AddMinutes(1);
        repository.AddComment(note.Id, "primeiro comentário");

        var loaded = repository.GetById(note.Id)!;
        Assert.Single(loaded.Comments);
        Assert.Equal("primeiro comentário", loaded.Comments[0].Text);
        Assert.Equal(_now, loaded.Comments[0].CreatedAt);
    }

    [Fact]
    public void AddComment_trims_surrounding_whitespace()
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");

        repository.AddComment(note.Id, "  com espaços  ");

        Assert.Equal("com espaços", repository.GetById(note.Id)!.Comments[0].Text);
    }

    [Fact]
    public void AddComment_round_trips_several_comments_ordered_by_created_at()
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");

        repository.AddComment(note.Id, "primeiro");
        _now = _now.AddMinutes(1);
        repository.AddComment(note.Id, "segundo");
        _now = _now.AddMinutes(1);
        repository.AddComment(note.Id, "terceiro");

        var loaded = repository.GetById(note.Id)!;
        Assert.Equal(["primeiro", "segundo", "terceiro"], loaded.Comments.Select(c => c.Text));
        Assert.True(loaded.Comments[0].CreatedAt < loaded.Comments[1].CreatedAt);
        Assert.True(loaded.Comments[1].CreatedAt < loaded.Comments[2].CreatedAt);
    }

    [Fact]
    public void AddComment_updates_note_updated_at()
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");

        _now = _now.AddMinutes(1);
        repository.AddComment(note.Id, "comentário");

        Assert.Equal(_now, repository.GetById(note.Id)!.UpdatedAt);
    }

    [Fact]
    public void AddComment_throws_when_note_does_not_exist()
    {
        var repository = CreateRepository();

        Assert.Throws<InvalidOperationException>(() => repository.AddComment(Guid.NewGuid(), "x"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddComment_rejects_empty_or_whitespace_only_text(string text)
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");

        Assert.Throws<ArgumentException>(() => repository.AddComment(note.Id, text));
        Assert.Empty(repository.GetById(note.Id)!.Comments);
    }

    [Fact]
    public void AddComment_rejects_text_with_line_break()
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");

        Assert.Throws<ArgumentException>(() => repository.AddComment(note.Id, "linha um\nlinha dois"));
        Assert.Empty(repository.GetById(note.Id)!.Comments);
    }

    [Fact]
    public void AddComment_rejects_text_above_max_length()
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");
        var tooLong = new string('x', Comment.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => repository.AddComment(note.Id, tooLong));
        Assert.Empty(repository.GetById(note.Id)!.Comments);
    }

    [Fact]
    public void AddComment_accepts_text_at_exactly_max_length()
    {
        var repository = CreateRepository();
        var note = repository.Create(null, "content");
        var exact = new string('x', Comment.MaxLength);

        repository.AddComment(note.Id, exact);

        Assert.Equal(exact, repository.GetById(note.Id)!.Comments[0].Text);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
