using System;

namespace FMLab.QuickNote.App.Autostart;

public static class AutostartServiceFactory
{
    public static IAutostartService Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsAutostartService();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxAutostartService();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacAutostartService();
        }

        return new UnsupportedAutostartService();
    }
}
