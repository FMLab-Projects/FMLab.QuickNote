namespace FMLab.QuickNote.Core.Settings;

/// <summary>Fonte usada no editor (título + corpo) e na prévia do histórico.</summary>
public sealed class FontSettings
{
    /// <summary>Nula/vazia = usar o fallback monoespaçado default da plataforma.</summary>
    public string? FontFamily { get; set; }

    public required double FontSize { get; set; }

    /// <summary>
    /// Lista de fallback (nomes separados por vírgula) usada quando <see cref="FontFamily"/> não
    /// está definido, ou quando a fonte salva não existe na plataforma atual — cada SO resolve o
    /// primeiro nome que reconhecer.
    /// </summary>
    public const string DefaultFontFamily = "Cascadia Mono,Consolas,Menlo,Monospace";

    public const double DefaultFontSize = 13;

    public static FontSettings Default => new() { FontFamily = null, FontSize = DefaultFontSize };
}
