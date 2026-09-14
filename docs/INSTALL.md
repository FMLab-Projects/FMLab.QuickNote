# Instalação e permissões por plataforma

Este documento cobre como publicar e instalar o FMLab.QuickNote como um
binário standalone (self-contained, arquivo único) e as permissões que cada
sistema operacional exige para o app funcionar por completo — em especial o
atalho global de teclado (hook via SharpHook/`libuiohook`). Para rodar a
partir do código-fonte durante o desenvolvimento, veja o `README.md`.

## Publicando

```
./publish.ps1          # Windows, todos os RIDs
./publish.sh           # Linux/macOS, todos os RIDs
./publish.sh win-x64    # ou só um RID específico
```

Gera um executável único, self-contained (não precisa do runtime .NET
instalado), por RID em `dist/<rid>/`:

| RID | Binário |
|---|---|
| `win-x64` | `dist/win-x64/FMLab.QuickNote.App.exe` |
| `linux-x64` | `dist/linux-x64/FMLab.QuickNote.App` |
| `osx-x64` | `dist/osx-x64/FMLab.QuickNote.App` |
| `osx-arm64` | `dist/osx-arm64/FMLab.QuickNote.App` |

> RIDs `linux-x64`/`osx-x64`/`osx-arm64` foram publicados e validados apenas
> por cross-compilation nesta rodada (build de máquina Windows) — o binário
> resultante não foi executado fisicamente em Linux ou macOS. Mesma ressalva
> já registrada na [ADR 0001](adr/0001-hotkey-global-linux.md) para os spikes
> da Fase 1: alguém precisa confirmar manualmente numa máquina real antes de
> considerar essas plataformas "prontas para uso".

## Windows

- Duas opções, ambas publicadas nos GitHub Releases:
  - **Instalador** (`FMLab.QuickNote-<versão>-win-x64-Setup.exe`, gerado via
    Inno Setup — script em [`installer/windows/FMLab.QuickNote.iss`](../installer/windows/FMLab.QuickNote.iss)):
    cria atalhos no Menu Iniciar/Desktop e um desinstalador. Não exige
    admin — instala por padrão em `%LOCALAPPDATA%\Programs`, com opção de
    elevar para Program Files.
  - **Portátil**: baixe/copie `FMLab.QuickNote.App.exe` (do zip `win-x64`) e
    rode diretamente, sem instalar nada.
- Como nenhum dos dois é assinado digitalmente, o SmartScreen pode alertar
  no primeiro clique duplo ("Windows protegeu seu PC"); use "Mais
  informações" → "Executar assim mesmo". Assinatura de código fica fora do
  escopo da v1.
- Hotkey global e `TrayIcon` funcionam sem nenhuma permissão adicional
  (validado na Fase 1 — ver ADR 0001).
- **Autostart**: tela de Configurações → "Iniciar com o sistema operacional"
  grava em `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` (não precisa
  de administrador).

## Linux

- Dê permissão de execução e rode: `chmod +x FMLab.QuickNote.App && ./FMLab.QuickNote.App`.
- **Hook global de teclado**: o binário `libuiohook` embutido precisa de
  acesso a `/dev/input` para capturar teclas fora da janela do app. Se a sua
  distro não concede isso por padrão, siga a Fase 1 / ADR 0001:
  - X11: normalmente já funciona sem configuração extra.
  - Wayland: o backend nativo via `libinput` (SharpHook 8+) costuma exigir
    uma regra `udev` (ex.: `KERNEL=="event*", SUBSYSTEM=="input", MODE="0660", GROUP="input"`)
    ou rodar como parte de uma sessão que já tenha esse acesso liberado.
  - Se o hook global não iniciar, o app continua funcionando: o erro é
    logado (`[GlobalHotkeyService] Hotkey global indisponível...`) e a
    bandeja/CLI (`--new-note`, `--edit-draft`, `--open-history`, `--toggle`)
    seguem operando normalmente — esse é o Plano B documentado na ADR 0001.
- **Autostart**: tela de Configurações → "Iniciar com o sistema operacional"
  escreve `~/.config/autostart/fmlab-quicknote.desktop`, reconhecido pelas
  principais DEs (GNOME, KDE, XFCE) via a spec XDG Autostart.

## macOS

- Dê permissão de execução e rode: `chmod +x FMLab.QuickNote.App && ./FMLab.QuickNote.App`.
- Como o binário não é assinado/notarizado, o Gatekeeper deve bloquear a
  primeira execução ("não pôde ser aberto porque o desenvolvedor não pôde
  ser verificado"); libere em Ajustes do Sistema → Privacidade e Segurança →
  "Abrir Assim Mesmo". Assinatura/notarização fica fora do escopo da v1.
- **Hook global de teclado**: o macOS exige permissão de **Acessibilidade**
  para qualquer app capturar eventos de teclado fora da própria janela.
  Vá em Ajustes do Sistema → Privacidade e Segurança → Acessibilidade e
  habilite o FMLab.QuickNote. Sem essa permissão, o hook global falha ao
  iniciar (mensagem logada) mas o app continua operável pela bandeja.
- **Autostart**: tela de Configurações → "Iniciar com o sistema operacional"
  grava um LaunchAgent em `~/Library/LaunchAgents/dev.fmlab.quicknote.plist`
  com `RunAtLoad`.

## Ícone da aplicação

Fontes em [`assets/icons/`](../assets/icons/): `icon.ico` (Windows, embutido
no executável via `ApplicationIcon` no `.csproj`), `icon.icns` (macOS) e
`icon-256.png` (Linux — referenciar em `Icon=` num `.desktop`/gerenciador de
pacotes, se algum empacotamento nativo for criado no futuro). O ícone da
bandeja em si continua gerado em runtime (`App.axaml.cs`, `CreateIcon`),
sem depender desses arquivos.
