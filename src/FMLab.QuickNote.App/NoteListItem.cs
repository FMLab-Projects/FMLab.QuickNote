using FMLab.QuickNote.Core.Notes;

namespace FMLab.QuickNote.App;

/// <summary>Envelope de exibição de uma <see cref="Note"/> na lista de histórico.</summary>
public sealed class NoteListItem(Note note, bool isDeletePending = false)
{
    public Note Note { get; } = note;

    /// <summary>Só notas concluídas (arquivadas) podem ser excluídas.</summary>
    public bool CanDelete => !Note.IsActive;

    public string DeleteActionLabel => isDeletePending ? "Confirmar?" : "Excluir";

    public string DisplayTitle => NoteDisplay.GetDisplayTitle(Note);

    public string StatusLine =>
        (Note.IsActive
            ? $"Ativa · atualizada em {Note.UpdatedAt.LocalDateTime:dd/MM/yyyy HH:mm}"
            : $"Concluída · atualizada em {Note.UpdatedAt.LocalDateTime:dd/MM/yyyy HH:mm}")
        + (Note.Comments.Count > 0 ? $" · {Note.Comments.Count} comentários" : string.Empty);

    public string ToggleActionLabel => Note.IsActive ? "Concluir" : "Reativar";
}
