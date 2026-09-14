using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SharpHook;
using SharpHook.Data;

namespace FMLab.QuickNote.App.Spikes;

/// <summary>
/// Fase 1 spike: valida se o hook global de teclado (SharpHook/libuiohook) consegue
/// detectar os atalhos padrão de "Nova nota" (Ctrl+Alt+N) e "Editar rascunho" (Ctrl+Alt+Q)
/// nesta plataforma. Falhas de inicialização (ex.: permissão negada em /dev/input no Linux)
/// são logadas e não derrubam a aplicação — o usuário ainda consegue operar via bandeja.
/// </summary>
public sealed class GlobalHotkeySpikeService : IDisposable
{
    private readonly TaskPoolGlobalHook _hook = new();
    private bool _ctrlDown;
    private bool _altDown;

    public event Action? NewNoteRequested;
    public event Action? EditDraftRequested;

    public GlobalHotkeySpikeService()
    {
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
    }

    public async Task<bool> TryStartAsync()
    {
        try
        {
            _ = _hook.RunAsync();
            Log("Hook global iniciado.");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Falha ao iniciar hook global: {ex.Message}");
            return false;
        }
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcLeftControl:
            case KeyCode.VcRightControl:
                _ctrlDown = true;
                break;
            case KeyCode.VcLeftAlt:
            case KeyCode.VcRightAlt:
                _altDown = true;
                break;
            case KeyCode.VcN when _ctrlDown && _altDown:
                Log("Ctrl+Alt+N detectado -> Nova nota");
                NewNoteRequested?.Invoke();
                break;
            case KeyCode.VcQ when _ctrlDown && _altDown:
                Log("Ctrl+Alt+Q detectado -> Editar rascunho anterior");
                EditDraftRequested?.Invoke();
                break;
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcLeftControl:
            case KeyCode.VcRightControl:
                _ctrlDown = false;
                break;
            case KeyCode.VcLeftAlt:
            case KeyCode.VcRightAlt:
                _altDown = false;
                break;
        }
    }

    private static void Log(string message)
    {
        var line = $"[GlobalHotkeySpike] {message}";
        Debug.WriteLine(line);
        Console.WriteLine(line);
    }

    public void Dispose()
    {
        _hook.KeyPressed -= OnKeyPressed;
        _hook.KeyReleased -= OnKeyReleased;
        _hook.Dispose();
    }
}
