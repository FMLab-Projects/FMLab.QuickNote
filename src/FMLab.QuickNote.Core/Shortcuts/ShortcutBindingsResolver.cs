namespace FMLab.QuickNote.Core.Shortcuts;

/// <summary>
/// Combina os defaults com os bindings customizados válidos do store: cada ação usa seu
/// binding customizado se houver um válido, senão cai no default — nunca fica sem atalho.
/// </summary>
public static class ShortcutBindingsResolver
{
    public static IReadOnlyDictionary<ShortcutAction, KeyCombo> Resolve(IShortcutBindingsStore store)
    {
        var result = ShortcutDefaults.All.ToDictionary(b => b.Action, b => b.KeyCombo);

        foreach (var binding in store.Load() ?? [])
        {
            result[binding.Action] = binding.KeyCombo;
        }

        return result;
    }
}
