using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using FMLab.QuickNote.App.Shortcuts;
using FMLab.QuickNote.Core.Editor;
using FMLab.QuickNote.Core.Notes;
using FMLab.QuickNote.Core.Settings;
using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.App;

/// <summary>
/// Janela principal (shell minimalista): sem barra de título, sempre no topo, esconde (não
/// fecha) via Esc ou pelo botão de fechar do sistema, e foca o editor ao ser exibida. Posição e
/// tamanho são lembrados entre sessões via <see cref="IWindowPlacementStore"/>. O ciclo de vida
/// da nota (criar/atualizar/concluir) é delegado à <see cref="NoteEditingSession"/>; esta classe
/// só traduz os TextBox de/para texto puro (e o formato de exibição do checkbox — ver
/// <see cref="StructuredTextEditor"/>). Os atalhos locais (fechar, concluir, alternar checkbox)
/// usam os bindings resolvidos pela Fase 7 (<see cref="ShortcutBindingsResolver"/>); os globais
/// (nova nota, editar rascunho, histórico) são tratados pelo <see cref="Shortcuts.GlobalHotkeyService"/> na <c>App</c>.
/// </summary>
public partial class NoteWindow : Window
{
    private static readonly TimeSpan AutosaveDebounce = TimeSpan.FromMilliseconds(1500);

    private readonly IWindowPlacementStore _placementStore;
    private readonly NoteEditingSession _session;
    private IReadOnlyDictionary<ShortcutAction, KeyCombo> _bindings;
    private readonly DispatcherTimer _autosaveTimer;

    public NoteWindow() : this(
        new FileWindowPlacementStore(FileWindowPlacementStore.GetDefaultPath()),
        new FileNoteRepository(FileNoteRepository.GetDefaultDirectory()),
        ShortcutBindingsResolver.Resolve(new FileShortcutBindingsStore(FileShortcutBindingsStore.GetDefaultPath())))
    {
    }

    public NoteWindow(
        IWindowPlacementStore placementStore,
        INoteRepository noteRepository,
        IReadOnlyDictionary<ShortcutAction, KeyCombo> bindings)
    {
        _placementStore = placementStore;
        _session = new NoteEditingSession(noteRepository);
        _bindings = bindings;
        InitializeComponent();

        ApplySavedPlacement();

        Opened += (_, _) => BodyTextBox.Focus();
        Activated += (_, _) => BodyTextBox.Focus();

        KeyDown += OnKeyDown;
        Closing += OnClosing;

        BodyTextBox.KeyDown += OnBodyKeyDown;
        BodyTextBox.PointerReleased += OnBodyPointerReleased;

        // Autosave (debounce curto): reduz a perda de conteúdo se o processo cair antes do
        // usuário fechar/esconder a janela (que já salva no ato).
        _autosaveTimer = new DispatcherTimer { Interval = AutosaveDebounce };
        _autosaveTimer.Tick += (_, _) =>
        {
            _autosaveTimer.Stop();
            SaveCurrentNote();
        };
        TitleTextBox.TextChanged += (_, _) => RestartAutosaveTimer();
        BodyTextBox.TextChanged += (_, _) => RestartAutosaveTimer();
    }

    /// <summary>Aplica bindings recém-salvos na tela de configurações sem precisar reiniciar o processo.</summary>
    public void ApplyBindings(IReadOnlyDictionary<ShortcutAction, KeyCombo> bindings) => _bindings = bindings;

    private void RestartAutosaveTimer()
    {
        _autosaveTimer.Stop();
        _autosaveTimer.Start();
    }

    private void ApplySavedPlacement()
    {
        var placement = _placementStore.Load();
        if (placement is null)
        {
            // Primeira execução: sem posição salva ainda, deixa o SO centralizar a janela.
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        Width = placement.Width;
        Height = placement.Height;

        if (placement.X is { } x && placement.Y is { } y)
        {
            Position = new PixelPoint((int)x, (int)y);
        }
    }

    private void SavePlacement() => _placementStore.Save(new WindowPlacement
    {
        X = Position.X,
        Y = Position.Y,
        Width = Width,
        Height = Height,
    });

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (Matches(e, ShortcutAction.Close))
        {
            HideAndPersist();
            e.Handled = true;
        }
    }

