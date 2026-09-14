using System.Globalization;

namespace FMLab.QuickNote.Core.Settings;

/// <summary>
/// Persists <see cref="FontSettings"/> as a small "Key: value" text file, same style as
/// <see cref="FileWindowPlacementStore"/>/<see cref="Shortcuts.FileShortcutBindingsStore"/> —
/// auditable/editable by hand, no extra dependency.
/// </summary>
public sealed class FileFontSettingsStore : IFontSettingsStore
{
    private readonly string _path;

    public FileFontSettingsStore(string path) => _path = path;

    public static string GetDefaultPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FMLab.QuickNote",
        "font.txt");

    public FontSettings? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        using var reader = new StringReader(File.ReadAllText(_path));

        var fontFamily = ReadHeaderValue(reader.ReadLine(), "FontFamily: ");
        var fontSizeText = ReadHeaderValue(reader.ReadLine(), "FontSize: ");

        if (!double.TryParse(fontSizeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var fontSize))
        {
            return null;
        }

        return new FontSettings
        {
            FontFamily = string.IsNullOrEmpty(fontFamily) ? null : fontFamily,
            FontSize = fontSize,
        };
    }

    public void Save(FontSettings settings)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var text = string.Join('\n',
            $"FontFamily: {settings.FontFamily ?? string.Empty}",
            $"FontSize: {settings.FontSize.ToString(CultureInfo.InvariantCulture)}");
        File.WriteAllText(_path, text);
    }

    private static string? ReadHeaderValue(string? line, string prefix) =>
        line is not null && line.StartsWith(prefix, StringComparison.Ordinal) ? line[prefix.Length..] : null;
}
