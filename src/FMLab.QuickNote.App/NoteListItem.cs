using FMLab.QuickNote.Core.Notes;

namespace FMLab.QuickNote.App;

/// <summary>Envelope de exibição de uma <see cref="Note"/> na lista de histórico.</summary>
public sealed class NoteListItem(Note note)
{
    public Note Note { get; } = note;

    public string DisplayTitle => NoteDisplay.GetDisplayTitle(Note);

    public string StatusLine => Note.IsActive
        ? $"Ativa · atualizada em {Note.UpdatedAt.LocalDateTime:dd/MM/yyyy HH:mm}"
        : $"Concluída · atualizada em {Note.UpdatedAt.LocalDateTime:dd/MM/yyyy HH:mm}";

    public string ToggleActionLabel => Note.IsActive ? "Concluir" : "Reativar";
}
