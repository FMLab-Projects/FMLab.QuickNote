using FMLab.QuickNote.Core.Notes;

namespace FMLab.QuickNote.Tests.Notes;

public sealed class HistoryFilterTests
{
    private static Note MakeNote(bool active) => new()
    {
        Id = Guid.NewGuid(),
        Content = "x",
        CreatedAt = DateTimeOffset.UnixEpoch,
        UpdatedAt = DateTimeOffset.UnixEpoch,
        CompletedAt = active ? null : DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public void All_returns_every_note()
    {
        var notes = new[] { MakeNote(active: true), MakeNote(active: false) };

        Assert.Equal(2, HistoryFilter.All.Apply(notes).Count);
    }

    [Fact]
    public void Active_returns_only_notes_without_CompletedAt()
    {
        var active = MakeNote(active: true);
        var completed = MakeNote(active: false);

        var result = HistoryFilter.Active.Apply([active, completed]);

        Assert.Equal([active], result);
    }

    [Fact]
    public void Completed_returns_only_notes_with_CompletedAt()
    {
        var active = MakeNote(active: true);
        var completed = MakeNote(active: false);

        var result = HistoryFilter.Completed.Apply([active, completed]);

        Assert.Equal([completed], result);
    }
}
