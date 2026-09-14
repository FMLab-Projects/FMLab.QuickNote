namespace FMLab.QuickNote.Core.Ipc;

/// <summary>Commands a second process can forward to the already-running instance.</summary>
public enum AppCommand
{
    NewNote,
    EditDraft,
    OpenHistory,
    Toggle,
}
