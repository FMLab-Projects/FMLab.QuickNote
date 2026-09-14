namespace FMLab.QuickNote.Core.Settings;

/// <summary>
/// Lógica pura de fallback: decide qual família de fonte usar dado o que foi salvo e o que
/// realmente está disponível na plataforma atual. Separado do <c>App</c> (que enumera as fontes
/// instaladas via Avalonia) para poder ser testado sem depender de UI.
/// </summary>
public static class FontFamilyResolver
{
    /// <summary>
    /// Retorna <paramref name="savedFontFamily"/> se ele estiver preenchido e presente em
    /// <paramref name="availableFontFamilies"/>; caso contrário cai no fallback monoespaçado
    /// default (<see cref="FontSettings.DefaultFontFamily"/>) — cobre tanto "nada foi salvo
    /// ainda" quanto "a fonte salva não existe nesta plataforma" (ex.: config migrada de outro SO).
    /// </summary>
    public static string Resolve(string? savedFontFamily, IReadOnlyCollection<string> availableFontFamilies)
    {
        if (string.IsNullOrWhiteSpace(savedFontFamily))
        {
            return FontSettings.DefaultFontFamily;
        }

        foreach (var available in availableFontFamilies)
        {
            if (string.Equals(available, savedFontFamily, StringComparison.OrdinalIgnoreCase))
            {
                return savedFontFamily;
            }
        }

        return FontSettings.DefaultFontFamily;
    }
}
