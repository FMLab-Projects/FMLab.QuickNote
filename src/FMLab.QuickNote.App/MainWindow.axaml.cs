using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;

namespace FMLab.QuickNote.App;

/// <summary>
/// Fase 1 spike: janela borderless + Topmost que esconde (não fecha) via Esc
/// ou via botão de fechar do sistema, e foca o editor ao ser exibida.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Opened += (_, _) => BodyTextBox.Focus();
        Activated += (_, _) => BodyTextBox.Focus();

        KeyDown += OnKeyDown;
        Closing += OnClosing;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Spike do ciclo "fechar esconde, não encerra o processo" (refinado na Fase 6).
        e.Cancel = true;
        Hide();
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
            Hide();
        }
        else
        {
            ShowAndFocus();
        }
    }
}
