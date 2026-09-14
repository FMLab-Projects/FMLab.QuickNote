namespace FMLab.QuickNote.Core.Ipc;

/// <summary>
/// Wire representation of <see cref="AppCommand"/> shared by the named-pipe protocol and the
/// CLI flags a second process (or a Linux keyboard shortcut, per ADR 0001) can pass in.
/// </summary>
public static class AppCommandNames
{
    public const string NewNote = "new-note";
    public const string EditDraft = "edit-draft";
    public const string OpenHistory = "open-history";
    public const string Toggle = "toggle";

    public static string ToWireName(AppCommand command) => command switch
    {
        AppCommand.NewNote => NewNote,
        AppCommand.EditDraft => EditDraft,
        AppCommand.OpenHistory => OpenHistory,
        AppCommand.Toggle => Toggle,
        _ => throw new ArgumentOutOfRangeException(nameof(command), command, null),
    };

    public static bool TryParse(string? text, out AppCommand command)
    {
        switch (text)
        {
            case NewNote:
                command = AppCommand.NewNote;
                return true;
            case EditDraft:
                command = AppCommand.EditDraft;
                return true;
            case OpenHistory:
                command = AppCommand.OpenHistory;
                return true;
            case Toggle:
                command = AppCommand.Toggle;
                return true;
            default:
                command = default;
                return false;
        }
    }
}
