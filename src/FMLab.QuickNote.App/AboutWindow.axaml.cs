using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace FMLab.QuickNote.App;

/// <summary>
/// Tela "Sobre": nome do app, versão e empresa desenvolvedora, lidos dos metadados do
/// assembly (ver <c>Directory.Build.props</c>) em vez de hardcoded, pra não desatualizar.
/// Segue o mesmo padrão das outras janelas auxiliares (<see cref="SettingsWindow"/>,
/// <see cref="HistoryWindow"/>): fica residente e some (<see cref="Hide"/>) em vez de fechar.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        AppIcon.Source = new Bitmap(AssetLoader.Open(new Uri("avares://FMLab.QuickNote.App/Assets/icon-256.png")));

        var assembly = Assembly.GetExecutingAssembly();
        var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "FMLab.QuickNote";
        var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "FMLab";
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        // O SourceLink embute o hash do commit após o "+" (ex: "0.1.0-beta+e131b78...");
        // é útil pra diagnóstico mas polui a tela de Sobre, então mostramos só o SemVer.
        var version = informationalVersion?.Split('+')[0]
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        AppNameText.Text = product;
        VersionText.Text = $"Versão {version}";
        CompanyText.Text = $"Desenvolvido por {company}";
        CopyrightText.Text = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;

        Closing += OnClosing;
    }

    public void ShowAndActivate()
    {
        Show();
        Activate();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Hide();

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
