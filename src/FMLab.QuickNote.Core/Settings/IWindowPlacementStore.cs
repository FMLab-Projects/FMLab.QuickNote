namespace FMLab.QuickNote.Core.Settings;

public interface IWindowPlacementStore
{
    /// <summary>Null when nothing was saved yet (fresh install) or the saved data is unreadable.</summary>
    WindowPlacement? Load();

    void Save(WindowPlacement placement);
}
