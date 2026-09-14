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

namespace FMLab.QuickNote.App;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private GlobalHotkeySpikeService? _hotkeySpike;

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

            desktop.ShutdownRequested += (_, _) => _hotkeySpike?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
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
