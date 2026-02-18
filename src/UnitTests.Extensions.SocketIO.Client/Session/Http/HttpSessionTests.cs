using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Session;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.Http;
using Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;

namespace UnitTests.Extensions.SocketIO.Client.Session.Http;

public class HttpSessionTests
{
    private readonly Mock<ILogger<HttpSession>> _mockLogger;
    private readonly Mock<IEngineIOAdapterFactory> _mockFactory;
    private readonly Mock<IHttpAdapter> _mockHttpAdapter;
    private readonly Mock<ISerializer> _mockSerializer;
    private readonly Mock<IEngineIOMessageAdapterFactory> _mockMsgAdapterFactory;
    private readonly Mock<IHttpEngineIOAdapter> _mockEngineIOAdapter;
    private readonly HttpSession _sut;

    public HttpSessionTests()
    {
        _mockLogger = new Mock<ILogger<HttpSession>>();
        _mockFactory = new Mock<IEngineIOAdapterFactory>();
        _mockHttpAdapter = new Mock<IHttpAdapter>();
        _mockSerializer = new Mock<ISerializer>();
        _mockMsgAdapterFactory = new Mock<IEngineIOMessageAdapterFactory>();
        _mockEngineIOAdapter = new Mock<IHttpEngineIOAdapter>();

        _mockFactory.Setup(f => f.Create<IHttpEngineIOAdapter>(It.IsAny<EngineIOCompatibility>()))
            .Returns(_mockEngineIOAdapter.Object);
        _mockMsgAdapterFactory.Setup(f => f.Create(It.IsAny<EngineIOVersion>()))
            .Returns(new Mock<IEngineIOMessageAdapter>().Object);

        _sut = new HttpSession(
            _mockLogger.Object,
            _mockFactory.Object,
            _mockHttpAdapter.Object,
            _mockSerializer.Object,
            _mockMsgAdapterFactory.Object);

        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
        };
    }

    [Fact(DisplayName = "HSE-001: ConnectAsync should set HttpAdapter Uri and send connect request")]
    public async Task HSE001()
    {
        HttpRequest? capturedRequest = null;
        _mockHttpAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .Callback<HttpRequest, CancellationToken>((r, _) => capturedRequest = r)
            .Returns(Task.CompletedTask);

        await _sut.ConnectAsync(CancellationToken.None);

        _mockHttpAdapter.VerifySet(h => h.Uri = It.IsAny<Uri>(), Times.Once);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.IsConnect.Should().BeTrue();
    }

    [Fact(DisplayName = "HSE-002: ConnectAsync with extra headers should set them on protocol adapter")]
    public async Task HSE002()
    {
        _mockHttpAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
            ExtraHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer token"
            }
        };

        await _sut.ConnectAsync(CancellationToken.None);

        _mockHttpAdapter.Verify(h => h.SetDefaultHeader("Authorization", "Bearer token"), Times.Once);
    }

    [Fact(DisplayName = "HSE-003: DisconnectAsync with no namespace should send 41")]
    public async Task HSE003()
    {
        var expectedRequest = new HttpRequest();
        _mockEngineIOAdapter.Setup(a => a.ToHttpRequest("41"))
            .Returns(expectedRequest);
        _mockHttpAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.DisconnectAsync(CancellationToken.None);

        _mockEngineIOAdapter.Verify(a => a.ToHttpRequest("41"), Times.Once);
    }

    [Fact(DisplayName = "HSE-004: DisconnectAsync with namespace should send 41{namespace},")]
    public async Task HSE004()
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri("http://localhost"),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
            Namespace = "/test",
        };

        var expectedRequest = new HttpRequest();
        _mockEngineIOAdapter.Setup(a => a.ToHttpRequest("41/test,"))
            .Returns(expectedRequest);
        _mockHttpAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.DisconnectAsync(CancellationToken.None);

        _mockEngineIOAdapter.Verify(a => a.ToHttpRequest("41/test,"), Times.Once);
    }

    [Fact(DisplayName = "HSE-005: OnNextAsync with text message should extract and handle messages")]
    public async Task HSE005()
    {
        var protocolMessages = new List<ProtocolMessage>
        {
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "test" }
        };
        _mockEngineIOAdapter.Setup(a => a.ExtractMessagesFromText("test-text"))
            .Returns(protocolMessages);
        _mockSerializer.Setup(s => s.Deserialize("test"))
            .Returns((IMessage?)null);

        var message = new ProtocolMessage
        {
            Type = ProtocolMessageType.Text,
            Text = "test-text"
        };

        await _sut.OnNextAsync(message);

        _mockEngineIOAdapter.Verify(a => a.ExtractMessagesFromText("test-text"), Times.Once);
    }

    [Fact(DisplayName = "HSE-006: OnNextAsync with bytes message should extract and handle messages")]
    public async Task HSE006()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var protocolMessages = new List<ProtocolMessage>();
        _mockEngineIOAdapter.Setup(a => a.ExtractMessagesFromBytes(bytes))
            .Returns(protocolMessages);

        var message = new ProtocolMessage
        {
            Type = ProtocolMessageType.Bytes,
            Bytes = bytes
        };

        await _sut.OnNextAsync(message);

        _mockEngineIOAdapter.Verify(a => a.ExtractMessagesFromBytes(bytes), Times.Once);
    }

    [Fact(DisplayName = "HSE-007: SendAsync should serialize data and send via http adapter")]
    public async Task HSE007()
    {
        var data = new object[] { "event", "data" };
        var protocolMessages = new List<ProtocolMessage>
        {
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "42[\"event\",\"data\"]" }
        };
        _mockSerializer.Setup(s => s.Serialize(data))
            .Returns(protocolMessages);
        var httpRequest = new HttpRequest();
        _mockEngineIOAdapter.Setup(a => a.ToHttpRequest(It.IsAny<string>()))
            .Returns(httpRequest);
        _mockHttpAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.SendAsync(data, CancellationToken.None);

        _mockSerializer.Verify(s => s.Serialize(data), Times.Once);
        _mockHttpAdapter.Verify(h => h.SendAsync(httpRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory(DisplayName = "HSE-008: GetServerUriSchema should map schemes correctly")]
    [InlineData("http://localhost", "http")]
    [InlineData("https://localhost", "https")]
    [InlineData("ws://localhost", "http")]
    [InlineData("wss://localhost", "https")]
    public async Task HSE008(string serverUri, string expectedScheme)
    {
        _sut.Options = new SessionOptions
        {
            ServerUri = new Uri(serverUri),
            EngineIO = EngineIOVersion.V4,
            Timeout = TimeSpan.FromSeconds(5),
        };

        Uri? capturedUri = null;
        _mockHttpAdapter.SetupSet(h => h.Uri = It.IsAny<Uri>())
            .Callback<Uri>(u => capturedUri = u);
        _mockHttpAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ConnectAsync(CancellationToken.None);

        capturedUri.Should().NotBeNull();
        capturedUri!.Scheme.Should().Be(expectedScheme);
    }
}
