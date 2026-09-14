# FMLab.QuickNote

Aplicação .NET multiplataforma (Windows/Linux/macOS) para captura rápida de
notas via atalho de teclado global. Veja `TASKS.md` para o mapeamento
completo de decisões de produto e fases de implementação.

## Rodando localmente

```
dotnet build
dotnet test
dotnet run --project src/FMLab.QuickNote.App
```

Ou use `build.ps1` (Windows) / `build.sh` (Linux/macOS) para build + testes.

## Decisões de arquitetura (ADRs)

- [`docs/adr/0001-hotkey-global-linux.md`](docs/adr/0001-hotkey-global-linux.md) —
  resultado do spike de viabilidade da Fase 1 (hotkey global via SharpHook,
  `TrayIcon` e janela borderless/`Topmost`) e o plano B para hotkey global
  no Linux caso o hook não consiga acesso a `/dev/input`.