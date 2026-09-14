using FMLab.QuickNote.Core.Settings;

namespace FMLab.QuickNote.Tests.Settings;

public sealed class FontFamilyResolverTests
{
    [Fact]
    public void Resolve_returns_default_when_nothing_was_saved()
    {
        var resolved = FontFamilyResolver.Resolve(null, ["Consolas", "Cascadia Mono"]);

        Assert.Equal(FontSettings.DefaultFontFamily, resolved);
    }

    [Fact]
    public void Resolve_returns_default_when_saved_family_is_empty()
    {
        var resolved = FontFamilyResolver.Resolve(string.Empty, ["Consolas"]);

        Assert.Equal(FontSettings.DefaultFontFamily, resolved);
    }

    [Fact]
    public void Resolve_returns_saved_family_when_installed()
    {
        var resolved = FontFamilyResolver.Resolve("Cascadia Mono", ["Consolas", "Cascadia Mono"]);

        Assert.Equal("Cascadia Mono", resolved);
    }

    [Fact]
    public void Resolve_matches_saved_family_case_insensitively()
    {
        var resolved = FontFamilyResolver.Resolve("cascadia mono", ["Cascadia Mono"]);

        Assert.Equal("cascadia mono", resolved);
    }

    [Fact]
    public void Resolve_falls_back_to_default_when_saved_family_is_not_installed()
    {
        // Simula uma config migrada de outro SO: a fonte salva não existe na plataforma atual.
        var resolved = FontFamilyResolver.Resolve("Menlo", ["Consolas", "Cascadia Mono"]);

        Assert.Equal(FontSettings.DefaultFontFamily, resolved);
    }
}
