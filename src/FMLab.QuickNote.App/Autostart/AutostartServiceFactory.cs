using System;

namespace FMLab.QuickNote.App.Autostart;

public static class AutostartServiceFactory
{
    public static IAutostartService Create() =>
        OperatingSystem.IsWindows() ? new WindowsAutostartService() : new UnsupportedAutostartService();
}
