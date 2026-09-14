using FMLab.QuickNote.Core.Shortcuts;

namespace FMLab.QuickNote.Tests.Shortcuts;

public sealed class ShortcutConflictDetectorTests
{
    [Fact]
    public void No_conflicts_among_the_defaults()
    {
        Assert.False(ShortcutConflictDetector.HasConflicts(ShortcutDefaults.All));
    }

    [Fact]
    public void Detects_conflict_between_two_global_actions_sharing_a_combo()
    {
        var combo = new KeyCombo(ShortcutModifiers.Control | ShortcutModifiers.Alt, "N");
        var bindings = new[]
        {
            new ShortcutBinding(ShortcutAction.NewNote, combo),
            new ShortcutBinding(ShortcutAction.EditDraft, combo),
        };

        var conflicts = ShortcutConflictDetector.FindConflicts(bindings);

        var conflict = Assert.Single(conflicts);
        Assert.Equal(combo, conflict.KeyCombo);
    }

    [Fact]
    public void Detects_conflict_between_two_local_actions_sharing_a_combo()
    {
        var combo = new KeyCombo(ShortcutModifiers.Control, "Enter");
        var bindings = new[]
        {
            new ShortcutBinding(ShortcutAction.Complete, combo),
            new ShortcutBinding(ShortcutAction.Close, combo),
        };

        Assert.True(ShortcutConflictDetector.HasConflicts(bindings));
    }

    [Fact]
    public void Does_not_flag_a_global_and_a_local_action_sharing_a_combo()
    {
        var combo = new KeyCombo(ShortcutModifiers.Control, "H");
        var bindings = new[]
        {
            new ShortcutBinding(ShortcutAction.OpenHistory, combo), // global
            new ShortcutBinding(ShortcutAction.Close, combo), // local
        };

        Assert.False(ShortcutConflictDetector.HasConflicts(bindings));
    }

    [Fact]
    public void Different_combos_never_conflict()
    {
        var bindings = new[]
        {
            new ShortcutBinding(ShortcutAction.NewNote, new KeyCombo(ShortcutModifiers.Control | ShortcutModifiers.Alt, "N")),
            new ShortcutBinding(ShortcutAction.EditDraft, new KeyCombo(ShortcutModifiers.Control | ShortcutModifiers.Alt, "Q")),
        };

        Assert.False(ShortcutConflictDetector.HasConflicts(bindings));
    }
}
