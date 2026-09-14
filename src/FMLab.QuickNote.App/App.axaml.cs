using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FMLab.QuickNote.App.Shortcuts;
using FMLab.QuickNote.Core.Ipc;
using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.App;

public partial class App : Application
{
    private NoteWindow? _noteWindow;
    private HistoryWindow? _historyWindow;
    private SettingsWindow? _settingsWindow;
    private HelpWindow? _helpWindow;
    private AboutWindow? _aboutWindow;
    private GlobalHotkeyService? _globalHotkeyService;
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
            // O processo fica residente na bandeja; fechar a janela apenas esconde
            // (ver NoteWindow.OnClosing). O encerramento real só acontece pelo menu
            // "Sair" da bandeja.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _noteWindow = new NoteWindow();
            desktop.MainWindow = _noteWindow;

            _historyWindow = new HistoryWindow();
            _historyWindow.NoteReopenRequested += note => _noteWindow?.OpenNoteForEditing(note);

            _settingsWindow = new SettingsWindow();
            _settingsWindow.BindingsSaved += ReloadShortcutBindings;
            _settingsWindow.FontSettingsSaved += ReloadFontSettings;

            _helpWindow = new HelpWindow();

            _aboutWindow = new AboutWindow();

            SetupTrayIcon(desktop);
            _ = SetupGlobalHotkeyServiceAsync();
            SetupCommandPipeServer();

            if (StartupCommand is { } startupCommand)
            {
                HandleCommand(startupCommand);
            }

            desktop.ShutdownRequested += (_, _) =>
            {
                _globalHotkeyService?.Dispose();
                _commandPipeServer?.Dispose();
                InstanceLock?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupCommandPipeServer()
    {
        _commandPipeServer = new CommandPipeServer(IpcDefaults.PipeName);
        // Assim como o hook global (ver GlobalHotkeyService), o servidor do pipe despacha
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
                _noteWindow?.ShowNewNote();
                break;
            case AppCommand.EditDraft:
                _noteWindow?.ShowPreviousDraft();
                break;
            case AppCommand.OpenHistory:
                _historyWindow?.ShowAndRefresh();
                break;
            case AppCommand.Toggle:
                _noteWindow?.ToggleVisibility();
                break;
        }
    }

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var trayIcon = new TrayIcon
        {
            Icon = CreateIcon(),
            ToolTipText = "FMLab.QuickNote",
            Menu = new NativeMenu()
        };

        var newNoteItem = new NativeMenuItem("Nova nota");
        newNoteItem.Click += (_, _) => HandleCommand(AppCommand.NewNote);

        var editDraftItem = new NativeMenuItem("Editar rascunho");
        editDraftItem.Click += (_, _) => HandleCommand(AppCommand.EditDraft);

        var openHistoryItem = new NativeMenuItem("Histórico");
        openHistoryItem.Click += (_, _) => HandleCommand(AppCommand.OpenHistory);

        var settingsItem = new NativeMenuItem("Configurações");
        settingsItem.Click += (_, _) => _settingsWindow?.ShowAndRefresh();

        var helpItem = new NativeMenuItem("Atalhos de teclado");
        helpItem.Click += (_, _) => _helpWindow?.ShowAndRefresh();

        var aboutItem = new NativeMenuItem("Sobre");
        aboutItem.Click += (_, _) => _aboutWindow?.ShowAndActivate();

        var exitItem = new NativeMenuItem("Sair");
        exitItem.Click += (_, _) => desktop.Shutdown();

        trayIcon.Menu.Items.Add(newNoteItem);
        trayIcon.Menu.Items.Add(editDraftItem);
        trayIcon.Menu.Items.Add(openHistoryItem);
        trayIcon.Menu.Items.Add(settingsItem);
        trayIcon.Menu.Items.Add(helpItem);
        trayIcon.Menu.Items.Add(new NativeMenuItemSeparator());
        trayIcon.Menu.Items.Add(aboutItem);
        trayIcon.Menu.Items.Add(new NativeMenuItemSeparator());
        trayIcon.Menu.Items.Add(exitItem);

        trayIcon.Clicked += (_, _) => _noteWindow?.ToggleVisibility();

        TrayIcon.SetIcons(this, [trayIcon]);
    }

    private async System.Threading.Tasks.Task SetupGlobalHotkeyServiceAsync()
    {
        var bindings = ShortcutBindingsResolver.Resolve(
            new FileShortcutBindingsStore(FileShortcutBindingsStore.GetDefaultPath()));

        _globalHotkeyService = new GlobalHotkeyService(bindings);
        // SharpHook despacha os eventos em uma thread da pool; qualquer acesso a
        // objetos de UI precisa ser marshalled de volta pro Dispatcher (achado do spike da Fase 1).
        _globalHotkeyService.ActionRequested += action =>
            Dispatcher.UIThread.Post(() => HandleCommand(ToAppCommand(action)));

        var started = await _globalHotkeyService.TryStartAsync();
        if (!started)
        {
            Console.WriteLine(
                "[GlobalHotkeyService] Hotkey global indisponível nesta sessão; use o ícone da bandeja.");
        }
    }

    /// <summary>
    /// Reaplica os bindings salvos na tela de configurações sem precisar reiniciar o processo:
    /// atualiza as janelas já abertas e reinicia o hook global com os novos atalhos.
    /// </summary>
    private void ReloadShortcutBindings()
    {
        var bindings = ShortcutBindingsResolver.Resolve(
            new FileShortcutBindingsStore(FileShortcutBindingsStore.GetDefaultPath()));

        _noteWindow?.ApplyBindings(bindings);
        _historyWindow?.ApplyBindings(bindings);
        _helpWindow?.ApplyBindings(bindings);

        _globalHotkeyService?.Dispose();
        _globalHotkeyService = null;
        _ = SetupGlobalHotkeyServiceAsync();
    }

    /// <summary>
    /// Reaplica a fonte salva na tela de configurações sem precisar reiniciar o processo:
    /// atualiza o editor principal e a lista de histórico já abertos.
    /// </summary>
    private void ReloadFontSettings()
    {
        _noteWindow?.ApplyFontSettings();
        _historyWindow?.ApplyFontSettings();
    }

    private static AppCommand ToAppCommand(ShortcutAction action) => action switch
    {
        ShortcutAction.NewNote => AppCommand.NewNote,
        ShortcutAction.EditDraft => AppCommand.EditDraft,
        ShortcutAction.OpenHistory => AppCommand.OpenHistory,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Ação não é um atalho global."),
    };

    private static WindowIcon CreateIcon()
    {
        var uri = new Uri("avares://FMLab.QuickNote.App/Assets/icon-256.png");
        using var stream = AssetLoader.Open(uri);
        return new WindowIcon(new Bitmap(stream));
    }
}
