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
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    _pipeName, PipeDirection.In, maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(token).ConfigureAwait(false);

                using var reader = new StreamReader(server);
                var line = await reader.ReadLineAsync(token).ConfigureAwait(false);
                if (line is not null && AppCommandNames.TryParse(line, out var command))
                {
                    CommandReceived?.Invoke(command);
                }
            }
            catch (OperationCanceledException)
            {
                // Shutting down.
            }
            catch (IOException)
            {
                // Client went away mid-connect/read; keep serving future connections.
            }
        }
    }

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
