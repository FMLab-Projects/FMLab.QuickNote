using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.Tests.Shortcuts;

public sealed class KeyComboTests
{
    [Theory]
    [InlineData("Ctrl+Alt+N", ShortcutModifiers.Control | ShortcutModifiers.Alt, "N")]
    [InlineData("ctrl+alt+n", ShortcutModifiers.Control | ShortcutModifiers.Alt, "n")]
    [InlineData("Escape", ShortcutModifiers.None, "Escape")]
    [InlineData("Ctrl+Enter", ShortcutModifiers.Control, "Enter")]
    [InlineData("Ctrl+Shift+C", ShortcutModifiers.Control | ShortcutModifiers.Shift, "C")]
    public void TryParse_reads_modifiers_and_key(string text, ShortcutModifiers expectedModifiers, string expectedKey)
    {
        var parsed = KeyCombo.TryParse(text, out var combo);

        Assert.True(parsed);
        Assert.Equal(expectedModifiers, combo.Modifiers);
        Assert.Equal(expectedKey, combo.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+Bogus+N")]
    [InlineData("+")]
    public void TryParse_rejects_invalid_input(string? text)
    {
        Assert.False(KeyCombo.TryParse(text, out _));
    }

    [Fact]
    public void ToString_roundtrips_through_TryParse()
    {
        var original = new KeyCombo(ShortcutModifiers.Control | ShortcutModifiers.Alt | ShortcutModifiers.Shift, "N");

        var text = original.ToString();
        var parsed = KeyCombo.TryParse(text, out var roundtripped);

        Assert.True(parsed);
        Assert.Equal(original, roundtripped);
    }

    [Fact]
    public void ToString_orders_modifiers_consistently()
    {
        var combo = new KeyCombo(ShortcutModifiers.Shift | ShortcutModifiers.Control | ShortcutModifiers.Alt, "N");

        Assert.Equal("Ctrl+Alt+Shift+N", combo.ToString());
    }
}
