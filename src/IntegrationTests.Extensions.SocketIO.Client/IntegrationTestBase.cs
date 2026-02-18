using System;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;

namespace IntegrationTests.Extensions.SocketIO.Client;

/// <summary>
/// Base class for integration tests providing access to the shared test server.
/// </summary>
[Collection("SocketIO Integration Tests")]
public abstract class IntegrationTestBase
{
    protected SocketIOServerFixture Fixture { get; }

    protected IntegrationTestBase(SocketIOServerFixture fixture)
    {
        Fixture = fixture;
    }

    protected Uri ServerUri => Fixture.ServerUri;

    protected string SkipReason => Fixture.IsNodeAvailable
        ? string.Empty
        : "Node.js is not available on this system";

    protected bool ShouldSkip => !Fixture.IsNodeAvailable;

    protected SocketIOClient CreateClient(SocketIOClientOptions? options = null)
    {
        return new SocketIOClient(
            ServerUri,
            options ?? new SocketIOClientOptions
            {
                Reconnection = false,
                AutoUpgrade = false,
                Transport = TransportProtocol.WebSocket,
            });
    }

    protected SocketIOClient CreatePollingClient(SocketIOClientOptions? options = null)
    {
        return new SocketIOClient(
            ServerUri,
            options ?? new SocketIOClientOptions
            {
                Reconnection = false,
                AutoUpgrade = false,
                Transport = TransportProtocol.Polling,
            });
    }

    protected SocketIOClient CreateClientForNamespace(string namespacePath, SocketIOClientOptions? options = null)
    {
        var uri = new Uri(ServerUri, namespacePath);
        return new SocketIOClient(
            uri,
            options ?? new SocketIOClientOptions
            {
                Reconnection = false,
                AutoUpgrade = false,
                Transport = TransportProtocol.WebSocket,
            });
    }
}
