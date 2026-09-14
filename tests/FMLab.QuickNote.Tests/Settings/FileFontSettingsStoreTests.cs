using FMLab.QuickNote.Core.Settings;

namespace FMLab.QuickNote.Tests.Settings;

public sealed class FileFontSettingsStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), "FMLab.QuickNote.Tests", $"{Guid.NewGuid():N}.font.txt");

    private FileFontSettingsStore CreateStore() => new(_path);

    [Fact]
    public void Load_returns_null_when_nothing_was_saved_yet()
    {
        var store = CreateStore();

        Assert.Null(store.Load());
    }

    [Fact]
    public void Save_then_load_round_trips_font_family_and_size()
    {
        var store = CreateStore();
        var settings = new FontSettings { FontFamily = "Cascadia Mono", FontSize = 16 };

        store.Save(settings);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Equal("Cascadia Mono", loaded!.FontFamily);
        Assert.Equal(16, loaded.FontSize);
    }

    [Fact]
    public void Save_then_load_round_trips_null_font_family_as_default_fallback()
    {
        var store = CreateStore();
        var settings = new FontSettings { FontFamily = null, FontSize = 13 };

        store.Save(settings);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Null(loaded!.FontFamily);
        Assert.Equal(13, loaded.FontSize);
    }

    [Fact]
    public void Load_returns_null_for_unreadable_content()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "not a font settings file");

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
