using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO.Server;

namespace IntegrationTests.Extensions.SocketIO.Server;

public abstract class ServerIntegrationTestBase : IAsyncLifetime
{
    private ServerTestHelper? _serverHelper;
    private readonly NodeClientRunner _clientRunner = new();

    protected bool ShouldSkip => !_clientRunner.IsNodeAvailable;

    protected ISocketIOServer Server => _serverHelper!.Server;
    protected int Port => _serverHelper!.Port;
    protected Uri ServerUri => _serverHelper!.ServerUri;

    protected virtual Action<SocketIOServerOptions>? ConfigureOptions => null;

    protected abstract void ConfigureServer(ISocketIOServer server);

    public async Task InitializeAsync()
    {
        if (ShouldSkip) return;

        _serverHelper = await ServerTestHelper.CreateAndStartAsync(
            ConfigureOptions,
            ConfigureServer);
    }

    public async Task DisposeAsync()
    {
        _clientRunner.Dispose();
        if (_serverHelper != null)
        {
            await _serverHelper.DisposeAsync();
        }
    }

    protected async Task<IReadOnlyList<NodeClientMessage>> RunClientAsync(
        string scenario, object? args = null, TimeSpan? timeout = null)
    {
        return await _clientRunner.RunScenarioAsync(Port, scenario, args, timeout);
    }

    protected ClientSession StartClient(string scenario, object? args = null)
    {
        return _clientRunner.StartScenario(Port, scenario, args);
    }

    protected static NodeClientMessage? FindMessage(
        IReadOnlyList<NodeClientMessage> messages, string type, string? name = null)
    {
        return messages.FirstOrDefault(m =>
            m.Type == type && (name == null || m.Name == name));
    }

    protected static IReadOnlyList<NodeClientMessage> FindMessages(
        IReadOnlyList<NodeClientMessage> messages, string type, string? name = null)
    {
        return messages.Where(m =>
            m.Type == type && (name == null || m.Name == name)).ToList();
    }

    protected static async Task<T> WaitForAsync<T>(
        TaskCompletionSource<T> tcs, int timeoutMs = 5000)
    {
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
        completed.Should().Be(tcs.Task, "expected operation should complete within timeout");
        return await tcs.Task;
    }

    protected static async Task WaitForCountAsync<T>(
        List<T> list, int expectedCount, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);
        while (list.Count < expectedCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
        list.Count.Should().BeGreaterThanOrEqualTo(expectedCount);
    }
}
