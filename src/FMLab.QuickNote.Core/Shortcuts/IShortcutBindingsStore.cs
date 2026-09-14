namespace FMLab.QuickNote.Core.Shortcuts;

public interface IShortcutBindingsStore
{
    /// <summary>Bindings customizados válidos, ou null se não houver nenhum salvo ainda.</summary>
    IReadOnlyList<ShortcutBinding>? Load();

    void Save(IReadOnlyList<ShortcutBinding> bindings);
}
