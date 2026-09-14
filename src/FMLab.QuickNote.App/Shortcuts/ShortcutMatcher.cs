using Avalonia.Input;
using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.App.Shortcuts;

/// <summary>Compara um <see cref="KeyEventArgs"/> do Avalonia com um <see cref="KeyCombo"/> configurado.</summary>
public static class ShortcutMatcher
{
    public static bool Matches(KeyEventArgs e, KeyCombo combo) =>
        string.Equals(e.Key.ToString(), combo.Key, System.StringComparison.OrdinalIgnoreCase)
        && ToShortcutModifiers(e.KeyModifiers) == combo.Modifiers;

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
