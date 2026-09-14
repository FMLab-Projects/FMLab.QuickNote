using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.App;

/// <summary>Linha de exibição de um atalho na tela de configurações.</summary>
public sealed class ShortcutRow(ShortcutAction action, string comboText)
{
    public ShortcutAction Action { get; } = action;

    public string ActionLabel { get; } = Describe(action);

    public string ComboText { get; } = comboText;

    public bool HasConflict { get; init; }

    public string ConflictMessage => "Conflito com outro atalho";

    /// <summary>True para atalhos globais (funcionam com a janela escondida, ver <see cref="ShortcutDefaults.IsGlobal"/>).</summary>
    public bool IsGlobal { get; init; }

    public string ScopeLabel => IsGlobal ? "Global" : "Nesta janela";

    private static string Describe(ShortcutAction action) => action switch
    {
        ShortcutAction.NewNote => "Nova nota",
        ShortcutAction.EditDraft => "Editar rascunho anterior",
        ShortcutAction.Close => "Fechar janela",
        ShortcutAction.Complete => "Concluir nota atual",
        ShortcutAction.OpenHistory => "Abrir histórico",
        ShortcutAction.ToggleCheckbox => "Alternar checkbox",
        _ => action.ToString(),
    };
}
