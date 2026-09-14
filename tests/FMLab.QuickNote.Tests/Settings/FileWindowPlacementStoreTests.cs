using FMLab.QuickNote.Core.Settings;

namespace FMLab.QuickNote.Tests.Settings;

public sealed class FileWindowPlacementStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), "FMLab.QuickNote.Tests", $"{Guid.NewGuid():N}.window-placement.txt");

    private FileWindowPlacementStore CreateStore() => new(_path);

    [Fact]
    public void Load_returns_null_when_nothing_was_saved_yet()
    {
        var store = CreateStore();

        Assert.Null(store.Load());
    }

    [Fact]
    public void Save_then_load_round_trips_position_and_size()
    {
        var store = CreateStore();
        var placement = new WindowPlacement { X = 123.5, Y = -10, Width = 400, Height = 520 };

        store.Save(placement);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Equal(123.5, loaded!.X);
        Assert.Equal(-10, loaded.Y);
        Assert.Equal(400, loaded.Width);
        Assert.Equal(520, loaded.Height);
    }

    [Fact]
    public void Save_then_load_round_trips_null_position()
    {
        var store = CreateStore();
        var placement = new WindowPlacement { X = null, Y = null, Width = 380, Height = 480 };

        store.Save(placement);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Null(loaded!.X);
        Assert.Null(loaded.Y);
    }

    [Fact]
    public void Load_returns_null_for_unreadable_content()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "not a placement file");

        Assert.Null(CreateStore().Load());
    }

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
