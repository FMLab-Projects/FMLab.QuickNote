using System.IO.Pipes;

namespace FMLab.QuickNote.Core.Ipc;

/// <summary>Sends a single command to whichever process is running <see cref="CommandPipeServer"/>.</summary>
public static class CommandPipeClient
{
    public static bool TrySend(string pipeName, AppCommand command, int timeoutMilliseconds = 500)
    {
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);

        try
        {
            client.Connect(timeoutMilliseconds);
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }

        using var writer = new StreamWriter(client) { AutoFlush = true };
        writer.WriteLine(AppCommandNames.ToWireName(command));
        return true;
    }
}
