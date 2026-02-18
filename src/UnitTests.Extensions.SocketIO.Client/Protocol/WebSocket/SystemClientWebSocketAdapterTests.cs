using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using ClientWebSocketMessageType = Ave.Extensions.SocketIO.Client.Protocol.WebSocket.WebSocketMessageType;
using SysWebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;

namespace UnitTests.Extensions.SocketIO.Client.Protocol.WebSocket;

public class SystemClientWebSocketAdapterTests
{
    private readonly Mock<IWebSocketClient> _mockWs;
    private readonly SystemClientWebSocketAdapter _sut;

    public SystemClientWebSocketAdapterTests()
    {
        _mockWs = new Mock<IWebSocketClient>();
        _sut = new SystemClientWebSocketAdapter(_mockWs.Object);
    }

    [Fact(DisplayName = "SCA-001: SendAsync with data smaller than chunk size should send in one call")]
    public async Task SCA001()
    {
        var data = new byte[] { 1, 2, 3 };
        _sut.SendChunkSize = 8192;

        await _sut.SendAsync(data, ClientWebSocketMessageType.Text, CancellationToken.None);

        _mockWs.Verify(ws => ws.SendAsync(
            It.Is<ArraySegment<byte>>(s => s.Count == 3),
            SysWebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "SCA-002: SendAsync with data larger than chunk size should send in multiple chunks")]
    public async Task SCA002()
    {
        var data = new byte[10];
        _sut.SendChunkSize = 4;

        await _sut.SendAsync(data, ClientWebSocketMessageType.Binary, CancellationToken.None);

        // 10 bytes / 4 chunk = 3 calls (4 + 4 + 2)
        _mockWs.Verify(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            It.IsAny<SysWebSocketMessageType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact(DisplayName = "SCA-003: SendAsync last chunk should have endOfMessage true")]
    public async Task SCA003()
    {
        var data = new byte[10];
        _sut.SendChunkSize = 4;

        var endOfMessageValues = new System.Collections.Generic.List<bool>();
        _mockWs.Setup(ws => ws.SendAsync(
                It.IsAny<ArraySegment<byte>>(),
                It.IsAny<SysWebSocketMessageType>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<ArraySegment<byte>, SysWebSocketMessageType, bool, CancellationToken>(
                (_, _, endOfMessage, _) => endOfMessageValues.Add(endOfMessage))
            .Returns(Task.CompletedTask);

        await _sut.SendAsync(data, ClientWebSocketMessageType.Binary, CancellationToken.None);

        endOfMessageValues.Should().HaveCount(3);
        endOfMessageValues[0].Should().BeFalse();
        endOfMessageValues[1].Should().BeFalse();
        endOfMessageValues[2].Should().BeTrue();
    }

    [Fact(DisplayName = "SCA-004: ConnectAsync should delegate to underlying websocket client")]
    public async Task SCA004()
    {
        var uri = new Uri("ws://localhost");

        await _sut.ConnectAsync(uri, CancellationToken.None);

        _mockWs.Verify(ws => ws.ConnectAsync(uri, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "SCA-005: ReceiveAsync should accumulate chunks until EndOfMessage")]
    public async Task SCA005()
    {
        _sut.ReceiveChunkSize = 8192;
        var callCount = 0;
        var chunk1 = Encoding.UTF8.GetBytes("hello");
        var chunk2 = Encoding.UTF8.GetBytes(" world");

        _mockWs.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Returns<ArraySegment<byte>, CancellationToken>((buffer, _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    Array.Copy(chunk1, 0, buffer.Array!, buffer.Offset, chunk1.Length);
                    return Task.FromResult(new WebSocketReceiveResult(chunk1.Length, SysWebSocketMessageType.Text, false));
                }
                Array.Copy(chunk2, 0, buffer.Array!, buffer.Offset, chunk2.Length);
                return Task.FromResult(new WebSocketReceiveResult(chunk2.Length, SysWebSocketMessageType.Text, true));
            });

        var result = await _sut.ReceiveAsync(CancellationToken.None);

        Encoding.UTF8.GetString(result.Bytes).Should().Be("hello world");
    }

    [Fact(DisplayName = "SCA-006: ReceiveAsync should return correct message type")]
    public async Task SCA006()
    {
        var data = new byte[] { 1, 2, 3 };
        _mockWs.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .Returns<ArraySegment<byte>, CancellationToken>((buffer, _) =>
            {
                Array.Copy(data, 0, buffer.Array!, buffer.Offset, data.Length);
                return Task.FromResult(new WebSocketReceiveResult(data.Length, SysWebSocketMessageType.Binary, true));
            });

        var result = await _sut.ReceiveAsync(CancellationToken.None);

        result.Type.Should().Be(ClientWebSocketMessageType.Binary);
    }

    [Fact(DisplayName = "SCA-007: SetDefaultHeader should delegate to underlying websocket client")]
    public void SCA007()
    {
        _sut.SetDefaultHeader("X-Custom", "value");

        _mockWs.Verify(ws => ws.SetDefaultHeader("X-Custom", "value"), Times.Once);
    }
}
