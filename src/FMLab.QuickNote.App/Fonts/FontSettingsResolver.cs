using Avalonia.Media;
using FMLab.QuickNote.Core.Settings;

namespace FMLab.QuickNote.App.Fonts;

/// <summary>
/// Combina o <see cref="FontSettings"/> salvo com as fontes monoespaçadas realmente instaladas
/// nesta máquina (via <see cref="FontFamilyResolver"/>) — nunca fica sem uma fonte monoespaçada
/// válida, mesmo quando nada foi salvo ainda ou a fonte salva não existe na plataforma atual
/// (ex.: config migrada de outro SO).
/// </summary>
public static class FontSettingsResolver
{
    public static (FontFamily FontFamily, double FontSize) Resolve(IFontSettingsStore store)
    {
        var settings = store.Load() ?? FontSettings.Default;
        var fontSize = settings.FontSize > 0 ? settings.FontSize : FontSettings.DefaultFontSize;
        var fontFamily = FontFamilyResolver.Resolve(settings.FontFamily, MonospaceFontCatalog.GetInstalledFontNames());

        return (new FontFamily(fontFamily), fontSize);
    }
}
