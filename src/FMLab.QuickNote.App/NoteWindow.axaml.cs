using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
