namespace FMLab.QuickNote.App.Autostart;

/// <summary>Placeholder para plataformas sem implementação ainda (Linux/macOS — ver Fase 10).</summary>
public sealed class UnsupportedAutostartService : IAutostartService
{
    public bool IsSupported => false;

    public bool IsEnabled() => false;

    public void SetEnabled(bool enabled)
    {
        // Nada a fazer: a UI de configurações desabilita o toggle quando IsSupported é false.
    }
}
