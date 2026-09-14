using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FMLab.QuickNote.App.Shortcuts;
using FMLab.QuickNote.Core.Notes;
using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.App;

/// <summary>
/// Tela de histórico: lista as notas (ativas e concluídas), com filtro e ações de
/// reabrir/concluir/reativar direto da lista. Nunca fecha de verdade (mesmo padrão de
/// <see cref="NoteWindow"/>: <c>Closing</c> cancela e apenas esconde).
/// </summary>
public partial class HistoryWindow : Window
{
    private readonly INoteRepository _noteRepository;
    private readonly IReadOnlyDictionary<ShortcutAction, KeyCombo> _bindings;

    /// <summary>Disparado quando o usuário pede pra reabrir uma nota da lista no editor principal.</summary>
    public event Action<Note>? NoteReopenRequested;

    public HistoryWindow() : this(
        new FileNoteRepository(FileNoteRepository.GetDefaultDirectory()),
        ShortcutBindingsResolver.Resolve(new FileShortcutBindingsStore(FileShortcutBindingsStore.GetDefaultPath())))
    {
    }

    public HistoryWindow(INoteRepository noteRepository, IReadOnlyDictionary<ShortcutAction, KeyCombo> bindings)
    {
        _noteRepository = noteRepository;
        _bindings = bindings;
        InitializeComponent();

        FilterComboBox.SelectionChanged += (_, _) => Reload();
        KeyDown += OnKeyDown;
        Closing += OnClosing;
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

        var notes = filter.Apply(_noteRepository.GetHistory());
        NotesListBox.ItemsSource = notes.Select(n => new NoteListItem(n)).ToList();
        EmptyStateText.IsVisible = notes.Count == 0;
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
