namespace FMLab.QuickNote.Core.Notes;

/// <summary>
/// Orquestra o ciclo de vida de uma nota sendo editada (criar/atualizar/concluir), sem
/// depender de UI: recebe título/conteúdo como texto puro e decide quando criar, atualizar
/// ou não gravar nada. A janela apenas traduz seus controles para essas chamadas.
/// </summary>
public sealed class NoteEditingSession
{
    private readonly INoteRepository _repository;

    public NoteEditingSession(INoteRepository repository) => _repository = repository;

    /// <summary>Id da nota atualmente carregada nesta sessão, ou null se ainda não foi salva.</summary>
    public Guid? CurrentNoteId { get; private set; }

    /// <summary>
    /// Salva o conteúdo atual (cria a nota se ainda não existir, atualiza caso contrário).
    /// Não grava nada se título e conteúdo estiverem ambos vazios. Retorna se algo foi salvo.
    /// </summary>
    public bool SaveIfNeeded(string? title, string content)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var normalizedTitle = NormalizeTitle(title);

        if (CurrentNoteId is { } id && _repository.GetById(id) is { } existing)
        {
            existing.Title = normalizedTitle;
            existing.Content = content;
            _repository.Update(existing);
            return true;
        }

        var created = _repository.Create(normalizedTitle, content);
        CurrentNoteId = created.Id;
        return true;
    }

    /// <summary>Desvincula a sessão de qualquer nota (próximo save cria uma nova).</summary>
    public void LoadBlank() => CurrentNoteId = null;

    /// <summary>Vincula a sessão à nota informada (próximo save atualiza essa nota).</summary>
    public void LoadNote(Note note) => CurrentNoteId = note.Id;

    /// <summary>
    /// Carrega a última nota ativa (rascunho) do repositório, se houver, e vincula a sessão a
    /// ela. Retorna null (e deixa a sessão em branco) se não houver rascunho.
    /// </summary>
    public Note? LoadPreviousDraft()
    {
        var draft = _repository.GetActiveDraft();
        CurrentNoteId = draft?.Id;
        return draft;
    }

    /// <summary>
    /// Salva o conteúdo atual (se houver), marca a nota como concluída e desvincula a sessão
    /// (para que a chamada seguinte comece uma nota em branco).
    /// </summary>
    public void CompleteCurrent(string? title, string content)
    {
        SaveIfNeeded(title, content);
        if (CurrentNoteId is { } id)
        {
            _repository.Complete(id);
        }

        LoadBlank();
    }

    private static string? NormalizeTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? null : title;
}
