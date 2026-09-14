# FMLab.QuickNote

Aplicação .NET multiplataforma (Windows/Linux/macOS) para captura rápida de
notas via atalho de teclado global — abre com um atalho, edita, fecha com
outro atalho, salva automaticamente. Fica residente na bandeja do sistema.

## Rodando localmente

```
dotnet build
dotnet test
dotnet run --project src/FMLab.QuickNote.App
```

Ou use `build.ps1` (Windows) / `build.sh` (Linux/macOS) para build + testes.

## Uso

O app fica na bandeja do sistema; a janela principal (`NoteWindow`) abre sem
barra de título, sempre no topo, e some (`Hide`) em vez de fechar o processo.

Atalhos padrão (customizáveis na tela de Configurações, acessível pelo menu
da bandeja):

| Ação | Contexto | Atalho padrão |
|---|---|---|
| Nova nota | Global | `Ctrl+Alt+N` |
| Editar rascunho anterior | Global | `Ctrl+Alt+Q` |
| Abrir histórico | Global | `Ctrl+Alt+H` |
| Fechar janela (salva) | Janela ativa | `Esc` |
| Concluir nota atual | Janela ativa | `Ctrl+Enter` |
| Indentar / recuar item de lista | Editor | `Tab` / `Shift+Tab` |
| Alternar checkbox do item atual | Editor | `Ctrl+Shift+C` |

O menu da bandeja também dá acesso a Nova nota, Editar rascunho, Histórico,
Configurações e Sair (o único jeito de encerrar o processo de verdade).

O editor é texto simples estruturado (sem rich text): `- ` inicia uma lista
com marcador (`Enter` continua a lista, linha vazia sai dela) e `- [ ] texto`
vira um item de checkbox (renderizado com glyph `☐`/`☑` no editor, salvo
como `[ ]`/`[x]` em disco). A fonte do editor é monoespaçada (estilo
terminal, com fallback `Cascadia Mono`/`Consolas`/`Menlo`/`Monospace`
conforme a plataforma) e é configurável — família e tamanho — na tela de
Configurações, junto com uma prévia ao vivo.

A tela de histórico (`Ctrl+Alt+H`) lista as notas (todas/ativas/concluídas),
com ações pra reabrir ou concluir/reativar cada uma direto da lista, um
campo de busca por aproximação (título + conteúdo, tolera acentuação
diferente e pequenos erros de digitação) combinável com o filtro de
status, e um footer com a contagem total de notas/ativas/concluídas.

Cada nota também aceita comentários curtos (até 140 caracteres, sem
formatação): a seção "Comentários" no rodapé da janela principal lista os
já adicionados e tem um campo pra adicionar um novo (`Enter` envia). A
nota precisa já existir pra receber um comentário — o app salva o que
estiver no editor antes de gravar o comentário. A quantidade de
comentários aparece no histórico junto com o status da nota.

## Publicando um binário standalone

Veja [`docs/INSTALL.md`](docs/INSTALL.md): `publish.ps1`/`publish.sh` geram
um executável self-contained (arquivo único) por plataforma, e o documento
cobre as permissões que cada SO exige (hotkey global, autostart).

## Onde os dados ficam

- Notas: `%AppData%/FMLab.QuickNote/notes/*.txt` no Windows (pasta de dados
  do usuário equivalente em Linux/macOS) — um arquivo de texto simples por
  nota, com um cabeçalho `Chave: valor` e o conteúdo cru depois de um `---`.
- Posição/tamanho da janela: `.../FMLab.QuickNote/window-placement.txt`.
- Atalhos customizados: `.../FMLab.QuickNote/shortcuts.txt`.
- Fonte do editor: `.../FMLab.QuickNote/font.txt`.

Todos em texto simples, auditáveis/editáveis à mão se precisar.

## Decisões de arquitetura (ADRs)

- [`docs/adr/0001-hotkey-global-linux.md`](docs/adr/0001-hotkey-global-linux.md) —
  resultado do spike de viabilidade da Fase 1 (hotkey global via SharpHook,
  `TrayIcon` e janela borderless/`Topmost`) e o plano B para hotkey global
  no Linux caso o hook não consiga acesso a `/dev/input`.
