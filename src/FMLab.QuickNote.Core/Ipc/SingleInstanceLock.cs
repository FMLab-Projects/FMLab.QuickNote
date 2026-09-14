namespace FMLab.QuickNote.Core.Ipc;

/// <summary>
/// Single-instance check based on an exclusively-held lock file (rather than a named OS mutex)
/// to stay consistent with the rest of the app's "plain files, no OS-specific primitives"
/// approach. Whichever process manages to open the file with <see cref="FileShare.None"/> is the
/// primary instance; holding the handle open for the process lifetime is what keeps the lock.
/// </summary>
public sealed class SingleInstanceLock : IDisposable
{
    private FileStream? _stream;

    private SingleInstanceLock(FileStream? stream) => _stream = stream;

    public bool IsPrimaryInstance => _stream is not null;

    public static SingleInstanceLock TryAcquire(string lockFilePath)
    {
        var directory = Path.GetDirectoryName(lockFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            var stream = new FileStream(lockFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            return new SingleInstanceLock(stream);
        }
        catch (IOException)
        {
            return new SingleInstanceLock(null);
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }
}
