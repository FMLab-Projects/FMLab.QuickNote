using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FMLab.QuickNote.App.Fonts;
using FMLab.QuickNote.App.Shortcuts;
using FMLab.QuickNote.Core.Notes;
using FMLab.QuickNote.Core.Settings;
using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.App;

/// <summary>
/// Tela de histórico: lista as notas (ativas e concluídas), com filtro e ações de
/// reabrir/concluir/reativar direto da lista. Nunca fecha de verdade (mesmo padrão de
/// <see cref="NoteWindow"/>: <c>Closing</c> cancela e apenas esconde).
/// </summary>
public partial class HistoryWindow : Window
{
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(250);

    private readonly INoteRepository _noteRepository;
    private readonly IFontSettingsStore _fontSettingsStore;
    private readonly DispatcherTimer _searchDebounceTimer;
    private IReadOnlyDictionary<ShortcutAction, KeyCombo> _bindings;

    /// <summary>Disparado quando o usuário pede pra reabrir uma nota da lista no editor principal.</summary>
    public event Action<Note>? NoteReopenRequested;

    public HistoryWindow() : this(
        new FileNoteRepository(FileNoteRepository.GetDefaultDirectory()),
        new FileFontSettingsStore(FileFontSettingsStore.GetDefaultPath()),
        ShortcutBindingsResolver.Resolve(new FileShortcutBindingsStore(FileShortcutBindingsStore.GetDefaultPath())))
    {
    }

    public HistoryWindow(
        INoteRepository noteRepository,
        IFontSettingsStore fontSettingsStore,
        IReadOnlyDictionary<ShortcutAction, KeyCombo> bindings)
    {
        _noteRepository = noteRepository;
        _fontSettingsStore = fontSettingsStore;
        _bindings = bindings;
        InitializeComponent();

        ApplyFontSettings();

        _searchDebounceTimer = new DispatcherTimer { Interval = SearchDebounce };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            Reload();
        };

        FilterComboBox.SelectionChanged += (_, _) => Reload();
        SearchTextBox.TextChanged += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        };
        KeyDown += OnKeyDown;
        Closing += OnClosing;
    }

    /// <summary>Aplica bindings recém-salvos na tela de configurações sem precisar reiniciar o processo.</summary>
    public void ApplyBindings(IReadOnlyDictionary<ShortcutAction, KeyCombo> bindings) => _bindings = bindings;

    /// <summary>
    /// Aplica a fonte salva (ou o fallback monoespaçado default) à lista de notas — o preview do
    /// corpo (<see cref="NoteListItem.DisplayTitle"/>) herda a fonte/tamanho da <c>ListBox</c>.
    /// </summary>
    public void ApplyFontSettings()
    {
        var (fontFamily, fontSize) = FontSettingsResolver.Resolve(_fontSettingsStore);
        NotesListBox.FontFamily = fontFamily;
        NotesListBox.FontSize = fontSize;
    }

    /// <summary>Recarrega a lista com os dados mais recentes e mostra a janela.</summary>
    public void ShowAndRefresh()
    {
        Reload();
        Show();
        Activate();
    }

    private void Reload()
    {
        var filter = FilterComboBox.SelectedIndex switch
        {
            1 => HistoryFilter.Active,
            2 => HistoryFilter.Completed,
            _ => HistoryFilter.All,
        };

        var allNotes = _noteRepository.GetHistory();
        var statusFiltered = filter.Apply(allNotes);

        var query = SearchTextBox.Text;
        var notes = string.IsNullOrWhiteSpace(query) ? statusFiltered : NoteSearch.Search(statusFiltered, query);

        NotesListBox.ItemsSource = notes.Select(n => new NoteListItem(n)).ToList();
        EmptyStateText.IsVisible = notes.Count == 0;
        EmptyStateText.Text = statusFiltered.Count == 0
            ? "Nenhuma nota encontrada."
            : "Nenhum resultado para a busca.";

        // Footer sempre reflete o total geral (Fase 13), independente de filtro/busca.
        FooterText.Text = HistoryStats.From(allNotes).ToString();
    }

    private void OnReopenClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: NoteListItem item })
        {
            NoteReopenRequested?.Invoke(item.Note);
            HideWindow();
        }
    }

    private void OnToggleCompletionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: NoteListItem item })
        {
            return;
        }

        if (item.Note.IsActive)
        {
            _noteRepository.Complete(item.Note.Id);
        }
        else
        {
            _noteRepository.Reactivate(item.Note.Id);
        }

        Reload();
    }

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
