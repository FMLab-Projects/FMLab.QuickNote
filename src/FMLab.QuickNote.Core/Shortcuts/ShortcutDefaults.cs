namespace FMLab.QuickNote.Core.Shortcuts;

/// <summary>Defaults da seção 4 do TASKS.md, usados quando não há binding customizado válido.</summary>
public static class ShortcutDefaults
{
    public static IReadOnlyList<ShortcutBinding> All { get; } =
    [
        new ShortcutBinding(ShortcutAction.NewNote, new KeyCombo(ShortcutModifiers.Control | ShortcutModifiers.Alt, "N")),
        new ShortcutBinding(ShortcutAction.EditDraft, new KeyCombo(ShortcutModifiers.Control | ShortcutModifiers.Alt, "Q")),
        new ShortcutBinding(ShortcutAction.Close, new KeyCombo(ShortcutModifiers.None, "Escape")),
        new ShortcutBinding(ShortcutAction.Complete, new KeyCombo(ShortcutModifiers.Control, "Enter")),
        new ShortcutBinding(ShortcutAction.OpenHistory, new KeyCombo(ShortcutModifiers.Control | ShortcutModifiers.Alt, "H")),
        new ShortcutBinding(ShortcutAction.ToggleCheckbox, new KeyCombo(ShortcutModifiers.Control | ShortcutModifiers.Shift, "C")),
    ];

    /// <summary>
    /// Ações disparadas pelo hook global de teclado (funcionam mesmo com a janela escondida);
    /// as demais só são tratadas localmente, com a janela em foco.
    /// </summary>
    public static bool IsGlobal(ShortcutAction action) => action
        is ShortcutAction.NewNote or ShortcutAction.EditDraft or ShortcutAction.OpenHistory;
}
