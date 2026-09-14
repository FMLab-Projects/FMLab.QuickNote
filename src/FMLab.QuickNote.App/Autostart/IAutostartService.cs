namespace FMLab.QuickNote.App.Autostart;

/// <summary>
/// Liga/desliga o app iniciar junto com o login do usuário. A implementação real varia por
/// plataforma: chave Run do usuário no Windows (<see cref="WindowsAutostartService"/>),
/// LaunchAgent no macOS (<see cref="MacAutostartService"/>), `.desktop` em
/// `~/.config/autostart` no Linux (<see cref="LinuxAutostartService"/>). Só a implementação
/// Windows foi executada de verdade (plataforma de desenvolvimento); Linux/macOS seguem a spec
/// upstream mas não foram testadas numa máquina real (ver ressalva equivalente na ADR 0001).
/// <see cref="UnsupportedAutostartService"/> cobre qualquer outra plataforma.
/// </summary>
public interface IAutostartService
{
    bool IsSupported { get; }

    bool IsEnabled();

    void SetEnabled(bool enabled);
}
