namespace FMLab.QuickNote.Core.Notes;

public interface INoteRepository
{
    Note Create(string? title, string content);

    void Update(Note note);

    Note? GetById(Guid id);

    /// <summary>Most recently updated note with <see cref="Note.CompletedAt"/> still null, if any.</summary>
    Note? GetActiveDraft();

    /// <summary>All notes, active and completed, ordered by <see cref="Note.UpdatedAt"/> descending.</summary>
    IReadOnlyList<Note> GetHistory();

    void Complete(Guid id);

    /// <summary>Reverte uma nota concluída de volta para ativa (limpa <see cref="Note.CompletedAt"/>).</summary>
    void Reactivate(Guid id);

    /// <summary>
    /// Adiciona um comentário à nota. Valida <paramref name="text"/> (não vazio depois de
    /// aparado, sem quebra de linha, até <see cref="Comment.MaxLength"/> caracteres) antes de
    /// gravar; lança <see cref="ArgumentException"/> se a validação falhar.
    /// </summary>
    void AddComment(Guid noteId, string text);
}
