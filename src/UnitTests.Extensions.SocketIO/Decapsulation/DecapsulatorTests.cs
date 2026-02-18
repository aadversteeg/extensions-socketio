using FluentAssertions;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Serialization.Decapsulation;

namespace UnitTests.Extensions.SocketIO.Decapsulation;

public class DecapsulatorTests
{
    [Theory(DisplayName = "DEC-001: DecapsulateRawText should parse message type prefix correctly")]
    [InlineData("", false, null, null)]
    [InlineData("0", true, MessageType.Opened, "")]
    [InlineData("2", true, MessageType.Ping, "")]
    [InlineData("3", true, MessageType.Pong, "")]
    [InlineData("40", true, MessageType.Connected, "")]
    [InlineData("40/test,", true, MessageType.Connected, "/test,")]
    [InlineData("42[\"hello\"]", true, MessageType.Event, "[\"hello\"]")]
    [InlineData("43/test,1[\"hello\"]", true, MessageType.Ack, "/test,1[\"hello\"]")]
    [InlineData("461-/test,2[]", true, MessageType.BinaryAck, "1-/test,2[]")]
    [InlineData(
        "0{\"sid\":\"123\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
        true,
        MessageType.Opened,
        "{\"sid\":\"123\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}")]
    public void DEC001(string text, bool success, MessageType? type, string? data)
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateRawText(text);

        result.Should()
            .BeEquivalentTo(new DecapsulationResult
            {
                Success = success,
                Type = type,
                Data = data!,
            });
    }

    [Theory(DisplayName = "DEC-002: DecapsulateEventMessage should parse namespace, id and data")]
    [InlineData("[\"hello\"]", -1, null, "[\"hello\"]")]
    [InlineData("1[\"hello\"]", 1, null, "[\"hello\"]")]
    [InlineData("/test,[\"hello\"]", -1, "/test", "[\"hello\"]")]
    [InlineData("/test,1[\"hello\"]", 1, "/test", "[\"hello\"]")]
    public void DEC002(string text, int id, string? ns, string data)
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateEventMessage(text);

        result.Should()
            .BeEquivalentTo(new MessageResult
            {
                Id = id,
                Namespace = ns,
                Data = data,
            });
    }

    [Theory(DisplayName = "DEC-003: DecapsulateBinaryEventMessage should parse bytes count, namespace, id and data")]
    [InlineData("1-[\"event\",{\"_placeholder\":true,\"num\":0}]", -1, null, "[\"event\",{\"_placeholder\":true,\"num\":0}]", 1)]
    [InlineData("1-2[\"event\",{\"_placeholder\":true,\"num\":0}]", 2, null, "[\"event\",{\"_placeholder\":true,\"num\":0}]", 1)]
    [InlineData("1-8[\"event\"]", 8, null, "[\"event\"]", 1)]
    [InlineData("1-/test,8[\"event\"]", 8, "/test", "[\"event\"]", 1)]
    public void DEC003(string text, int id, string? ns, string data, int bytesCount)
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.DecapsulateBinaryEventMessage(text);

        result.Should()
            .BeEquivalentTo(new BinaryEventMessageResult
            {
                Id = id,
                Namespace = ns,
                Data = data,
                BytesCount = bytesCount,
            });
    }
}
