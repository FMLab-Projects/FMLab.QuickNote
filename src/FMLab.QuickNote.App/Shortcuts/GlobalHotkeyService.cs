using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FMLab.QuickNote.Core.Shortcuts;
using SharpHook;
using SharpHook.Data;

namespace FMLab.QuickNote.App.Shortcuts;

/// <summary>
/// Hook global de teclado (SharpHook/libuiohook) que dispara as ações configuradas como
/// globais (ver <see cref="ShortcutDefaults.IsGlobal"/>) — funciona mesmo com a janela
/// escondida/sem foco. Roda sempre na instância principal do processo (ver Fase 3): não há
/// necessidade de repassar por IPC, o hook já está no processo dono da janela.
/// Sucede o spike da Fase 1 (<c>docs/adr/0001-hotkey-global-linux.md</c> continua valendo como
/// referência de risco/plano B para Linux).
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private readonly TaskPoolGlobalHook _hook = new();
    private readonly Dictionary<KeyCode, ShortcutAction> _bindingsByKeyCode = new();
    private readonly Dictionary<KeyCode, ShortcutModifiers> _requiredModifiers = new();
    private readonly HashSet<KeyCode> _modifiersDown = [];

    public event Action<ShortcutAction>? ActionRequested;

    public GlobalHotkeyService(IReadOnlyDictionary<ShortcutAction, KeyCombo> bindings)
    {
        foreach (var (action, combo) in bindings)
        {
            if (ShortcutDefaults.IsGlobal(action) && TryMapKeyCode(combo.Key, out var keyCode))
            {
                _bindingsByKeyCode[keyCode] = action;
                _requiredModifiers[keyCode] = combo.Modifiers;
            }
        }

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
        var keyCode = e.Data.KeyCode;

        if (IsModifier(keyCode))
        {
            _modifiersDown.Add(keyCode);
            return;
        }

        if (_bindingsByKeyCode.TryGetValue(keyCode, out var action)
            && CurrentModifiers() == _requiredModifiers[keyCode])
        {
            Log($"{action} disparada por atalho global.");
            ActionRequested?.Invoke(action);
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        if (IsModifier(e.Data.KeyCode))
        {
            _modifiersDown.Remove(e.Data.KeyCode);
        }
    }

    private ShortcutModifiers CurrentModifiers()
    {
        var modifiers = ShortcutModifiers.None;
        if (_modifiersDown.Contains(KeyCode.VcLeftControl) || _modifiersDown.Contains(KeyCode.VcRightControl))
            modifiers |= ShortcutModifiers.Control;
        if (_modifiersDown.Contains(KeyCode.VcLeftAlt) || _modifiersDown.Contains(KeyCode.VcRightAlt))
            modifiers |= ShortcutModifiers.Alt;
        if (_modifiersDown.Contains(KeyCode.VcLeftShift) || _modifiersDown.Contains(KeyCode.VcRightShift))
            modifiers |= ShortcutModifiers.Shift;
        if (_modifiersDown.Contains(KeyCode.VcLeftMeta) || _modifiersDown.Contains(KeyCode.VcRightMeta))
            modifiers |= ShortcutModifiers.Meta;
        return modifiers;
    }

    private static bool IsModifier(KeyCode keyCode) => keyCode
        is KeyCode.VcLeftControl or KeyCode.VcRightControl
        or KeyCode.VcLeftAlt or KeyCode.VcRightAlt
        or KeyCode.VcLeftShift or KeyCode.VcRightShift
        or KeyCode.VcLeftMeta or KeyCode.VcRightMeta;

    /// <summary>Mapeia o nome canônico da tecla (ver <see cref="KeyCombo"/>) pro <see cref="KeyCode"/> do SharpHook.</summary>
    private static bool TryMapKeyCode(string key, out KeyCode keyCode)
    {
        if (key.Length == 1 && char.IsAsciiLetterOrDigit(key[0]))
        {
            var suffix = char.ToUpperInvariant(key[0]);
            return Enum.TryParse($"Vc{suffix}", out keyCode);
        }

        if (key.Length is 2 or 3 && key[0] is 'F' or 'f' && int.TryParse(key.AsSpan(1), out var fnNumber) && fnNumber is >= 1 and <= 12)
        {
            return Enum.TryParse($"VcF{fnNumber}", out keyCode);
        }

        return Enum.TryParse($"Vc{key}", ignoreCase: true, out keyCode);
    }

    private static void Log(string message)
    {
        var line = $"[GlobalHotkeyService] {message}";
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
