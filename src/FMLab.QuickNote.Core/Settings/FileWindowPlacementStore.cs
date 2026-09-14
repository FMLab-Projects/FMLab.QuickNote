using System.Globalization;

namespace FMLab.QuickNote.Core.Settings;

/// <summary>
/// Persists <see cref="WindowPlacement"/> as a small "Key: value" text file, same style as
/// <see cref="Core.Notes.FileNoteRepository"/> — auditable/editable by hand, no extra dependency.
/// </summary>
public sealed class FileWindowPlacementStore : IWindowPlacementStore
{
    private readonly string _path;

    public FileWindowPlacementStore(string path) => _path = path;

    public static string GetDefaultPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FMLab.QuickNote",
        "window-placement.txt");

    public WindowPlacement? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        using var reader = new StringReader(File.ReadAllText(_path));

        var x = ReadHeaderValue(reader.ReadLine(), "X: ");
        var y = ReadHeaderValue(reader.ReadLine(), "Y: ");
        var widthText = ReadHeaderValue(reader.ReadLine(), "Width: ");
        var heightText = ReadHeaderValue(reader.ReadLine(), "Height: ");

        if (!TryParseDouble(widthText, out var width) || !TryParseDouble(heightText, out var height))
        {
            return null;
        }

        return new WindowPlacement
        {
            X = TryParseDouble(x, out var parsedX) ? parsedX : null,
            Y = TryParseDouble(y, out var parsedY) ? parsedY : null,
            Width = width,
            Height = height,
        };
    }

    public void Save(WindowPlacement placement)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var text = string.Join('\n',
            $"X: {Format(placement.X)}",
            $"Y: {Format(placement.Y)}",
            $"Width: {Format(placement.Width)}",
            $"Height: {Format(placement.Height)}");
        File.WriteAllText(_path, text);
    }

    private static string Format(double? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static string? ReadHeaderValue(string? line, string prefix) =>
        line is not null && line.StartsWith(prefix, StringComparison.Ordinal) ? line[prefix.Length..] : null;

    private static bool TryParseDouble(string? value, out double result)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = default;
            return false;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
