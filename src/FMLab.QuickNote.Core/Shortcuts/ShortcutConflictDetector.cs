namespace FMLab.QuickNote.Core.Shortcuts;

/// <summary>
/// Detecta atalhos com a mesma combinação de teclas dentro do mesmo escopo: dois atalhos
/// globais colidem entre si, dois locais colidem entre si, mas um global e um local não —
/// disparam em contextos diferentes (janela escondida vs. em foco) e podem compartilhar tecla.
/// </summary>
public static class ShortcutConflictDetector
{
    public sealed record Conflict(ShortcutAction First, ShortcutAction Second, KeyCombo KeyCombo);

    public static IReadOnlyList<Conflict> FindConflicts(IEnumerable<ShortcutBinding> bindings)
    {
        var list = bindings.ToList();
        var conflicts = new List<Conflict>();

        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                var a = list[i];
                var b = list[j];

                if (a.KeyCombo == b.KeyCombo && ShortcutDefaults.IsGlobal(a.Action) == ShortcutDefaults.IsGlobal(b.Action))
                {
                    conflicts.Add(new Conflict(a.Action, b.Action, a.KeyCombo));
                }
            }
        }

        return conflicts;
    }

    public static bool HasConflicts(IEnumerable<ShortcutBinding> bindings) => FindConflicts(bindings).Count > 0;
}
