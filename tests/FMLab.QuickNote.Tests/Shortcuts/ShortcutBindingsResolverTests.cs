using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.Tests.Shortcuts;

public sealed class ShortcutBindingsResolverTests
{
    private sealed class FakeStore(IReadOnlyList<ShortcutBinding>? toReturn) : IShortcutBindingsStore
    {
        public IReadOnlyList<ShortcutBinding>? Load() => toReturn;
        public void Save(IReadOnlyList<ShortcutBinding> bindings) => throw new NotSupportedException();
    }

    [Fact]
    public void Resolve_returns_defaults_when_store_has_nothing_saved()
    {
        var resolved = ShortcutBindingsResolver.Resolve(new FakeStore(null));

        foreach (var defaultBinding in ShortcutDefaults.All)
        {
            Assert.Equal(defaultBinding.KeyCombo, resolved[defaultBinding.Action]);
        }
    }

    [Fact]
    public void Resolve_overrides_only_the_actions_present_in_the_custom_bindings()
    {
        var custom = new[]
        {
            new ShortcutBinding(ShortcutAction.NewNote, new KeyCombo(ShortcutModifiers.Meta, "Space")),
        };

        var resolved = ShortcutBindingsResolver.Resolve(new FakeStore(custom));

        Assert.Equal(new KeyCombo(ShortcutModifiers.Meta, "Space"), resolved[ShortcutAction.NewNote]);
        var defaultEditDraft = ShortcutDefaults.All.Single(b => b.Action == ShortcutAction.EditDraft).KeyCombo;
        Assert.Equal(defaultEditDraft, resolved[ShortcutAction.EditDraft]);
    }

    [Fact]
    public void Resolve_covers_every_known_action()
    {
        var resolved = ShortcutBindingsResolver.Resolve(new FakeStore(null));

        foreach (ShortcutAction action in Enum.GetValues<ShortcutAction>())
        {
            Assert.True(resolved.ContainsKey(action));
        }
    }
}
