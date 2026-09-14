namespace FMLab.QuickNote.Core.Notes;

public enum HistoryFilter
{
    All,
    Active,
    Completed,
}

public static class HistoryFilterExtensions
{
    public static IReadOnlyList<Note> Apply(this HistoryFilter filter, IEnumerable<Note> notes) => filter switch
    {
        HistoryFilter.Active => notes.Where(n => n.IsActive).ToList(),
        HistoryFilter.Completed => notes.Where(n => !n.IsActive).ToList(),
        _ => notes.ToList(),
    };
}
