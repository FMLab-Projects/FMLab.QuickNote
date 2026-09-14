namespace FMLab.QuickNote.Core.Notes;

/// <summary>
/// Comentário curto anexado a uma nota. Sem <c>Id</c> nem edição própria — só é possível
/// adicionar (ver <see cref="INoteRepository.AddComment"/>); <see cref="CreatedAt"/> só serve
/// pra ordenação/organização, não é editável pelo usuário.
/// </summary>
public sealed record Comment(DateTimeOffset CreatedAt, string Text)
{
    public const int MaxLength = 140;
}
