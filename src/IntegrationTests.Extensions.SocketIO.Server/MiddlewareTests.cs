using System;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO.Server;

namespace IntegrationTests.Extensions.SocketIO.Server;

public class MiddlewareAllowTests : ServerIntegrationTestBase
{
    private bool _middlewareExecuted;
    private bool _connectionHandlerExecuted;

    protected override void ConfigureServer(ISocketIOServer server)
    {
        server.Default.Use(async (socket, next) =>
        {
            _middlewareExecuted = true;
            await next();
        });

        server.OnConnection(socket =>
        {
            _connectionHandlerExecuted = true;
            return Task.CompletedTask;
        });
    }

    [Fact(DisplayName = "SMW-001: Middleware calls next — connection succeeds")]
    public async Task SMW001()
    {
        if (ShouldSkip) return;

        var messages = await RunClientAsync("middleware-connect", new { auth = new { token = "valid" } });

        var connected = FindMessage(messages, "connected");
        connected.Should().NotBeNull();

        await Task.Delay(300);

        _middlewareExecuted.Should().BeTrue();
        _connectionHandlerExecuted.Should().BeTrue();
    }
}

public class MiddlewareRejectTests : ServerIntegrationTestBase
{
    private bool _connectionHandlerExecuted;

    protected override void ConfigureServer(ISocketIOServer server)
    {
        server.Default.Use((socket, next) =>
        {
            // Do NOT call next — reject the connection
            return Task.CompletedTask;
        });

        server.OnConnection(socket =>
        {
            _connectionHandlerExecuted = true;
            return Task.CompletedTask;
        });
    }

    [Fact(DisplayName = "SMW-002: Middleware does not call next — connection rejected")]
    public async Task SMW002()
    {
        if (ShouldSkip) return;

        var messages = await RunClientAsync("middleware-connect", new
        {
            auth = new { token = "invalid" },
        });
        await Task.Delay(500);

        // The connection handler should NOT have been called
        _connectionHandlerExecuted.Should().BeFalse();

        // Client should have received a connect_error or timed out
        var connectError = FindMessage(messages, "connect-error");
        var connected = FindMessage(messages, "connected");
        // Either we got an explicit error, or connection never succeeded
        if (connectError != null)
        {
            connectError.Should().NotBeNull();
        }
        else
        {
            connected.Should().BeNull("connection should not succeed when middleware rejects");
        }
    }
}
