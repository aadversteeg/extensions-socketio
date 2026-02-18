using System;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;

namespace IntegrationTests.Extensions.SocketIO.Client;

/// <summary>
/// Base class for Engine.IO v3 (Socket.IO v2) integration tests.
/// </summary>
[Collection("SocketIO V3 Integration Tests")]
public abstract class V3IntegrationTestBase
{
    protected SocketIOV2ServerFixture Fixture { get; }

    protected V3IntegrationTestBase(SocketIOV2ServerFixture fixture)
    {
        Fixture = fixture;
    }

    protected Uri ServerUri => Fixture.ServerUri;

    protected bool ShouldSkip => !Fixture.IsNodeAvailable;

    protected SocketIOClient CreateClient(SocketIOClientOptions? options = null)
    {
        return new SocketIOClient(
            ServerUri,
            options ?? new SocketIOClientOptions
            {
                EIO = EngineIOVersion.V3,
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
                EIO = EngineIOVersion.V3,
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
                EIO = EngineIOVersion.V3,
                Reconnection = false,
                AutoUpgrade = false,
                Transport = TransportProtocol.WebSocket,
            });
    }
}
