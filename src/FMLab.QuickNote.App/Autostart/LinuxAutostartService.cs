using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace FMLab.QuickNote.App.Autostart;

/// <summary>
/// Liga/desliga via um arquivo `.desktop` em `~/.config/autostart` (convenção XDG seguida
/// pelas principais DEs — GNOME, KDE, XFCE). Implementado a partir da spec upstream; não
/// executado fisicamente em Linux nesta rodada (mesma ressalva da ADR 0001 pros spikes de
/// hotkey/tray da Fase 1) — validar manualmente numa sessão gráfica real antes de confiar 100%.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxAutostartService : IAutostartService
{
    private const string DesktopFileName = "fmlab-quicknote.desktop";

    public bool IsSupported => OperatingSystem.IsLinux();

    public bool IsEnabled() => IsSupported && File.Exists(GetDesktopFilePath());

    public void SetEnabled(bool enabled)
    {
        if (!IsSupported)
        {
            return;
        }

        var path = GetDesktopFilePath();

        if (!enabled)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return;
        }

        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath is null)
        {
            return;
        }

        Directory.CreateDirectory(GetAutostartDirectory());
        var content = string.Join('\n',
            "[Desktop Entry]",
            "Type=Application",
            "Name=Quick Note",
            $"Exec=\"{exePath}\" --toggle",
            "X-GNOME-Autostart-enabled=true",
            "");
        File.WriteAllText(path, content);
    }

    // Environment.SpecialFolder.ApplicationData mapeia pra ~/.config no Unix PAL do .NET —
    // mesma pasta base já usada por FileNoteRepository/FileWindowPlacementStore.
    private static string GetAutostartDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autostart");

    private static string GetDesktopFilePath() => Path.Combine(GetAutostartDirectory(), DesktopFileName);
}
