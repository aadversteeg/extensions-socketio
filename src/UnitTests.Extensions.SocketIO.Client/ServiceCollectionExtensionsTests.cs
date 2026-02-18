using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Serialization.Decapsulation;

namespace UnitTests.Extensions.SocketIO.Client;

public class ServiceCollectionExtensionsTests
{
    private IServiceCollection CaptureServices()
    {
        IServiceCollection? captured = null;
        var client = new SocketIOClient(
            new Uri("http://localhost"),
            new SocketIOClientOptions { Reconnection = false },
            services => { captured = services; });
        client.Dispose();
        return captured!;
    }

    [Fact(DisplayName = "SCE-001: Default services should be registered without errors")]
    public void SCE001()
    {
        var act = () =>
        {
            var client = new SocketIOClient(
                new Uri("http://localhost"),
                new SocketIOClientOptions { Reconnection = false });
            client.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact(DisplayName = "SCE-002: Should register ISerializer")]
    public void SCE002()
    {
        var services = CaptureServices();

        services.Should().Contain(d => d.ServiceType == typeof(ISerializer));
    }

    [Fact(DisplayName = "SCE-003: Should register polling session")]
    public void SCE003()
    {
        var services = CaptureServices();

        services.Where(d =>
            d.IsKeyedService
            && d.ServiceKey != null
            && d.ServiceKey.Equals(TransportProtocol.Polling)
            && d.ServiceType == typeof(ISession))
            .Should().NotBeEmpty();
    }

    [Fact(DisplayName = "SCE-004: Should register websocket session")]
    public void SCE004()
    {
        var services = CaptureServices();

        services.Where(d =>
            d.IsKeyedService
            && d.ServiceKey != null
            && d.ServiceKey.Equals(TransportProtocol.WebSocket)
            && d.ServiceType == typeof(ISession))
            .Should().NotBeEmpty();
    }

    [Fact(DisplayName = "SCE-005: Configure callback should be invoked allowing service inspection")]
    public void SCE005()
    {
        var configureInvoked = false;
        var client = new SocketIOClient(
            new Uri("http://localhost"),
            new SocketIOClientOptions { Reconnection = false },
            services => { configureInvoked = true; });
        client.Dispose();

        configureInvoked.Should().BeTrue();
    }

    [Fact(DisplayName = "SCE-006: Should register IEngineIOMessageAdapterFactory")]
    public void SCE006()
    {
        var services = CaptureServices();

        services.Should().Contain(d => d.ServiceType == typeof(IEngineIOMessageAdapterFactory));
    }

    [Fact(DisplayName = "SCE-007: Should register IStopwatch")]
    public void SCE007()
    {
        var services = CaptureServices();

        services.Should().Contain(d => d.ServiceType == typeof(IStopwatch));
    }

    [Fact(DisplayName = "SCE-008: Should register IRandom")]
    public void SCE008()
    {
        var services = CaptureServices();

        services.Should().Contain(d => d.ServiceType == typeof(IRandom));
    }

    [Fact(DisplayName = "SCE-009: Should register IDelay")]
    public void SCE009()
    {
        var services = CaptureServices();

        services.Should().Contain(d => d.ServiceType == typeof(IDelay));
    }

    [Fact(DisplayName = "SCE-010: Should register IEventRunner")]
    public void SCE010()
    {
        var services = CaptureServices();

        services.Should().Contain(d => d.ServiceType == typeof(IEventRunner));
    }

    [Fact(DisplayName = "SCE-011: Should register IDecapsulable")]
    public void SCE011()
    {
        var services = CaptureServices();

        services.Should().Contain(d => d.ServiceType == typeof(IDecapsulable));
    }
}
