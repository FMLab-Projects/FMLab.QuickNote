namespace FMLab.QuickNote.App.Autostart;

/// <summary>
/// Liga/desliga o app iniciar junto com o login do usuário. A implementação real varia por
/// plataforma (Fase 10 cobre o mecanismo definitivo de cada uma — chave Run/atalho no Startup
/// no Windows, LaunchAgent no macOS, `.desktop` no Linux — a partir do empacotamento
/// `dotnet publish` self-contained). Por ora só Windows está implementado (plataforma de
/// desenvolvimento); as demais reportam <see cref="IsSupported"/> false.
/// </summary>
public interface IAutostartService
{
    bool IsSupported { get; }

    bool IsEnabled();

    void SetEnabled(bool enabled);
}
