using FMLab.QuickNote.Core.Ipc;

namespace FMLab.QuickNote.Tests.Ipc;

public sealed class CommandPipeTests
{
    private static string NewPipeName() => $"FMLab.QuickNote.Tests.{Guid.NewGuid():N}";

    [Fact]
    public async Task Server_receives_command_sent_by_client()
    {
        var pipeName = NewPipeName();
        using var server = new CommandPipeServer(pipeName);
        var received = new TaskCompletionSource<AppCommand>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.CommandReceived += command => received.TrySetResult(command);
        server.Start();

        var sent = CommandPipeClient.TrySend(pipeName, AppCommand.NewNote, timeoutMilliseconds: 2000);

        Assert.True(sent);
        var command = await received.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(AppCommand.NewNote, command);
    }

    [Fact]
    public async Task Server_keeps_listening_after_handling_a_command()
    {
        var pipeName = NewPipeName();
        using var server = new CommandPipeServer(pipeName);
        var receivedCommands = new List<AppCommand>();
        var receivedSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.CommandReceived += command =>
        {
            receivedCommands.Add(command);
            if (receivedCommands.Count == 2)
            {
                receivedSecond.TrySetResult();
            }
        };
        server.Start();

        Assert.True(CommandPipeClient.TrySend(pipeName, AppCommand.NewNote, timeoutMilliseconds: 2000));
        Assert.True(CommandPipeClient.TrySend(pipeName, AppCommand.Toggle, timeoutMilliseconds: 2000));

        await receivedSecond.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal([AppCommand.NewNote, AppCommand.Toggle], receivedCommands);
    }

    [Fact]
    public void TrySend_returns_false_when_no_server_listening()
    {
        var sent = CommandPipeClient.TrySend(NewPipeName(), AppCommand.Toggle, timeoutMilliseconds: 200);

        Assert.False(sent);
    }
}
