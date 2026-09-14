using Avalonia.Input;
using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.App.Shortcuts;

/// <summary>Compara um <see cref="KeyEventArgs"/> do Avalonia com um <see cref="KeyCombo"/> configurado.</summary>
public static class ShortcutMatcher
{
    public static bool Matches(KeyEventArgs e, KeyCombo combo) =>
        string.Equals(NormalizeKeyName(e.Key), combo.Key, System.StringComparison.OrdinalIgnoreCase)
        && ToShortcutModifiers(e.KeyModifiers) == combo.Modifiers;

    /// <summary>
    /// Avalonia reporta a tecla Enter principal como <see cref="Key.Return"/> (achado do teste
    /// manual da Fase 11 — "Concluir"/Ctrl+Enter nunca disparava porque o binding default usa
    /// "Enter" e a comparação nunca batia). Normaliza pro nome usado em <c>ShortcutDefaults</c>
    /// e na captura de tecla da tela de configurações, pra "Enter" e "Return" serem a mesma tecla.
    /// </summary>
    public static string NormalizeKeyName(Key key) => key == Key.Return ? "Enter" : key.ToString();

    public static ShortcutModifiers ToShortcutModifiers(KeyModifiers modifiers)
    {
        var result = ShortcutModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Control)) result |= ShortcutModifiers.Control;
        if (modifiers.HasFlag(KeyModifiers.Alt)) result |= ShortcutModifiers.Alt;
        if (modifiers.HasFlag(KeyModifiers.Shift)) result |= ShortcutModifiers.Shift;
        if (modifiers.HasFlag(KeyModifiers.Meta)) result |= ShortcutModifiers.Meta;
        return result;
    }
}
