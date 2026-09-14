using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace FMLab.QuickNote.App.Autostart;

/// <summary>
/// Liga/desliga via um LaunchAgent (`~/Library/LaunchAgents/dev.fmlab.quicknote.plist`) com
/// `RunAtLoad`. Implementado a partir da spec upstream (`launchd.plist(5)`); não executado
/// fisicamente em macOS nesta rodada — mesma ressalva da ADR 0001 (validar numa Mac real antes
/// de confiar 100%). Lembrete de plataforma: o hook global de teclado (SharpHook) também exige
/// permissão de Acessibilidade no macOS — ver `docs/INSTALL.md`.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacAutostartService : IAutostartService
{
    private const string Label = "dev.fmlab.quicknote";

    public bool IsSupported => OperatingSystem.IsMacOS();

    public bool IsEnabled() => IsSupported && File.Exists(GetPlistPath());

    public void SetEnabled(bool enabled)
    {
        if (!IsSupported)
        {
            return;
        }

        var path = GetPlistPath();

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

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var content = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>Label</key>
                <string>{Label}</string>
                <key>ProgramArguments</key>
                <array>
                    <string>{exePath}</string>
                    <string>--toggle</string>
                </array>
                <key>RunAtLoad</key>
                <true/>
            </dict>
            </plist>

            """;
        File.WriteAllText(path, content);
    }

    private static string GetPlistPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "LaunchAgents", $"{Label}.plist");
}
