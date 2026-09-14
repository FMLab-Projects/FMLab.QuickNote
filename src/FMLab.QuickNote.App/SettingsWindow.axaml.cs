using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using FMLab.QuickNote.App.Autostart;
using FMLab.QuickNote.App.Fonts;
using FMLab.QuickNote.App.Shortcuts;
using FMLab.QuickNote.Core.Settings;
using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.App;

/// <summary>
/// Tela de configurações: remapear cada atalho (captura de tecla + validação de conflito
/// dentro do mesmo escopo, ver <see cref="ShortcutConflictDetector"/>), ligar/desligar o
/// autostart, e escolher a fonte do editor. Salvar atalhos persiste no
/// <see cref="IShortcutBindingsStore"/> e dispara <see cref="BindingsSaved"/>; salvar a fonte
/// persiste no <see cref="IFontSettingsStore"/> e dispara <see cref="FontSettingsSaved"/> — a
/// <c>App</c> reaplica os dois em tempo real (janelas já abertas + o hook global, sem precisar
/// reiniciar o processo).
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>Item de exibição do <c>ComboBox</c> de fonte; <c>FontFamily == null</c> representa o fallback default.</summary>
    private sealed record FontOption(string? FontFamily, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly IShortcutBindingsStore _bindingsStore;
    private readonly IFontSettingsStore _fontSettingsStore;
    private readonly IAutostartService _autostartService;
    private readonly Dictionary<ShortcutAction, KeyCombo> _workingBindings = new();

    private ShortcutAction? _capturingAction;
    private bool _loadingFontSettings;

    /// <summary>Disparado depois que os atalhos são salvos com sucesso (sem conflitos).</summary>
    public event Action? BindingsSaved;

    /// <summary>Disparado depois que a fonte do editor é salva.</summary>
    public event Action? FontSettingsSaved;

    public SettingsWindow() : this(
        new FileShortcutBindingsStore(FileShortcutBindingsStore.GetDefaultPath()),
        new FileFontSettingsStore(FileFontSettingsStore.GetDefaultPath()),
        AutostartServiceFactory.Create())
    {
    }

    public SettingsWindow(
        IShortcutBindingsStore bindingsStore,
        IFontSettingsStore fontSettingsStore,
        IAutostartService autostartService)
    {
        _bindingsStore = bindingsStore;
        _fontSettingsStore = fontSettingsStore;
        _autostartService = autostartService;

        InitializeComponent();

        if (!_autostartService.IsSupported)
        {
            AutostartCheckBox.IsEnabled = false;
            AutostartCheckBox.Content = "Iniciar com o sistema operacional (não suportado nesta plataforma)";
        }

        KeyDown += OnKeyDown;
        Closing += OnClosing;

        LoadWorkingBindings();
        LoadWorkingFontSettings();
    }

    /// <summary>Recarrega do disco (descarta edições não salvas) e mostra a janela.</summary>
    public void ShowAndRefresh()
    {
        _capturingAction = null;
        LoadWorkingBindings();
        LoadWorkingFontSettings();
        AutostartCheckBox.IsChecked = _autostartService.IsEnabled();
        Show();
        Activate();
    }

    private void LoadWorkingFontSettings()
    {
        _loadingFontSettings = true;

        var options = new List<FontOption> { new(null, "Padrão (monoespaçada do sistema)") };
        options.AddRange(MonospaceFontCatalog.GetInstalledFontNames().Select(name => new FontOption(name, name)));
        FontFamilyComboBox.ItemsSource = options;

        var saved = _fontSettingsStore.Load() ?? FontSettings.Default;
        FontFamilyComboBox.SelectedItem = options.FirstOrDefault(o =>
            string.Equals(o.FontFamily, saved.FontFamily, StringComparison.OrdinalIgnoreCase)) ?? options[0];
        FontSizeNumericUpDown.Value = (decimal)(saved.FontSize > 0 ? saved.FontSize : FontSettings.DefaultFontSize);

        _loadingFontSettings = false;
        UpdateFontPreview();
    }

    private void OnFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_loadingFontSettings)
        {
            return;
        }

        UpdateFontPreview();
    }

    private void OnFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_loadingFontSettings)
        {
            return;
        }

        UpdateFontPreview();
    }

    private void UpdateFontPreview()
    {
        var familyName = (FontFamilyComboBox.SelectedItem as FontOption)?.FontFamily;
        FontPreviewText.FontFamily = string.IsNullOrEmpty(familyName)
            ? new FontFamily(FontSettings.DefaultFontFamily)
            : new FontFamily(familyName);
        FontPreviewText.FontSize = (double)(FontSizeNumericUpDown.Value ?? (decimal)FontSettings.DefaultFontSize);
    }

    private void LoadWorkingBindings()
    {
        _workingBindings.Clear();
        foreach (var (action, combo) in ShortcutBindingsResolver.Resolve(_bindingsStore))
        {
            _workingBindings[action] = combo;
        }

        RefreshRows();
    }

    private void RefreshRows()
    {
        var bindings = _workingBindings.Select(kv => new ShortcutBinding(kv.Key, kv.Value)).ToList();
        var conflicts = ShortcutConflictDetector.FindConflicts(bindings);
        var conflictingActions = conflicts.SelectMany(c => new[] { c.First, c.Second }).ToHashSet();

        ShortcutsItemsControl.ItemsSource = bindings.Select(b => new ShortcutRow(
                b.Action,
                _capturingAction == b.Action ? "Pressione uma combinação..." : b.KeyCombo.ToString())
            {
                HasConflict = conflictingActions.Contains(b.Action),
            })
            .ToList();
    }

    private void OnCaptureClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ShortcutRow row })
        {
            return;
        }

        _capturingAction = row.Action;
        RefreshRows();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_capturingAction is { } action)
        {
            if (IsModifierKey(e.Key))
            {
                return;
            }

            e.Handled = true;

            if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
            {
                // Escape durante a captura cancela (mantém o binding anterior) em vez de virar o atalho.
                _capturingAction = null;
                RefreshRows();
                return;
            }

            _workingBindings[action] = new KeyCombo(ShortcutMatcher.ToShortcutModifiers(e.KeyModifiers), ShortcutMatcher.NormalizeKeyName(e.Key));
            _capturingAction = null;
            RefreshRows();
            return;
        }

        if (ShortcutMatcher.Matches(e, _workingBindings[ShortcutAction.Close]))
        {
            HideWindow();
            e.Handled = true;
        }
    }

    private static bool IsModifierKey(Key key) => key
        is Key.LeftCtrl or Key.RightCtrl
        or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift
        or Key.LWin or Key.RWin;

    private void OnAutostartClick(object? sender, RoutedEventArgs e) =>
        _autostartService.SetEnabled(AutostartCheckBox.IsChecked == true);

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var bindings = _workingBindings.Select(kv => new ShortcutBinding(kv.Key, kv.Value)).ToList();

        if (ShortcutConflictDetector.HasConflicts(bindings))
        {
            StatusText.Foreground = Brushes.IndianRed;
            StatusText.Text = "Existem atalhos em conflito (destacados abaixo). Resolva antes de salvar.";
            StatusText.IsVisible = true;
            RefreshRows();
            return;
        }

        _bindingsStore.Save(bindings);

        var fontFamily = (FontFamilyComboBox.SelectedItem as FontOption)?.FontFamily;
        var fontSize = (double)(FontSizeNumericUpDown.Value ?? (decimal)FontSettings.DefaultFontSize);
        _fontSettingsStore.Save(new FontSettings { FontFamily = fontFamily, FontSize = fontSize });

        StatusText.Foreground = Brushes.LightGreen;
        StatusText.Text = "Configurações salvas.";
        StatusText.IsVisible = true;

        BindingsSaved?.Invoke();
        FontSettingsSaved?.Invoke();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        HideWindow();
    }

    private void HideWindow()
    {
        _capturingAction = null;
        Hide();
    }
}
