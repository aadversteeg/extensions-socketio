using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ave.Extensions.SocketIO.Client.Observers;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Protocol;

namespace UnitTests.Extensions.SocketIO.Client.Protocol.Http;

public class HttpAdapterTests
{
    private readonly Mock<IHttpClient> _mockHttpClient;
    private readonly Mock<ILogger<HttpAdapter>> _mockLogger;
    private readonly HttpAdapter _sut;

    public HttpAdapterTests()
    {
        _mockHttpClient = new Mock<IHttpClient>();
        _mockLogger = new Mock<ILogger<HttpAdapter>>();
        _sut = new HttpAdapter(_mockHttpClient.Object, _mockLogger.Object);
    }

    [Fact(DisplayName = "HTA-001: IsReadyToSend when Uri is null should return false")]
    public void HTA001()
    {
        _sut.Uri = null;

        _sut.IsReadyToSend.Should().BeFalse();
    }

    [Fact(DisplayName = "HTA-002: IsReadyToSend when Uri has no sid should return false")]
    public void HTA002()
    {
        _sut.Uri = new Uri("http://localhost?EIO=4&transport=polling");

        _sut.IsReadyToSend.Should().BeFalse();
    }

    [Fact(DisplayName = "HTA-003: IsReadyToSend when Uri has sid should return true")]
    public void HTA003()
    {
        _sut.Uri = new Uri("http://localhost?EIO=4&transport=polling&sid=abc123");

        _sut.IsReadyToSend.Should().BeTrue();
    }

    [Fact(DisplayName = "HTA-004: SendAsync should call httpClient SendAsync")]
    public async Task HTA004()
    {
        var mockResponse = new Mock<IHttpResponse>();
        mockResponse.Setup(r => r.MediaType).Returns("text/plain");
        mockResponse.Setup(r => r.ReadAsStringAsync()).ReturnsAsync("response");
        _mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        _sut.Uri = new Uri("http://localhost?EIO=4&transport=polling&sid=abc");
        var req = new HttpRequest { Uri = new Uri("http://localhost/test") };
        await _sut.SendAsync(req, CancellationToken.None);

        _mockHttpClient.Verify(c => c.SendAsync(req, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "HTA-005: SendAsync with text response should notify observer with text message")]
    public async Task HTA005()
    {
        var mockResponse = new Mock<IHttpResponse>();
        mockResponse.Setup(r => r.MediaType).Returns("text/plain");
        mockResponse.Setup(r => r.ReadAsStringAsync()).ReturnsAsync("test-message");
        _mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        ProtocolMessage? receivedMessage = null;
        var mockObserver = new Mock<IMyObserver<ProtocolMessage>>();
        mockObserver.Setup(o => o.OnNextAsync(It.IsAny<ProtocolMessage>()))
            .Callback<ProtocolMessage>(m => receivedMessage = m)
            .Returns(Task.CompletedTask);
        _sut.Subscribe(mockObserver.Object);

        _sut.Uri = new Uri("http://localhost?EIO=4&transport=polling&sid=abc");
        var req = new HttpRequest { Uri = new Uri("http://localhost/test") };
        await _sut.SendAsync(req, CancellationToken.None);

        // Give time for fire-and-forget HandleResponseAsync
        await Task.Delay(100);

        receivedMessage.Should().NotBeNull();
        receivedMessage!.Type.Should().Be(ProtocolMessageType.Text);
        receivedMessage.Text.Should().Be("test-message");
    }

    [Fact(DisplayName = "HTA-006: SendAsync with binary response should notify observer with bytes message")]
    public async Task HTA006()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var mockResponse = new Mock<IHttpResponse>();
        mockResponse.Setup(r => r.MediaType).Returns("application/octet-stream");
        mockResponse.Setup(r => r.ReadAsByteArrayAsync()).ReturnsAsync(bytes);
        _mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        ProtocolMessage? receivedMessage = null;
        var mockObserver = new Mock<IMyObserver<ProtocolMessage>>();
        mockObserver.Setup(o => o.OnNextAsync(It.IsAny<ProtocolMessage>()))
            .Callback<ProtocolMessage>(m => receivedMessage = m)
            .Returns(Task.CompletedTask);
        _sut.Subscribe(mockObserver.Object);

        _sut.Uri = new Uri("http://localhost?EIO=4&transport=polling&sid=abc");
        var req = new HttpRequest { Uri = new Uri("http://localhost/test") };
        await _sut.SendAsync(req, CancellationToken.None);

        await Task.Delay(100);

        receivedMessage.Should().NotBeNull();
        receivedMessage!.Type.Should().Be(ProtocolMessageType.Bytes);
        receivedMessage.Bytes.Should().BeEquivalentTo(bytes);
    }

    [Fact(DisplayName = "HTA-007: SendAsync when request has no Uri should use adapter Uri with timestamp")]
    public async Task HTA007()
    {
        var mockResponse = new Mock<IHttpResponse>();
        mockResponse.Setup(r => r.MediaType).Returns("text/plain");
        mockResponse.Setup(r => r.ReadAsStringAsync()).ReturnsAsync("ok");

        HttpRequest? capturedRequest = null;
        _mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .Callback<HttpRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(mockResponse.Object);

        _sut.Uri = new Uri("http://localhost?EIO=4&transport=polling&sid=abc");
        var req = new HttpRequest(); // No Uri set
        await _sut.SendAsync(req, CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Uri.Should().NotBeNull();
        capturedRequest.Uri!.AbsoluteUri.Should().Contain("&t=");
    }

    [Fact(DisplayName = "HTA-008: SendAsync when httpClient throws and IsConnect is true should not call OnDisconnected")]
    public async Task HTA008()
    {
        _mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("connection failed"));

        var disconnected = false;
        _sut.OnDisconnected = () => disconnected = true;
        _sut.Uri = new Uri("http://localhost?EIO=4&transport=polling&sid=abc");

        var req = new HttpRequest { Uri = new Uri("http://localhost/test"), IsConnect = true };
        var act = () => _sut.SendAsync(req, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        disconnected.Should().BeFalse();
    }

    [Fact(DisplayName = "HTA-009: SendAsync when httpClient throws and IsConnect is false should call OnDisconnected")]
    public async Task HTA009()
    {
        _mockHttpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("connection lost"));

        var disconnected = false;
        _sut.OnDisconnected = () => disconnected = true;
        _sut.Uri = new Uri("http://localhost?EIO=4&transport=polling&sid=abc");

        var req = new HttpRequest { Uri = new Uri("http://localhost/test"), IsConnect = false };
        var act = () => _sut.SendAsync(req, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        disconnected.Should().BeTrue();
    }

    [Fact(DisplayName = "HTA-010: SetDefaultHeader should delegate to httpClient")]
    public void HTA010()
    {
        _sut.SetDefaultHeader("Authorization", "Bearer token");

        _mockHttpClient.Verify(c => c.SetDefaultHeader("Authorization", "Bearer token"), Times.Once);
    }
}
