namespace FMLab.QuickNote.Core.Shortcuts;

/// <summary>
/// Combinação de teclas independente de framework de UI (ex: "Ctrl+Alt+N", "Escape"). O nome da
/// tecla é o texto canônico usado tanto na config quanto no mapeamento feito pela camada de UI
/// (Avalonia `Key`/SharpHook `KeyCode`) — ver <see cref="TryParse"/> para o formato aceito.
/// </summary>
public readonly record struct KeyCombo(ShortcutModifiers Modifiers, string Key)
{
    public override string ToString()
    {
        var parts = new List<string>(4);
        if (Modifiers.HasFlag(ShortcutModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ShortcutModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ShortcutModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ShortcutModifiers.Meta)) parts.Add("Meta");
        parts.Add(Key);
        return string.Join('+', parts);
    }

    /// <summary>Aceita "Mod1+Mod2+...+Tecla" (ex.: "Ctrl+Alt+N") ou apenas "Tecla" (ex.: "Escape").</summary>
    public static bool TryParse(string? text, out KeyCombo combo)
    {
        combo = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var modifiers = ShortcutModifiers.None;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= ShortcutModifiers.Control;
                    break;
                case "alt":
                    modifiers |= ShortcutModifiers.Alt;
                    break;
                case "shift":
                    modifiers |= ShortcutModifiers.Shift;
                    break;
                case "meta":
                case "cmd":
                case "win":
                    modifiers |= ShortcutModifiers.Meta;
                    break;
                default:
                    return false;
            }
        }

        var key = parts[^1];
        if (key.Length == 0)
        {
            return false;
        }

        combo = new KeyCombo(modifiers, key);
        return true;
    }
}
