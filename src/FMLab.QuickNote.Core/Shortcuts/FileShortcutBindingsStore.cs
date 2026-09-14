namespace FMLab.QuickNote.Core.Shortcuts;

/// <summary>
/// Persiste bindings customizados como um arquivo de texto simples, uma linha por ação
/// ("Ação: Combinação", ex.: "NewNote: Ctrl+Alt+N") — mesmo estilo "Key: value" usado pelos
/// outros stores do projeto (ver <see cref="Core.Settings.FileWindowPlacementStore"/>).
/// Linhas com ação desconhecida ou combinação inválida são ignoradas (fallback para o default
/// dessa ação específica); se nenhuma linha for válida, <see cref="Load"/> retorna null.
/// </summary>
public sealed class FileShortcutBindingsStore : IShortcutBindingsStore
{
    private readonly string _path;

    public FileShortcutBindingsStore(string path) => _path = path;

    public static string GetDefaultPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FMLab.QuickNote",
        "shortcuts.txt");

    public IReadOnlyList<ShortcutBinding>? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        var bindings = new List<ShortcutBinding>();
        foreach (var line in File.ReadAllLines(_path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex < 0)
            {
                continue;
            }

            var actionText = line[..separatorIndex].Trim();
            var comboText = line[(separatorIndex + 1)..].Trim();

            if (!Enum.TryParse<ShortcutAction>(actionText, ignoreCase: true, out var action)
                || !KeyCombo.TryParse(comboText, out var combo))
            {
                continue;
            }

            bindings.Add(new ShortcutBinding(action, combo));
        }

        return bindings.Count > 0 ? bindings : null;
    }

    public void Save(IReadOnlyList<ShortcutBinding> bindings)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = bindings.Select(b => $"{b.Action}: {b.KeyCombo}");
        File.WriteAllLines(_path, lines);
    }
}
