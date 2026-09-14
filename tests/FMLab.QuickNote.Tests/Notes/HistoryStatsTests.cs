using FMLab.QuickNote.Core.Notes;

namespace FMLab.QuickNote.Tests.Notes;

public sealed class HistoryStatsTests
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
    public void From_empty_list_returns_all_zeros()
    {
        var stats = HistoryStats.From([]);

        Assert.Equal(0, stats.Total);
        Assert.Equal(0, stats.Active);
        Assert.Equal(0, stats.Completed);
    }

    [Fact]
    public void From_counts_active_and_completed_separately()
    {
        var notes = new[] { MakeNote(active: true), MakeNote(active: true), MakeNote(active: false) };

        var stats = HistoryStats.From(notes);

        Assert.Equal(3, stats.Total);
        Assert.Equal(2, stats.Active);
        Assert.Equal(1, stats.Completed);
    }

    [Fact]
    public void ToString_formats_as_expected()
    {
        var stats = new HistoryStats(3, 2, 1);

        Assert.Equal("3 notas · 2 ativas · 1 concluídas", stats.ToString());
    }

    [Fact]
    public void ToString_handles_zero_notes()
    {
        var stats = HistoryStats.From([]);

        Assert.Equal("0 notas · 0 ativas · 0 concluídas", stats.ToString());
    }
}
