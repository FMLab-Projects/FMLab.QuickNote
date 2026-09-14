using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.Tests.Shortcuts;

public sealed class FileShortcutBindingsStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), "FMLab.QuickNote.Tests", $"{Guid.NewGuid():N}.shortcuts.txt");

    private FileShortcutBindingsStore CreateStore() => new(_path);

    [Fact]
    public void Load_returns_null_when_nothing_was_saved_yet()
    {
        Assert.Null(CreateStore().Load());
    }

    [Fact]
    public void Save_then_Load_roundtrips_bindings()
    {
        var store = CreateStore();
        var bindings = new[]
        {
            new ShortcutBinding(ShortcutAction.NewNote, new KeyCombo(ShortcutModifiers.Control | ShortcutModifiers.Alt, "N")),
            new ShortcutBinding(ShortcutAction.Close, new KeyCombo(ShortcutModifiers.None, "Escape")),
        };

        store.Save(bindings);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Equal(bindings, loaded);
    }

    [Fact]
    public void Load_skips_lines_with_unknown_action_or_invalid_combo()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllLines(_path,
        [
            "NewNote: Ctrl+Alt+N",
            "NotAnAction: Ctrl+X",
            "Close: Ctrl+Bogus+Y",
            "",
        ]);

        var loaded = CreateStore().Load();

        Assert.NotNull(loaded);
        var binding = Assert.Single(loaded!);
        Assert.Equal(ShortcutAction.NewNote, binding.Action);
    }

    [Fact]
    public void Load_returns_null_when_every_line_is_invalid()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllLines(_path, ["garbage", "NotAnAction: Ctrl+X"]);

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
