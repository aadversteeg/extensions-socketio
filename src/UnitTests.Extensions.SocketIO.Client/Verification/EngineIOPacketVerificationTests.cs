using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;
using Microsoft.Extensions.Logging;

namespace UnitTests.Extensions.SocketIO.Client.Verification;

public class EngineIOPacketVerificationTests
{
    [Fact(DisplayName = "VEP-001: Close packet (1) should trigger disconnection in V4")]
    public async Task VEP001()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var disconnected = false;

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        sut.OnDisconnected = () => disconnected = true;

        var closeMessage = new Mock<IMessage>();
        closeMessage.Setup(m => m.Type).Returns(MessageType.Close);

        var result = await sut.ProcessMessageAsync(closeMessage.Object);

        result.Should().BeTrue("close packet should be swallowed");
        disconnected.Should().BeTrue("close packet should trigger disconnection");

        sut.Dispose();
    }

    [Fact(DisplayName = "VEP-002: Noop packet (6) should be handled gracefully in V4")]
    public async Task VEP002()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        var noopMessage = new Mock<IMessage>();
        noopMessage.Setup(m => m.Type).Returns(MessageType.Noop);

        var result = await sut.ProcessMessageAsync(noopMessage.Object);

        result.Should().BeTrue("noop packet should be swallowed");

        sut.Dispose();
    }

    [Fact(DisplayName = "VEP-003: V4 HTTP multiple packets should be separated by record separator")]
    public void VEP003()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockHttpAdapter = new Mock<IHttpAdapter>();
        var mockRetryPolicy = new Mock<IRetriable>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockPollingHandler = new Mock<IPollingHandler>();

        var sut = new HttpEngineIO4Adapter(
            mockStopwatch.Object,
            mockHttpAdapter.Object,
            mockRetryPolicy.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockPollingHandler.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        // Record separator \x1e between packets
        var text = "42[\"event1\"]" + "\u001E" + "42[\"event2\"]";

        var messages = sut.ExtractMessagesFromText(text).ToList();

        messages.Should().HaveCount(2);
        messages[0].Type.Should().Be(ProtocolMessageType.Text);
        messages[0].Text.Should().Be("42[\"event1\"]");
        messages[1].Type.Should().Be(ProtocolMessageType.Text);
        messages[1].Text.Should().Be("42[\"event2\"]");
    }

    [Fact(DisplayName = "VEP-004: V4 HTTP binary should be base64 encoded with 'b' prefix")]
    public void VEP004()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockHttpAdapter = new Mock<IHttpAdapter>();
        var mockRetryPolicy = new Mock<IRetriable>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockPollingHandler = new Mock<IPollingHandler>();

        var sut = new HttpEngineIO4Adapter(
            mockStopwatch.Object,
            mockHttpAdapter.Object,
            mockRetryPolicy.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockPollingHandler.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        var data = new byte[] { 1, 2, 3, 4, 5 };
        var base64 = Convert.ToBase64String(data);
        var text = "b" + base64;

        var messages = sut.ExtractMessagesFromText(text).ToList();

        messages.Should().HaveCount(1);
        messages[0].Type.Should().Be(ProtocolMessageType.Bytes);
        messages[0].Bytes.Should().BeEquivalentTo(data);
    }

    [Fact(DisplayName = "VEP-005: V3 HTTP text should use character counting format")]
    public void VEP005()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockHttpAdapter = new Mock<IHttpAdapter>();
        var mockRetryPolicy = new Mock<IRetriable>();
        var mockLogger = new Mock<ILogger<HttpEngineIO3Adapter>>();
        var mockPollingHandler = new Mock<IPollingHandler>();
        var mockDelay = new Mock<IDelay>();

        var sut = new HttpEngineIO3Adapter(
            mockStopwatch.Object,
            mockHttpAdapter.Object,
            mockRetryPolicy.Object,
            mockLogger.Object,
            mockPollingHandler.Object,
            mockDelay.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        // V3 format: length:content
        var text = "12:42[\"event1\"]12:42[\"event2\"]";

        var messages = sut.ExtractMessagesFromText(text).ToList();

        messages.Should().HaveCount(2);
        messages[0].Text.Should().Be("42[\"event1\"]");
        messages[1].Text.Should().Be("42[\"event2\"]");
    }

    [Fact(DisplayName = "VEP-006: Close packet (1) should trigger disconnection in V3")]
    public async Task VEP006()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockLogger = new Mock<ILogger<WebSocketEngineIO3Adapter>>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockDelay = new Mock<IDelay>();
        var disconnected = false;

        var sut = new WebSocketEngineIO3Adapter(
            mockStopwatch.Object,
            mockLogger.Object,
            mockWsAdapter.Object,
            mockDelay.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        sut.OnDisconnected = () => disconnected = true;

        var closeMessage = new Mock<IMessage>();
        closeMessage.Setup(m => m.Type).Returns(MessageType.Close);

        var result = await sut.ProcessMessageAsync(closeMessage.Object);

        result.Should().BeTrue("close packet should be swallowed");
        disconnected.Should().BeTrue("close packet should trigger disconnection in V3");

        sut.Dispose();
    }

    [Fact(DisplayName = "VEP-007: Noop packet (6) should be handled gracefully in V3")]
    public async Task VEP007()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockLogger = new Mock<ILogger<WebSocketEngineIO3Adapter>>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();
        var mockDelay = new Mock<IDelay>();

        var sut = new WebSocketEngineIO3Adapter(
            mockStopwatch.Object,
            mockLogger.Object,
            mockWsAdapter.Object,
            mockDelay.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        var noopMessage = new Mock<IMessage>();
        noopMessage.Setup(m => m.Type).Returns(MessageType.Noop);

        var result = await sut.ProcessMessageAsync(noopMessage.Object);

        result.Should().BeTrue("noop packet should be swallowed in V3");

        sut.Dispose();
    }

    [Fact(DisplayName = "VEP-008: V4 HTTP ToHttpRequest with bytes should produce base64 with 'b' prefix")]
    public void VEP008()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockHttpAdapter = new Mock<IHttpAdapter>();
        var mockRetryPolicy = new Mock<IRetriable>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockPollingHandler = new Mock<IPollingHandler>();

        var sut = new HttpEngineIO4Adapter(
            mockStopwatch.Object,
            mockHttpAdapter.Object,
            mockRetryPolicy.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockPollingHandler.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        var data1 = new byte[] { 10, 20, 30 };
        var data2 = new byte[] { 40, 50 };
        var bytes = new List<byte[]> { data1, data2 };

        var result = sut.ToHttpRequest(bytes);

        result.Method.Should().Be(RequestMethod.Post);
        result.BodyType.Should().Be(RequestBodyType.Text);

        // Multiple binary payloads separated by \x1e with 'b' prefix
        var expected = "b" + Convert.ToBase64String(data1) + "\u001E" + "b" + Convert.ToBase64String(data2);
        result.BodyText.Should().Be(expected);
    }

    [Fact(DisplayName = "VEP-009: V4 HTTP close packet in multi-packet payload should be extracted")]
    public void VEP009()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockHttpAdapter = new Mock<IHttpAdapter>();
        var mockRetryPolicy = new Mock<IRetriable>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockPollingHandler = new Mock<IPollingHandler>();

        var sut = new HttpEngineIO4Adapter(
            mockStopwatch.Object,
            mockHttpAdapter.Object,
            mockRetryPolicy.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockPollingHandler.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        // Record separator \x1e between packets: message + close
        var text = "4hello" + "\u001E" + "1";

        var messages = sut.ExtractMessagesFromText(text).ToList();

        messages.Should().HaveCount(2);
        messages[0].Text.Should().Be("4hello");
        messages[1].Text.Should().Be("1", "close packet '1' should be extracted as separate message");
    }

    [Fact(DisplayName = "VEP-010: V4 HTTP mixed text and binary extraction")]
    public void VEP010()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockHttpAdapter = new Mock<IHttpAdapter>();
        var mockRetryPolicy = new Mock<IRetriable>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockPollingHandler = new Mock<IPollingHandler>();

        var sut = new HttpEngineIO4Adapter(
            mockStopwatch.Object,
            mockHttpAdapter.Object,
            mockRetryPolicy.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockPollingHandler.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        var binaryData = new byte[] { 1, 2, 3, 4 };
        var base64 = Convert.ToBase64String(binaryData);
        // text message + binary message
        var text = "4hello" + "\u001E" + "b" + base64;

        var messages = sut.ExtractMessagesFromText(text).ToList();

        messages.Should().HaveCount(2);
        messages[0].Type.Should().Be(ProtocolMessageType.Text);
        messages[0].Text.Should().Be("4hello");
        messages[1].Type.Should().Be(ProtocolMessageType.Bytes);
        messages[1].Bytes.Should().BeEquivalentTo(binaryData);
    }

    [Fact(DisplayName = "VEP-011: V3 HTTP binary payload extraction from bytes")]
    public void VEP011()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockHttpAdapter = new Mock<IHttpAdapter>();
        var mockRetryPolicy = new Mock<IRetriable>();
        var mockLogger = new Mock<ILogger<HttpEngineIO3Adapter>>();
        var mockPollingHandler = new Mock<IPollingHandler>();
        var mockDelay = new Mock<IDelay>();

        var sut = new HttpEngineIO3Adapter(
            mockStopwatch.Object,
            mockHttpAdapter.Object,
            mockRetryPolicy.Object,
            mockLogger.Object,
            mockPollingHandler.Object,
            mockDelay.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        // V3 binary payload format:
        // messageType(1 byte) + lengthDigits + 0xFF + payload
        // Type 0 = text, Type 1 = binary
        // For text "4hello" (6 chars): 0x00 + 0x06 + 0xFF + "4hello"
        // For binary [1,2,3] (4 bytes with type byte): 0x01 + 0x04 + 0xFF + 0x04 + [1,2,3]
        var payload = new List<byte>();

        // Text message: "4hello"
        payload.Add(0x00); // text type
        payload.Add(0x06); // length = 6
        payload.Add(0xFF); // separator
        payload.AddRange(System.Text.Encoding.UTF8.GetBytes("4hello"));

        // Binary message: [1, 2, 3]
        payload.Add(0x01); // binary type
        payload.Add(0x04); // length = 4 (1 type byte + 3 data bytes)
        payload.Add(0xFF); // separator
        payload.Add(0x04); // type byte for binary
        payload.AddRange(new byte[] { 1, 2, 3 });

        var messages = sut.ExtractMessagesFromBytes(payload.ToArray()).ToList();

        messages.Should().HaveCount(2);
        messages[0].Type.Should().Be(ProtocolMessageType.Text);
        messages[0].Text.Should().Be("4hello");
        messages[1].Type.Should().Be(ProtocolMessageType.Bytes);
        messages[1].Bytes.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
    }

    [Fact(DisplayName = "VEP-012: Unknown message type in ProcessMessageAsync returns false")]
    public async Task VEP012()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockWsAdapter = new Mock<IWebSocketAdapter>();

        var sut = new WebSocketEngineIO4Adapter(
            mockStopwatch.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockWsAdapter.Object);

        sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        // ConnectedMessage is not handled by EngineIO4Adapter, should return false
        var connected = new ConnectedMessage { Namespace = "/" };
        var result = await sut.ProcessMessageAsync(connected);

        result.Should().BeFalse("unhandled message types should return false");

        sut.Dispose();
    }
}
