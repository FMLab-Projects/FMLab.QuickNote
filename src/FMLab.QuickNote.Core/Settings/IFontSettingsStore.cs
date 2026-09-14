namespace FMLab.QuickNote.Core.Settings;

public interface IFontSettingsStore
{
    /// <summary>Null when nothing was saved yet (fresh install) or the saved data is unreadable.</summary>
    FontSettings? Load();

    void Save(FontSettings settings);
}
