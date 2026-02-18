using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client;
using Ave.Extensions.SocketIO.Client.Observers;
using Ave.Extensions.SocketIO.Client.Session;
using Ave.Extensions.SocketIO.Messages;

namespace UnitTests.Extensions.SocketIO.Client;

public class SocketIOClientTests
{
    private static SocketIOClient CreateClient(
        Uri? uri = null,
        SocketIOClientOptions? options = null,
        Action<IServiceCollection>? configure = null)
    {
        return new SocketIOClient(
            uri ?? new Uri("http://localhost"),
            options ?? new SocketIOClientOptions { Reconnection = false },
            configure);
    }

    private static (Mock<ISession> session, Func<Task> deliverConnected) SetupMockSession()
    {
        var mockSession = new Mock<ISession>();
        mockSession.SetupAllProperties();
        IMyObserver<IMessage>? observer = null;
        var observerReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        mockSession.Setup(s => s.Subscribe(It.IsAny<IMyObserver<IMessage>>()))
            .Callback<IMyObserver<IMessage>>(o =>
            {
                observer = o;
                observerReady.TrySetResult(true);
            });

        mockSession.Setup(s => s.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockSession.Setup(s => s.DisconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockSession.Setup(s => s.SendAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockSession.Setup(s => s.SendAsync(It.IsAny<object[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockSession.Setup(s => s.SendAckDataAsync(It.IsAny<object[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> deliverConnected = async () =>
        {
            // Wait for ConnectAsync to subscribe the observer
            await observerReady.Task;
            await observer!.OnNextAsync(new ConnectedMessage { Sid = "test-sid" });
        };

        return (mockSession, deliverConnected);
    }

    private static void ReplaceSession(IServiceCollection services, Mock<ISession> mockSession)
    {
        var pollingDescriptor = services.FirstOrDefault(d =>
            d.IsKeyedService && d.ServiceKey is TransportProtocol t && t == TransportProtocol.Polling);
        if (pollingDescriptor != null) services.Remove(pollingDescriptor);
        services.AddKeyedScoped<ISession>(TransportProtocol.Polling, (_, _) => mockSession.Object);

        var wsDescriptor = services.FirstOrDefault(d =>
            d.IsKeyedService && d.ServiceKey is TransportProtocol t && t == TransportProtocol.WebSocket);
        if (wsDescriptor != null) services.Remove(wsDescriptor);
        services.AddKeyedScoped<ISession>(TransportProtocol.WebSocket, (_, _) => mockSession.Object);
    }

    private static async Task<SocketIOClient> CreateConnectedClient(Mock<ISession> mockSession, Func<Task> deliverConnected)
    {
        var client = CreateClient(
            options: new SocketIOClientOptions { Reconnection = false, ConnectionTimeout = TimeSpan.FromSeconds(5) },
            configure: services => ReplaceSession(services, mockSession));

        _ = deliverConnected();
        await client.ConnectAsync();
        return client;
    }

    [Fact(DisplayName = "SIO-001: New client should not be connected")]
    public void SIO001()
    {
        using var client = CreateClient();

        client.Connected.Should().BeFalse();
    }

    [Fact(DisplayName = "SIO-002: New client Id should be null")]
    public void SIO002()
    {
        using var client = CreateClient();

        client.Id.Should().BeNull();
    }

    [Fact(DisplayName = "SIO-003: On with null event name should throw ArgumentException")]
    public void SIO003()
    {
        using var client = CreateClient();

        var act = () => client.On(null!, _ => Task.CompletedTask);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "SIO-004: On with empty event name should throw ArgumentException")]
    public void SIO004()
    {
        using var client = CreateClient();

        var act = () => client.On(string.Empty, _ => Task.CompletedTask);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "SIO-005: On with null handler should throw ArgumentNullException")]
    public void SIO005()
    {
        using var client = CreateClient();

        var act = () => client.On("test", (Func<IEventContext, Task>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "SIO-006: OnAny with null handler should throw ArgumentNullException")]
    public void SIO006()
    {
        using var client = CreateClient();

        var act = () => client.OnAny(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "SIO-007: OffAny with null handler should throw ArgumentNullException")]
    public void SIO007()
    {
        using var client = CreateClient();

        var act = () => client.OffAny(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "SIO-008: PrependAny with null handler should throw ArgumentNullException")]
    public void SIO008()
    {
        using var client = CreateClient();

        var act = () => client.PrependAny(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "SIO-009: EmitAsync when not connected should throw InvalidOperationException")]
    public async Task SIO009()
    {
        using var client = CreateClient();

        var act = () => client.EmitAsync("test", new object[] { "data" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact(DisplayName = "SIO-010: ConnectAsync should set Connected to true after receiving ConnectedMessage")]
    public async Task SIO010()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        client.Connected.Should().BeTrue();
    }

    [Fact(DisplayName = "SIO-011: ConnectAsync should set Id from ConnectedMessage Sid")]
    public async Task SIO011()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        client.Id.Should().Be("test-sid");
    }

    [Fact(DisplayName = "SIO-012: ConnectAsync when already connected should return immediately")]
    public async Task SIO012()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        // Second connect should return without trying to connect again
        await client.ConnectAsync();

        mockSession.Verify(s => s.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "SIO-013: On handler should be invoked when matching event is received")]
    public async Task SIO013()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        var handlerCalled = false;
        client.On("test-event", _ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        var mockEventMsg = new Mock<IEventMessage>();
        mockEventMsg.Setup(m => m.Type).Returns(MessageType.Event);
        mockEventMsg.Setup(m => m.Event).Returns("test-event");

        var observer = (IMyObserver<IMessage>)client;
        await observer.OnNextAsync(mockEventMsg.Object);

        handlerCalled.Should().BeTrue();
    }

    [Fact(DisplayName = "SIO-014: Once handler should be invoked only once")]
    public async Task SIO014()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        var callCount = 0;
        client.Once("test-event", _ =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        var mockEventMsg = new Mock<IEventMessage>();
        mockEventMsg.Setup(m => m.Type).Returns(MessageType.Event);
        mockEventMsg.Setup(m => m.Event).Returns("test-event");

        var observer = (IMyObserver<IMessage>)client;
        await observer.OnNextAsync(mockEventMsg.Object);
        await observer.OnNextAsync(mockEventMsg.Object);

        callCount.Should().Be(1);
    }

    [Fact(DisplayName = "SIO-015: OnAny handler should be invoked for any event")]
    public async Task SIO015()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        string? receivedEventName = null;
        client.OnAny((eventName, _) =>
        {
            receivedEventName = eventName;
            return Task.CompletedTask;
        });

        var mockEventMsg = new Mock<IEventMessage>();
        mockEventMsg.Setup(m => m.Type).Returns(MessageType.Event);
        mockEventMsg.Setup(m => m.Event).Returns("any-event");

        var observer = (IMyObserver<IMessage>)client;
        await observer.OnNextAsync(mockEventMsg.Object);

        receivedEventName.Should().Be("any-event");
    }

    [Fact(DisplayName = "SIO-016: OffAny should remove handler")]
    public async Task SIO016()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        var callCount = 0;
        Func<string, IEventContext, Task> handler = (_, _) =>
        {
            callCount++;
            return Task.CompletedTask;
        };
        client.OnAny(handler);
        client.OffAny(handler);

        var mockEventMsg = new Mock<IEventMessage>();
        mockEventMsg.Setup(m => m.Type).Returns(MessageType.Event);
        mockEventMsg.Setup(m => m.Event).Returns("test");

        var observer = (IMyObserver<IMessage>)client;
        await observer.OnNextAsync(mockEventMsg.Object);

        callCount.Should().Be(0);
    }

    [Fact(DisplayName = "SIO-017: PrependAny should insert handler at beginning")]
    public async Task SIO017()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        var order = new List<int>();
        client.OnAny((_, _) =>
        {
            order.Add(1);
            return Task.CompletedTask;
        });
        client.PrependAny((_, _) =>
        {
            order.Add(0);
            return Task.CompletedTask;
        });

        var mockEventMsg = new Mock<IEventMessage>();
        mockEventMsg.Setup(m => m.Type).Returns(MessageType.Event);
        mockEventMsg.Setup(m => m.Event).Returns("test");

        var observer = (IMyObserver<IMessage>)client;
        await observer.OnNextAsync(mockEventMsg.Object);

        order.Should().Equal(0, 1);
    }

    [Fact(DisplayName = "SIO-018: DisconnectAsync should set Connected to false")]
    public async Task SIO018()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        await client.DisconnectAsync();

        client.Connected.Should().BeFalse();
    }

    [Fact(DisplayName = "SIO-019: DisconnectAsync should set Id to null")]
    public async Task SIO019()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        await client.DisconnectAsync();

        client.Id.Should().BeNull();
    }

    [Fact(DisplayName = "SIO-020: DisconnectAsync should call session DisconnectAsync")]
    public async Task SIO020()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        await client.DisconnectAsync();

        mockSession.Verify(s => s.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "SIO-021: EmitAsync should call session SendAsync with event name and data")]
    public async Task SIO021()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        await client.EmitAsync("test-event", new object[] { "data1" });

        mockSession.Verify(s => s.SendAsync(
            It.Is<object[]>(d => (string)d[0] == "test-event" && (string)d[1] == "data1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "SIO-022: EmitAsync with ack should increment PacketId")]
    public async Task SIO022()
    {
        var (mockSession, deliverConnected) = SetupMockSession();
        using var client = await CreateConnectedClient(mockSession, deliverConnected);

        var internalClient = (IInternalSocketIOClient)client;
        var initialPacketId = internalClient.PacketId;

        await client.EmitAsync("test", new object[] { "data" }, _ => Task.CompletedTask);

        internalClient.PacketId.Should().Be(initialPacketId + 1);
    }
}
