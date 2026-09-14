namespace FMLab.QuickNote.Core.Ipc;

public static class IpcDefaults
{
    /// <summary>Named-pipe name the primary instance listens on for commands from other processes.</summary>
    public const string PipeName = "FMLab.QuickNote.Commands";

    public static string GetDefaultLockFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FMLab.QuickNote",
        "instance.lock");
}
