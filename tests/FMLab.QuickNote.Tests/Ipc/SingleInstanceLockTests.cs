using FMLab.QuickNote.Core.Ipc;

namespace FMLab.QuickNote.Tests.Ipc;

public sealed class SingleInstanceLockTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), "FMLab.QuickNote.Tests", $"{Guid.NewGuid():N}.lock");

    [Fact]
    public void First_acquire_becomes_primary_instance()
    {
        using var first = SingleInstanceLock.TryAcquire(_path);

        Assert.True(first.IsPrimaryInstance);
    }

    [Fact]
    public void Second_acquire_while_first_is_held_is_not_primary()
    {
        using var first = SingleInstanceLock.TryAcquire(_path);
        using var second = SingleInstanceLock.TryAcquire(_path);

        Assert.True(first.IsPrimaryInstance);
        Assert.False(second.IsPrimaryInstance);
    }

    [Fact]
    public void Acquire_succeeds_again_after_previous_lock_disposed()
    {
        var first = SingleInstanceLock.TryAcquire(_path);
        Assert.True(first.IsPrimaryInstance);
        first.Dispose();

        using var second = SingleInstanceLock.TryAcquire(_path);
        Assert.True(second.IsPrimaryInstance);
    }

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
