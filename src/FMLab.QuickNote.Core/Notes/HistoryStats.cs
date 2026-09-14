namespace FMLab.QuickNote.Core.Notes;

/// <summary>
/// Contagens usadas no footer da tela de histórico — sempre sobre o total de notas, independente
/// do filtro de status (todas/ativas/concluídas) selecionado na lista.
/// </summary>
public readonly record struct HistoryStats(int Total, int Active, int Completed)
{
    public static HistoryStats From(IReadOnlyList<Note> notes)
    {
        var active = notes.Count(n => n.IsActive);
        return new HistoryStats(notes.Count, active, notes.Count - active);
    }

    public override string ToString() => $"{Total} notas · {Active} ativas · {Completed} concluídas";
}
