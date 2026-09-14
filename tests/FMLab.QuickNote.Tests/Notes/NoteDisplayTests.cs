using FMLab.QuickNote.Core.Notes;

namespace FMLab.QuickNote.Tests.Notes;

public sealed class NoteDisplayTests
{
    private static Note MakeNote(string? title, string content) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Content = content,
        CreatedAt = DateTimeOffset.UnixEpoch,
        UpdatedAt = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public void Uses_title_when_present()
    {
        var note = MakeNote("Meu titulo", "corpo qualquer");

        Assert.Equal("Meu titulo", NoteDisplay.GetDisplayTitle(note));
    }

    [Fact]
    public void Falls_back_to_first_non_empty_line_of_content_when_title_is_absent()
    {
        var note = MakeNote(null, "\n\nprimeira linha real\nsegunda linha");

        Assert.Equal("primeira linha real", NoteDisplay.GetDisplayTitle(note));
    }

    [Fact]
    public void Falls_back_to_empty_string_when_title_and_content_are_blank()
    {
        var note = MakeNote(null, "   \n  ");

        Assert.Equal(string.Empty, NoteDisplay.GetDisplayTitle(note));
    }

    [Fact]
    public void Truncates_long_preview_with_ellipsis()
    {
        var note = MakeNote(null, new string('a', 80));

        var preview = NoteDisplay.GetDisplayTitle(note, maxLength: 10);

        Assert.Equal(new string('a', 10) + "…", preview);
    }
}
