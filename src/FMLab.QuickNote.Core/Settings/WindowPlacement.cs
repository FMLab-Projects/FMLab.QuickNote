namespace FMLab.QuickNote.Core.Settings;

/// <summary>Last known position/size of the note window, remembered across sessions.</summary>
public sealed class WindowPlacement
{
    /// <summary>Null on a fresh install: the window has never been positioned by the user yet.</summary>
    public double? X { get; set; }

    public double? Y { get; set; }

    public required double Width { get; set; }

    public required double Height { get; set; }
}