    private void OnBodyKeyDown(object? sender, KeyEventArgs e)
    {
        var text = BodyTextBox.Text ?? string.Empty;
        var selectionStart = BodyTextBox.SelectionStart;
        var selectionEnd = BodyTextBox.SelectionEnd;

        if (Matches(e, ShortcutAction.Complete))
        {
            CompleteCurrentNote();
            e.Handled = true;
        }
        else if (e.Key == Key.Tab)
        {
            ApplyEdit(e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                ? StructuredTextEditor.Outdent(text, selectionStart, selectionEnd)
                : StructuredTextEditor.Indent(text, selectionStart, selectionEnd));
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
        {
            ApplyEdit(StructuredTextEditor.HandleEnter(text, selectionStart, selectionEnd));
            e.Handled = true;
        }
        else if (Matches(e, ShortcutAction.ToggleCheckbox))
        {
            ApplyEdit(StructuredTextEditor.ToggleCheckboxOnLine(text, BodyTextBox.CaretIndex));
            e.Handled = true;
        }
    }

    /// <summary>Compara a tecla/modificadores pressionados com o binding configurado para a ação.</summary>
    private bool Matches(KeyEventArgs e, ShortcutAction action) =>
        _bindings.TryGetValue(action, out var combo) && ShortcutMatcher.Matches(e, combo);

    private void OnBodyPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var text = BodyTextBox.Text ?? string.Empty;
        var result = StructuredTextEditor.ToggleCheckboxNearColumn(text, BodyTextBox.CaretIndex);
        if (result.Text != text)
        {
            ApplyEdit(result);
        }
    }

    private void ApplyEdit(EditResult result)
    {
        BodyTextBox.Text = result.Text;
        BodyTextBox.SelectionStart = result.SelectionStart;
        BodyTextBox.SelectionEnd = result.SelectionEnd;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Fechar (Esc, botão do SO) esconde e salva; o processo só encerra pelo menu "Sair" da
        // bandeja.
        e.Cancel = true;
        HideAndPersist();
    }

    private void HideAndPersist()
    {
        _autosaveTimer.Stop();
        SaveCurrentNote();
        Hide();
        SavePlacement();
    }

    public void ShowAndFocus()
    {
        Show();
        Activate();
        BodyTextBox.Focus();
    }

    public void ToggleVisibility()
    {
        if (IsVisible)
        {
            HideAndPersist();
        }
        else
        {
            ShowAndFocus();
        }
    }

    /// <summary>Salva a nota aberta (se houver algo digitado) e abre a janela com uma nota em branco.</summary>
    public void ShowNewNote()
    {
        SaveCurrentNote();
        _session.LoadBlank();
        ClearEditor();
        ShowAndFocus();
    }

    /// <summary>Salva a nota aberta (se houver algo digitado) e carrega o rascunho ativo mais recente.</summary>
    public void ShowPreviousDraft()
    {
        SaveCurrentNote();
        var draft = _session.LoadPreviousDraft();
        if (draft is not null)
        {
            LoadNoteIntoEditor(draft);
        }
        else
        {
            ClearEditor();
        }

        ShowAndFocus();
    }

    /// <summary>Salva a nota aberta (se houver algo digitado) e carrega a nota informada (ex.: reabrir a partir do histórico).</summary>
    public void OpenNoteForEditing(Note note)
    {
        SaveCurrentNote();
        _session.LoadNote(note);
        LoadNoteIntoEditor(note);
        ShowAndFocus();
    }

    /// <summary>
    /// Salva a nota aberta, marca como concluída (soft delete) e abre uma nota em branco em
    /// seguida — mantém a janela pronta para a próxima captura rápida.
    /// </summary>
    public void CompleteCurrentNote()
    {
        _session.CompleteCurrent(TitleTextBox.Text, GetStorageContent());
        ClearEditor();
    }

    private void SaveCurrentNote() => _session.SaveIfNeeded(TitleTextBox.Text, GetStorageContent());

    private string GetStorageContent() => StructuredTextEditor.ToStorageText(BodyTextBox.Text ?? string.Empty);

    private void ClearEditor()
    {
        TitleTextBox.Text = string.Empty;
        BodyTextBox.Text = string.Empty;
    }

    private void LoadNoteIntoEditor(Note note)
    {
        TitleTextBox.Text = note.Title ?? string.Empty;
        BodyTextBox.Text = StructuredTextEditor.ToDisplayText(note.Content);
    }
}
