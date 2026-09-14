using System.IO.Pipes;

namespace FMLab.QuickNote.Core.Ipc;

/// <summary>
/// Listens on a named pipe (backed by a Unix domain socket on Linux/macOS) for one command per
/// connection, raising <see cref="CommandReceived"/> for each one understood. Runs its own accept
/// loop so it keeps serving further commands from later processes for as long as this instance lives.
/// </summary>
public sealed class CommandPipeServer : IDisposable
{
    private readonly string _pipeName;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public CommandPipeServer(string pipeName) => _pipeName = pipeName;

    public event Action<AppCommand>? CommandReceived;

    public void Start() => _loop = RunAsync(_cts.Token);

    private async Task RunAsync(CancellationToken token)
    {
        // Commands are still handled strictly one at a time, in connection order (a single
        // CommandReceived invocation in flight at any point) — only the *accept* step is
        // pipelined: the next instance starts listening before the current connection is read,
        // so there is never a gap with nothing listening. Windows tolerates such a gap (a
        // connecting client just waits); the Unix domain socket backing this on Linux/macOS
        // does not — a client that connects during the gap is silently dropped, which used to
        // make this loop miss commands sent in quick succession there.
        NamedPipeServerStream? next = CreateServer();
        Task nextConnect = next.WaitForConnectionAsync(token);

        try
        {
            while (!token.IsCancellationRequested)
            {
                var server = next;
                try
                {
                    await nextConnect.ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // Couldn't accept this instance; drop it and try a fresh one.
                    server.Dispose();
                    next = CreateServer();
                    nextConnect = next.WaitForConnectionAsync(token);
                    continue;
                }

                next = CreateServer();
                nextConnect = next.WaitForConnectionAsync(token);

                using (server)
                {
                    try
                    {
                        using var reader = new StreamReader(server);
                        var line = await reader.ReadLineAsync(token).ConfigureAwait(false);
                        if (line is not null && AppCommandNames.TryParse(line, out var command))
                        {
                            CommandReceived?.Invoke(command);
                        }
                    }
                    catch (IOException)
                    {
                        // Client went away mid-read; keep serving future connections.
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        finally
        {
            next.Dispose();
        }
    }

    private NamedPipeServerStream CreateServer() => new(
        _pipeName, PipeDirection.In, maxNumberOfServerInstances: 2,
        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _loop?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // Expected: the accept loop observed the cancellation.
        }

        _cts.Dispose();
    }
}
