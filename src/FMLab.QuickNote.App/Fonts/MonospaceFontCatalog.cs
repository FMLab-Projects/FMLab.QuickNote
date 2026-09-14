using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace FMLab.QuickNote.App.Fonts;

/// <summary>
/// Lista as fontes monoespaçadas instaladas no sistema. Avalonia não expõe uma flag
/// "IsFixedPitch" de forma uniforme entre plataformas, então a heurística compara a largura do
/// glyph de um caractere estreito ("i") com a de um largo ("W") no mesmo typeface: em fontes de
/// largura fixa (monoespaçadas) as duas são iguais.
/// </summary>
public static class MonospaceFontCatalog
{
    public static IReadOnlyList<string> GetInstalledFontNames() => FontManager.Current.SystemFonts
        .Select(family => family.Name)
        .Where(IsFixedPitch)
        .Distinct()
        .OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static bool IsFixedPitch(string fontFamilyName)
    {
        var typeface = new Typeface(fontFamilyName);
        if (!FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface) || glyphTypeface is null)
        {
            return false;
        }

        var characterToGlyphMap = glyphTypeface.CharacterToGlyphMap;
        if (!characterToGlyphMap.TryGetGlyph('i', out var narrowGlyph)
            || !characterToGlyphMap.TryGetGlyph('W', out var wideGlyph)
            || !glyphTypeface.TryGetHorizontalGlyphAdvance(narrowGlyph, out var narrowAdvance)
            || !glyphTypeface.TryGetHorizontalGlyphAdvance(wideGlyph, out var wideAdvance))
        {
            return false;
        }

        return narrowAdvance == wideAdvance;
    }
}
