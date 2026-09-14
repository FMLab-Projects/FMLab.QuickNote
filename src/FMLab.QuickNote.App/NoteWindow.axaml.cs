using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FMLab.QuickNote.Core.Editor;
using FMLab.QuickNote.Core.Settings;

namespace FMLab.QuickNote.App;

/// <summary>
/// Janela principal (shell minimalista): sem barra de título, sempre no topo, esconde (não
/// fecha) via Esc ou pelo botão de fechar do sistema, e foca o editor ao ser exibida. Posição e
/// tamanho são lembrados entre sessões via <see cref="IWindowPlacementStore"/>.
/// </summary>
public partial class NoteWindow : Window
{
    private readonly IWindowPlacementStore _placementStore;

    public NoteWindow() : this(new FileWindowPlacementStore(FileWindowPlacementStore.GetDefaultPath()))
    {
    }

    public NoteWindow(IWindowPlacementStore placementStore)
    {
        _placementStore = placementStore;
        InitializeComponent();

        ApplySavedPlacement();

        Opened += (_, _) => BodyTextBox.Focus();
        Activated += (_, _) => BodyTextBox.Focus();

        KeyDown += OnKeyDown;
        Closing += OnClosing;

        BodyTextBox.KeyDown += OnBodyKeyDown;
        BodyTextBox.PointerReleased += OnBodyPointerReleased;
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
        if (e.Key == Key.Escape)
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

        if (e.Key == Key.Tab)
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
        else if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            ApplyEdit(StructuredTextEditor.ToggleCheckboxOnLine(text, BodyTextBox.CaretIndex));
            e.Handled = true;
        }
    }

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
        // Ciclo de vida real (Fase 6): fechar esconde e persiste; o processo só encerra pelo
        // menu "Sair" da bandeja.
        e.Cancel = true;
        HideAndPersist();
    }

    private void HideAndPersist()
    {
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
}
