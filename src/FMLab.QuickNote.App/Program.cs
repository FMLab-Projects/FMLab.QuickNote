using Avalonia;
using FMLab.QuickNote.Core.Ipc;
using System;

namespace FMLab.QuickNote.App;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var command = ParseCommand(args);

        var instanceLock = SingleInstanceLock.TryAcquire(IpcDefaults.GetDefaultLockFilePath());
        if (!instanceLock.IsPrimaryInstance)
        {
            // Já existe uma instância rodando (ex: usuário disparou o atalho de novo, ou o
            // plano B de CLI do Linux descrito na ADR 0001): repassa o comando por named pipe
            // pra ela e encerra este processo sem abrir uma segunda janela.
            instanceLock.Dispose();
            CommandPipeClient.TrySend(IpcDefaults.PipeName, command, timeoutMilliseconds: 2000);
            return;
        }

        App.InstanceLock = instanceLock;
        App.StartupCommand = command;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    // Comandos aceitos como "--new-note", "--edit-draft", "--open-history", "--toggle"
    // (mesmos nomes usados no protocolo do named pipe, ver AppCommandNames).
    private static AppCommand ParseCommand(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith("--", StringComparison.Ordinal)
                && AppCommandNames.TryParse(arg[2..], out var command))
            {
                return command;
            }
        }

        return AppCommand.Toggle;
    }
}
