using FMLab.QuickNote.Core.Notes;

namespace FMLab.QuickNote.Tests.Notes;

public sealed class NoteSearchTests
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
    public void Empty_query_returns_all_notes_without_reordering()
    {
        var first = MakeNote("Zebra", "zzz");
        var second = MakeNote("Abacate", "aaa");
        var notes = new[] { first, second };

        var result = NoteSearch.Search(notes, string.Empty);

        Assert.Equal(notes, result);
    }

    [Fact]
    public void Whitespace_query_returns_all_notes()
    {
        var notes = new[] { MakeNote("Nota", "conteúdo") };

        var result = NoteSearch.Search(notes, "   ");

        Assert.Equal(notes, result);
    }

    [Fact]
    public void Exact_match_on_title_is_found()
    {
        var target = MakeNote("Lista de compras", "leite, ovos");
        var other = MakeNote("Ideias", "brainstorm");

        var result = NoteSearch.Search([target, other], "compras");

        Assert.Equal([target], result);
    }

    [Fact]
    public void Exact_match_on_content_is_found()
    {
        var target = MakeNote(null, "lembrar de comprar leite");
        var other = MakeNote(null, "reunião às 10h");

        var result = NoteSearch.Search([target, other], "leite");

        Assert.Equal([target], result);
    }

    [Fact]
    public void Approximate_match_tolerates_one_or_two_character_typo()
    {
        var target = MakeNote(null, "o rato roeu a roupa do rei de roma");

        var result = NoteSearch.Search([target], "roupa");
        Assert.Equal([target], result);

        var resultWithTypo = NoteSearch.Search([target], "ropua"); // 2 chars trocados
        Assert.Equal([target], resultWithTypo);
    }

    [Fact]
    public void Search_ignores_accentuation_differences()
    {
        var target = MakeNote("Reunião", "discutir pauta");

        var result = NoteSearch.Search([target], "reuniao");

        Assert.Equal([target], result);
    }

    [Fact]
    public void Search_ignores_accentuation_when_query_has_accents_and_note_does_not()
    {
        var target = MakeNote("Reuniao", "discutir pauta");

        var result = NoteSearch.Search([target], "reunião");

        Assert.Equal([target], result);
    }

    [Fact]
    public void No_results_when_nothing_matches()
    {
        var notes = new[] { MakeNote("Lista de compras", "leite, ovos") };

        var result = NoteSearch.Search(notes, "xilofone completamente diferente");

        Assert.Empty(result);
    }

    [Fact]
    public void Results_are_ordered_by_relevance_strongest_first()
    {
        var exact = MakeNote("roupa", "x");
        var approximate = MakeNote("ropua", "x"); // typo de 2 posições trocadas

        var result = NoteSearch.Search([approximate, exact], "roupa");

        Assert.Equal([exact, approximate], result);
    }
}
