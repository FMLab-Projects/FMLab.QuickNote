# ADR 0001 — Hotkey global no Linux (X11/Wayland) e resultado dos spikes da Fase 1

Status: aceito
Data: 2026-09-14

## Contexto

A Fase 0 (`TASKS.md`) registrou um risco conhecido a validar cedo: o hook
global de teclado (`SharpHook`, que embrulha a `libuiohook`) poderia não
funcionar em compositores Wayland puros, já que a suposição inicial era de
dependência de X11.

A Fase 1 existe para des-riscar esse ponto (e outros dois: `TrayIcon` do
Avalonia e janela borderless/`Topmost`) antes de investir nas fases
seguintes.

**Limitação desta rodada de spike**: o ambiente de desenvolvimento usado
para executar os spikes é Windows. Os itens de Linux e macOS abaixo foram
implementados em código e documentados a partir da documentação oficial
upstream (SharpHook e Avalonia), mas **não foram executados fisicamente**
em Linux ou macOS nesta rodada. Antes de marcar esses itens como
concluídos em `TASKS.md`, alguém precisa rodar `dotnet run --project
src/FMLab.QuickNote.App` numa máquina Linux (X11 e, se possível, Wayland)
e numa Mac e confirmar manualmente.

## O que foi validado em Windows

Implementado em `src/FMLab.QuickNote.App`:

- `App.axaml.cs`: `TrayIcon` com ícone gerado em runtime (sem asset externo),
  menu com "Mostrar/Ocultar", "Nova nota" e "Sair".
- `Spikes/GlobalHotkeySpikeService.cs`: hook global via `SharpHook.TaskPoolGlobalHook`,
  detectando `Ctrl+Alt+N` (nova nota) e `Ctrl+Alt+Q` (editar rascunho) — os
  defaults definidos em `TASKS.md`.
- `MainWindow.axaml`/`.cs`: janela `WindowDecorations="None"` + `Topmost="True"`,
  `Esc` esconde a janela, fechar pelo botão do sistema também esconde
  (`Closing` cancelado) em vez de encerrar o processo, foco automático no
  editor ao mostrar.

Testado manualmente rodando o app compilado e simulando as combinações de
teclas (via `SendKeys`/`keybd_event`) com o foco em outras janelas:

- Hook global iniciou sem erro e ambos os atalhos foram detectados
  corretamente mesmo sem a janela do QuickNote estar em foco.
- A janela aparece sem moldura nativa, fica acima de outras janelas
  (inclusive um terminal maximizado) e o toggle mostrar/esconder funciona.
- `Esc` esconde a janela sem encerrar o processo.

### Bug real encontrado e corrigido no spike

O callback do `SharpHook` é despachado em uma thread da thread pool, **não**
na UI thread do Avalonia. Chamar `Window.Show()`/`.Focus()` diretamente
dentro do handler do hook derrubava o processo inteiro com
`InvalidOperationException: The calling thread cannot access this object
because a different thread owns it.`

Correção aplicada em `App.axaml.cs`: os handlers dos eventos do
`GlobalHotkeySpikeService` fazem `Dispatcher.UIThread.Post(...)` antes de
tocar em qualquer objeto de UI. **Esse padrão deve ser seguido em toda
integração futura entre o hook global e a UI** (Fase 7).

## Pesquisa sobre Linux (X11 e Wayland)

Fonte: documentação oficial do SharpHook (`sharphook.tolik.io`, versão
v8.0.0, mesma versão usada no projeto).

- **Wayland tem um backend dedicado** (`libuiohook-wayland.so`, via
  `libinput`), não depende de X11/XWayland para o hook de teclado. A
  suposição original em `TASKS.md` ("depende de X11") estava desatualizada
  para a v8 da lib.
- Um hook **somente de teclado** nem chega a abrir conexão com o
  compositor — isso só é necessário quando eventos de mouse estão
  habilitados (para saber o tamanho da tela).
- **Requer privilégio elevado**: o backend X11/Wayland precisa de acesso a
  `/dev/input` (hook global) e `/dev/uinput` (simulação de eventos, não
  usada nesta fase). Isso não exige rodar como `root`, mas exige uma das
  duas opções:
  - uma regra `udev` liberando acesso ao dispositivo de input para o
    usuário (abordagem recomendada, embora a própria doc do SharpHook
    reconheça que isso "meio que quebra o modelo de segurança do
    Wayland");
  - adicionar o usuário ao grupo `input` (não recomendado).
- Sem uma dessas duas configurações, o hook global **falha ao iniciar**
  (não trava a aplicação — `GlobalHotkeySpikeService.TryStartAsync` já
  captura a exceção e loga, permitindo o app continuar funcionando via
  bandeja).

Sobre o `TrayIcon` do Avalonia no Linux: por padrão, sessões Wayland rodam
a aplicação Avalonia via camada de compatibilidade XWayland (o backend
Wayland nativo é experimental e opt-in a partir do Avalonia 12.1.0). Ou
seja, no caso comum (sem opt-in explícito), `TrayIcon` e o comportamento de
janela devem se aproximar do X11. O `TrayIcon` também depende do desktop
environment suportar `StatusNotifierItem`/`AppIndicator` — em DEs sem esse
suporte o ícone simplesmente não aparece, independentemente de X11/Wayland.

## Decisão

1. **Manter `SharpHook` como biblioteca de hook global** — a limitação real
   no Linux não é "não funciona no Wayland", e sim "precisa de acesso a
   `/dev/input`", o que é resolvível via `udev` na maioria das distros-alvo
   (desktop tradicional) e é uma limitação aceitável para v1.
2. **Plano B, caso o hook global falhe ao iniciar** (ambiente Wayland
   travado, distro imutável, contêiner sem acesso a `/dev/input` etc.):
   expor os comandos (`new-note`, `edit-draft`, `open-history`, `toggle`)
   como argumentos de CLI que se comunicam com a instância principal via o
   IPC por named pipe já planejado para a Fase 3. Assim o usuário pode
   configurar o atalho diretamente no compositor/DE para rodar
   `quicknote --new-note` (por exemplo), sem depender de hook global. Esse
   plano B já era previsto em `TASKS.md`; esta ADR só confirma que ele
   segue necessário como fallback, não como caminho principal.
3. `GlobalHotkeySpikeService.TryStartAsync` estabelece o contrato que a
   Fase 7 deve seguir: falha ao iniciar o hook global é **não-fatal** e
   deve ser logada, mantendo a aplicação operável via bandeja/CLI.

## Pendências antes de fechar a Fase 1 por completo

- [ ] Rodar o app em Linux com sessão X11 e confirmar `TrayIcon` + hotkey
      global funcionando.
- [ ] Rodar o app em Linux com sessão Wayland (GNOME e/ou KDE) e confirmar
      se o hook global inicia sem configuração extra, ou se exige a regra
      `udev` descrita acima; documentar o resultado real aqui.
- [ ] Rodar o app em macOS e confirmar `TrayIcon`, hotkey global (permissão
      de Acessibilidade é esperada) e janela borderless/`Topmost`.
