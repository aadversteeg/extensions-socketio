using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;
using Newtonsoft.Json;

namespace UnitTests.Extensions.SocketIO.Client.Verification;

public class EngineIOHandshakeVerificationTests
{
    private WebSocketSession CreateWebSocketSession(
        Mock<IWebSocketAdapter> mockWsAdapter,
        EngineIOVersion version = EngineIOVersion.V4)
    {
        var mockLogger = new Mock<ILogger<WebSocketSession>>();
        var mockFactory = new Mock<IEngineIOAdapterFactory>();
        var mockSerializer = new Mock<ISerializer>();
        var mockMsgAdapterFactory = new Mock<IEngineIOMessageAdapterFactory>();
        var mockEngineIOAdapter = new Mock<IWebSocketEngineIOAdapter>();

        mockFactory.Setup(f => f.Create<IWebSocketEngineIOAdapter>(It.IsAny<EngineIOCompatibility>()))
            .Returns(mockEngineIOAdapter.Object);
        mockMsgAdapterFactory.Setup(f => f.Create(It.IsAny<EngineIOVersion>()))
            .Returns(new Mock<IEngineIOMessageAdapter>().Object);

        return new WebSocketSession(
            mockLogger.Object,
            mockFactory.Object,
            mockWsAdapter.Object,
            mockSerializer.Object,
            mockMsgAdapterFactory.Object);
    }

    [Fact(DisplayName = "VEH-001: V4 WebSocket URL should contain EIO=4&transport=websocket")]
    public async Task VEH001()
    {
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        Uri? capturedUri = null;

        mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((u, _) => capturedUri = u)
            .Returns(Task.CompletedTask);

        var sut = CreateWebSocketSession(mockWsAdapter);
        sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
        };

        await sut.ConnectAsync(CancellationToken.None);

        capturedUri.Should().NotBeNull();
        capturedUri!.Query.Should().Contain("EIO=4");
        capturedUri.Query.Should().Contain("transport=websocket");
    }

    [Fact(DisplayName = "VEH-002: V3 WebSocket URL should contain EIO=3&transport=websocket")]
    public async Task VEH002()
    {
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        Uri? capturedUri = null;

        mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((u, _) => capturedUri = u)
            .Returns(Task.CompletedTask);

        var sut = CreateWebSocketSession(mockWsAdapter, EngineIOVersion.V3);
        sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V3,
            Timeout = TimeSpan.FromSeconds(5),
        };

        await sut.ConnectAsync(CancellationToken.None);

        capturedUri.Should().NotBeNull();
        capturedUri!.Query.Should().Contain("EIO=3");
        capturedUri.Query.Should().Contain("transport=websocket");
    }

    [Fact(DisplayName = "VEH-003: Handshake response with maxPayload should deserialize correctly")]
    public void VEH003()
    {
        var json = "{\"sid\":\"abc123\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":20000,\"maxPayload\":1000000}";

        var opened = JsonConvert.DeserializeObject<OpenedMessage>(json, new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy(),
            },
        });

        opened.Should().NotBeNull();
        opened!.Sid.Should().Be("abc123");
        opened.PingInterval.Should().Be(25000);
        opened.PingTimeout.Should().Be(20000);
        opened.MaxPayload.Should().Be(1000000);
        opened.Upgrades.Should().Contain("websocket");
    }

    [Fact(DisplayName = "VEH-004: Custom query parameters should be appended to URL")]
    public async Task VEH004()
    {
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        Uri? capturedUri = null;

        mockWsAdapter.Setup(w => w.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Callback<Uri, CancellationToken>((u, _) => capturedUri = u)
            .Returns(Task.CompletedTask);

        var sut = CreateWebSocketSession(mockWsAdapter);
        sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
            Query = new NameValueCollection
            {
                ["token"] = "abc",
                ["room"] = "main",
            },
        };

        await sut.ConnectAsync(CancellationToken.None);

        capturedUri.Should().NotBeNull();
        capturedUri!.Query.Should().Contain("token=abc");
        capturedUri.Query.Should().Contain("room=main");
    }

    [Fact(DisplayName = "VEH-005: OpenedMessage should have correct type")]
    public void VEH005()
    {
        var opened = new OpenedMessage
        {
            Sid = "test",
            PingInterval = 25000,
            PingTimeout = 20000,
            MaxPayload = 1000000,
        };

        opened.Type.Should().Be(MessageType.Opened);
    }

    [Fact(DisplayName = "VEH-006: Handshake without maxPayload should default to 0")]
    public void VEH006()
    {
        var json = "{\"sid\":\"abc123\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":20000}";

        var opened = JsonConvert.DeserializeObject<OpenedMessage>(json, new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy(),
            },
        });

        opened.Should().NotBeNull();
        opened!.Sid.Should().Be("abc123");
        opened.MaxPayload.Should().Be(0, "maxPayload should default to 0 when not present in handshake");
    }

    [Fact(DisplayName = "VEH-007: Handshake with empty upgrades list should deserialize correctly")]
    public void VEH007()
    {
        var json = "{\"sid\":\"xyz789\",\"upgrades\":[],\"pingInterval\":25000,\"pingTimeout\":20000}";

        var opened = JsonConvert.DeserializeObject<OpenedMessage>(json, new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy(),
            },
        });

        opened.Should().NotBeNull();
        opened!.Upgrades.Should().NotBeNull();
        opened.Upgrades.Should().BeEmpty("empty upgrades array should deserialize to empty list");
    }
}
