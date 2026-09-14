using FMLab.QuickNote.Core.Ipc;

namespace FMLab.QuickNote.Tests.Ipc;

public sealed class AppCommandNamesTests
{
    [Theory]
    [InlineData(AppCommand.NewNote, "new-note")]
    [InlineData(AppCommand.EditDraft, "edit-draft")]
    [InlineData(AppCommand.OpenHistory, "open-history")]
    [InlineData(AppCommand.Toggle, "toggle")]
    public void ToWireName_and_TryParse_round_trip(AppCommand command, string wireName)
    {
        Assert.Equal(wireName, AppCommandNames.ToWireName(command));

        Assert.True(AppCommandNames.TryParse(wireName, out var parsed));
        Assert.Equal(command, parsed);
    }

    [Fact]
    public void TryParse_returns_false_for_unknown_text()
    {
        Assert.False(AppCommandNames.TryParse("bogus", out _));
    }

    [Fact]
    public void TryParse_returns_false_for_null()
    {
        Assert.False(AppCommandNames.TryParse(null, out _));
    }
}
