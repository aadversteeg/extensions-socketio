using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;
using Ave.Extensions.SocketIO.Protocol;

namespace UnitTests.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;

public class HttpEngineIO3AdapterTests
{
    private readonly HttpEngineIO3Adapter _sut;

    public HttpEngineIO3AdapterTests()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockHttpAdapter = new Mock<IHttpAdapter>();
        var mockRetryPolicy = new Mock<IRetriable>();
        var mockLogger = new Mock<ILogger<HttpEngineIO3Adapter>>();
        var mockPollingHandler = new Mock<IPollingHandler>();
        var mockDelay = new Mock<IDelay>();

        _sut = new HttpEngineIO3Adapter(
            mockStopwatch.Object,
            mockHttpAdapter.Object,
            mockRetryPolicy.Object,
            mockLogger.Object,
            mockPollingHandler.Object,
            mockDelay.Object);

        _sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };
    }

    [Fact(DisplayName = "HE3-001: ToHttpRequest with text should return length-prefixed format")]
    public void HE3001()
    {
        var result = _sut.ToHttpRequest("hello");

        result.Method.Should().Be(RequestMethod.Post);
        result.BodyType.Should().Be(RequestBodyType.Text);
        result.BodyText.Should().Be("5:hello");
    }

    [Fact(DisplayName = "HE3-002: ToHttpRequest with empty string should throw ArgumentException")]
    public void HE3002()
    {
        var act = () => _sut.ToHttpRequest(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "HE3-003: ToHttpRequest with bytes should return binary-encoded request")]
    public void HE3003()
    {
        var bytes = new List<byte[]> { new byte[] { 1, 2, 3 } };

        var result = _sut.ToHttpRequest(bytes);

        result.Method.Should().Be(RequestMethod.Post);
        result.BodyType.Should().Be(RequestBodyType.Bytes);
        result.BodyBytes.Should().NotBeNull();
        result.Headers.Should().ContainKey("Content-Type");
    }

    [Fact(DisplayName = "HE3-004: ToHttpRequest with empty bytes collection should throw ArgumentException")]
    public void HE3004()
    {
        var bytes = new List<byte[]>();

        var act = () => _sut.ToHttpRequest(bytes);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "HE3-005: ExtractMessagesFromText should parse single message")]
    public void HE3005()
    {
        var text = "5:hello";

        var messages = _sut.ExtractMessagesFromText(text).ToList();

        messages.Should().HaveCount(1);
        messages[0].Text.Should().Be("hello");
    }

    [Fact(DisplayName = "HE3-006: ExtractMessagesFromText should parse multiple messages")]
    public void HE3006()
    {
        var text = "5:hello5:world";

        var messages = _sut.ExtractMessagesFromText(text).ToList();

        messages.Should().HaveCount(2);
        messages[0].Text.Should().Be("hello");
        messages[1].Text.Should().Be("world");
    }

    [Fact(DisplayName = "HE3-007: ExtractMessagesFromBytes should parse text message")]
    public void HE3007()
    {
        // Format: type(0=text) + length digits + 0xFF + payload
        var payload = Encoding.UTF8.GetBytes("hi");
        var bytes = new byte[] { 0, 2, 0xFF };
        var fullBytes = new byte[bytes.Length + payload.Length];
        Buffer.BlockCopy(bytes, 0, fullBytes, 0, bytes.Length);
        Buffer.BlockCopy(payload, 0, fullBytes, bytes.Length, payload.Length);

        var messages = _sut.ExtractMessagesFromBytes(fullBytes).ToList();

        messages.Should().HaveCount(1);
        messages[0].Type.Should().Be(ProtocolMessageType.Text);
        messages[0].Text.Should().Be("hi");
    }

    [Fact(DisplayName = "HE3-008: ExtractMessagesFromBytes should parse binary message")]
    public void HE3008()
    {
        // Format: type(1=binary) + length digits + 0xFF + 4(binary type prefix) + payload
        var payload = new byte[] { 10, 20, 30 };
        // Length = payload.Length + 1 (for the type byte 4) = 4
        var header = new byte[] { 1, 4, 0xFF, 4 };
        var fullBytes = new byte[header.Length + payload.Length];
        Buffer.BlockCopy(header, 0, fullBytes, 0, header.Length);
        Buffer.BlockCopy(payload, 0, fullBytes, header.Length, payload.Length);

        var messages = _sut.ExtractMessagesFromBytes(fullBytes).ToList();

        messages.Should().HaveCount(1);
        messages[0].Type.Should().Be(ProtocolMessageType.Bytes);
        messages[0].Bytes.Should().BeEquivalentTo(payload);
    }
}
