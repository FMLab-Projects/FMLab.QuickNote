using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FMLab.QuickNote.App.Shortcuts;
using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.App;

/// <summary>
/// Tela de ajuda: lista somente-leitura de todos os atalhos atualmente em vigor (defaults ou
/// remapeados na tela de Configurações), servindo de referência rápida pro usuário. Segue o
/// mesmo padrão das outras janelas auxiliares (<see cref="HistoryWindow"/>): fica residente e
/// some (<see cref="Hide"/>) em vez de fechar; <see cref="ApplyBindings"/> é chamado pela
/// <c>App</c> quando os atalhos são salvos, sem precisar reabrir a janela.
/// </summary>
public partial class HelpWindow : Window
{
    private IReadOnlyDictionary<ShortcutAction, KeyCombo> _bindings;

    public HelpWindow() : this(
        ShortcutBindingsResolver.Resolve(new FileShortcutBindingsStore(FileShortcutBindingsStore.GetDefaultPath())))
    {
    }

    public HelpWindow(IReadOnlyDictionary<ShortcutAction, KeyCombo> bindings)
    {
        _bindings = bindings;
        InitializeComponent();

        KeyDown += OnKeyDown;
        Closing += OnClosing;

        RefreshRows();
    }

    /// <summary>Aplica bindings recém-salvos na tela de configurações sem precisar reiniciar o processo.</summary>
    public void ApplyBindings(IReadOnlyDictionary<ShortcutAction, KeyCombo> bindings)
    {
        _bindings = bindings;
        RefreshRows();
    }

    public void ShowAndRefresh()
    {
        RefreshRows();
        Show();
        Activate();
    }

    private void RefreshRows()
    {
        // Segue a ordem canônica dos defaults em vez da ordem do dicionário, que não é estável.
        ShortcutsItemsControl.ItemsSource = ShortcutDefaults.All
            .Select(b => b.Action)
            .Where(_bindings.ContainsKey)
            .Select(action => new ShortcutRow(action, _bindings[action].ToString())
            {
                IsGlobal = ShortcutDefaults.IsGlobal(action),
            })
            .ToList();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => HideWindow();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_bindings.TryGetValue(ShortcutAction.Close, out var combo) && ShortcutMatcher.Matches(e, combo))
        {
            HideWindow();
            e.Handled = true;
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        HideWindow();
    }

    private void HideWindow() => Hide();
}
