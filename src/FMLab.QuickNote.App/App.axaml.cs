using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FMLab.QuickNote.App.Spikes;
using FMLab.QuickNote.Core.Ipc;

namespace FMLab.QuickNote.App;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private GlobalHotkeySpikeService? _hotkeySpike;
    private CommandPipeServer? _commandPipeServer;

    /// <summary>Set by <see cref="Program.Main"/> before startup; owned/disposed by this instance from here on.</summary>
    public static SingleInstanceLock? InstanceLock { get; set; }

    /// <summary>Command this process was launched with (CLI flag), applied once the window exists.</summary>
    public static AppCommand? StartupCommand { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Fase 1 spike: o processo fica residente na bandeja; fechar a janela apenas
            // esconde (ver MainWindow.OnClosing). O encerramento real só acontece pelo
            // menu "Sair" da bandeja.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow = new MainWindow();
            desktop.MainWindow = _mainWindow;

            SetupTrayIconSpike(desktop);
            _ = SetupGlobalHotkeySpikeAsync();
            SetupCommandPipeServer();

            if (StartupCommand is { } startupCommand)
            {
                HandleCommand(startupCommand);
            }

            desktop.ShutdownRequested += (_, _) =>
            {
                _hotkeySpike?.Dispose();
                _commandPipeServer?.Dispose();
                InstanceLock?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupCommandPipeServer()
    {
        _commandPipeServer = new CommandPipeServer(IpcDefaults.PipeName);
        // Assim como o hook global (ver GlobalHotkeySpikeService), o servidor do pipe despacha
        // em thread própria; qualquer toque em UI precisa passar pelo Dispatcher.
        _commandPipeServer.CommandReceived += command =>
            Dispatcher.UIThread.Post(() => HandleCommand(command));
        _commandPipeServer.Start();
    }

    private void HandleCommand(AppCommand command)
    {
        switch (command)
        {
            case AppCommand.NewNote:
            case AppCommand.EditDraft:
                // Fluxo real de "nova nota" vs. "editar rascunho" chega na Fase 6; por ora
                // ambos só trazem a janela à frente, igual ao spike de hotkey da Fase 1.
                _mainWindow?.ShowAndFocus();
                break;
            case AppCommand.OpenHistory:
                // Tela de histórico ainda não existe (Fase 8); por ora só traz a janela à frente.
                _mainWindow?.ShowAndFocus();
                break;
            case AppCommand.Toggle:
                _mainWindow?.ToggleVisibility();
                break;
        }
    }

    private void SetupTrayIconSpike(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var trayIcon = new TrayIcon
        {
            Icon = CreateSpikeIcon(),
            ToolTipText = "FMLab.QuickNote (spike Fase 1)",
            Menu = new NativeMenu()
        };

        var toggleItem = new NativeMenuItem("Mostrar/Ocultar janela");
        toggleItem.Click += (_, _) => _mainWindow?.ToggleVisibility();

        var newNoteItem = new NativeMenuItem("Nova nota (spike)");
        newNoteItem.Click += (_, _) => _mainWindow?.ShowAndFocus();

        var exitItem = new NativeMenuItem("Sair");
        exitItem.Click += (_, _) => desktop.Shutdown();

        trayIcon.Menu.Items.Add(toggleItem);
        trayIcon.Menu.Items.Add(newNoteItem);
        trayIcon.Menu.Items.Add(new NativeMenuItemSeparator());
        trayIcon.Menu.Items.Add(exitItem);

        trayIcon.Clicked += (_, _) => _mainWindow?.ToggleVisibility();

        TrayIcon.SetIcons(this, [trayIcon]);
    }

    private async System.Threading.Tasks.Task SetupGlobalHotkeySpikeAsync()
    {
        _hotkeySpike = new GlobalHotkeySpikeService();
        // SharpHook despacha os eventos em uma thread da pool; qualquer acesso a
        // objetos de UI precisa ser marshalled de volta pro Dispatcher (achado do spike).
        _hotkeySpike.NewNoteRequested += () =>
            Dispatcher.UIThread.Post(() => _mainWindow?.ShowAndFocus());
        _hotkeySpike.EditDraftRequested += () =>
            Dispatcher.UIThread.Post(() => _mainWindow?.ToggleVisibility());

        var started = await _hotkeySpike.TryStartAsync();
        if (!started)
        {
            Console.WriteLine(
                "[GlobalHotkeySpike] Hotkey global indisponível nesta sessão; use o ícone da bandeja.");
        }
    }

    // Gera o ícone da bandeja em runtime (sem depender de um asset .ico externo)
    // apenas para fins deste spike; um ícone real entra na Fase 10.
    private static WindowIcon CreateSpikeIcon()
    {
        const int size = 32;
        var pixelSize = new PixelSize(size, size);
        var visual = new Border
        {
            Width = size,
            Height = size,
            Background = Brushes.SlateBlue,
            CornerRadius = new CornerRadius(6),
            Child = new TextBlock
            {
                Text = "Q",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        visual.Measure(new Size(size, size));
        visual.Arrange(new Rect(0, 0, size, size));

        var bitmap = new RenderTargetBitmap(pixelSize);
        bitmap.Render(visual);

        return new WindowIcon(bitmap);
    }
}
