using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Serialization.Decapsulation;
using Ave.Extensions.SocketIO.Serialization.SystemTextJson;
using UnitTests.Extensions.SocketIO.Helpers;

namespace UnitTests.Extensions.SocketIO.Serialization.SystemTextJson;

public class SystemJsonSerializerTests
{
    private readonly Decapsulator _realDecapsulator = new Decapsulator();

    private SystemJsonSerializer NewSerializer(IEngineIOMessageAdapter adapter)
    {
        return NewSerializer(adapter, new JsonSerializerOptions());
    }

    private SystemJsonSerializer NewSerializer(IEngineIOMessageAdapter adapter, JsonSerializerOptions options)
    {
        return NewSerializer(_realDecapsulator, adapter, options);
    }

    private SystemJsonSerializer NewSerializer(JsonSerializerOptions options)
    {
        return NewSerializer(new Mock<IEngineIOMessageAdapter>().Object, options);
    }

    private static SystemJsonSerializer NewSerializer(
        IDecapsulable decapsulator,
        IEngineIOMessageAdapter adapter,
        JsonSerializerOptions options)
    {
        var serializer = new SystemJsonSerializer(decapsulator, options);
        serializer.SetEngineIOMessageAdapter(adapter);
        return serializer;
    }

    [Fact(DisplayName = "SJS-001: Serialize with null data should throw ArgumentNullException")]
    public void SJS001()
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Invoking(x => x.Serialize(null!))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "SJS-002: Serialize with empty data should throw ArgumentException")]
    public void SJS002()
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Invoking(x => x.Serialize(new object[0]))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "SJS-003: Serialize event name only should produce correct text")]
    public void SJS003()
    {
        var serializer = NewSerializer(new JsonSerializerOptions());
        var list = serializer.Serialize(new object[] { "event" });
        list[0].Text.Should().Be("42[\"event\"]");
        list[0].Type.Should().Be(ProtocolMessageType.Text);
    }

    [Fact(DisplayName = "SJS-004: Serialize event with null value should include null in JSON")]
    public void SJS004()
    {
        var serializer = NewSerializer(new JsonSerializerOptions());
        var list = serializer.Serialize(new object[] { "event", null! });
        list[0].Text.Should().Be("42[\"event\",null]");
    }

    [Fact(DisplayName = "SJS-005: Serialize event with string should produce correct text")]
    public void SJS005()
    {
        var serializer = NewSerializer(new JsonSerializerOptions());
        var list = serializer.Serialize(new object[] { "event", "hello, world!" });
        list[0].Text.Should().Be("42[\"event\",\"hello, world!\"]");
    }

    [Fact(DisplayName = "SJS-006: Serialize event with true should produce correct text")]
    public void SJS006()
    {
        var serializer = NewSerializer(new JsonSerializerOptions());
        var list = serializer.Serialize(new object[] { "event", true });
        list[0].Text.Should().Be("42[\"event\",true]");
    }

    [Fact(DisplayName = "SJS-007: Serialize event with false should produce correct text")]
    public void SJS007()
    {
        var serializer = NewSerializer(new JsonSerializerOptions());
        var list = serializer.Serialize(new object[] { "event", false });
        list[0].Text.Should().Be("42[\"event\",false]");
    }

    [Fact(DisplayName = "SJS-008: Serialize event with int max should produce correct text")]
    public void SJS008()
    {
        var serializer = NewSerializer(new JsonSerializerOptions());
        var list = serializer.Serialize(new object[] { "event", int.MaxValue });
        list[0].Text.Should().Be("42[\"event\",2147483647]");
    }

    [Fact(DisplayName = "SJS-009: Serialize event with object should produce correct text")]
    public void SJS009()
    {
        var serializer = NewSerializer(new JsonSerializerOptions());
        var list = serializer.Serialize(new object[] { "event", new { id = 1, name = "Alice" } });
        list[0].Text.Should().Be("42[\"event\",{\"id\":1,\"name\":\"Alice\"}]");
    }

    [Theory(DisplayName = "SJS-010: Serialize with namespace and no bytes should include namespace prefix")]
    [InlineData(null, "42[\"event\"]")]
    [InlineData("", "42[\"event\"]")]
    [InlineData("/test", "42/test,[\"event\"]")]
    public void SJS010(string? ns, string expected)
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Namespace = ns;
        var list = serializer.Serialize(new object[] { "event" });
        list[0].Text.Should().Be(expected);
    }

    [Theory(DisplayName = "SJS-011: Serialize with namespace and bytes should include binary prefix")]
    [InlineData(null, "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("", "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("/test", "451-/test,[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    public void SJS011(string? ns, string expected)
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Namespace = ns;
        var list = serializer.Serialize(new object[] { "event", TestFile.NiuB.Bytes });
        list[0].Text.Should().Be(expected);
    }

    [Fact(DisplayName = "SJS-012: Serialize with bytes should return text and binary protocol messages")]
    public void SJS012()
    {
        var serializer = NewSerializer(new JsonSerializerOptions());
        var list = serializer.Serialize(new object[] { "event", TestFile.IndexHtml });
        list.Should().HaveCount(2);
        list[0].Type.Should().Be(ProtocolMessageType.Text);
        list[1].Type.Should().Be(ProtocolMessageType.Bytes);
        list[1].Bytes.Should().BeEquivalentTo(TestFile.IndexHtml.Bytes);
    }

    [Fact(DisplayName = "SJS-013: Deserialize opened message should return OpenedMessage with correct properties")]
    public void SJS013()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("0{\"sid\":\"123\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
        message.Should().BeEquivalentTo(new OpenedMessage
        {
            Sid = "123",
            Upgrades = new List<string> { "websocket" },
            PingInterval = 10000,
            PingTimeout = 5000,
        });
    }

    [Fact(DisplayName = "SJS-014: Deserialize with EIO3 adapter for connected message")]
    public void SJS014()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO3MessageAdapter());
        var message = serializer.Deserialize("40/test,");
        message.Should().BeEquivalentTo(new ConnectedMessage { Namespace = "/test" });
    }

    [Fact(DisplayName = "SJS-015: Deserialize with EIO4 adapter for connected message with sid")]
    public void SJS015()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("40{\"sid\":\"123\"}");
        message.Should().BeEquivalentTo(new ConnectedMessage { Sid = "123" });
    }

    [Fact(DisplayName = "SJS-016: Deserialize with EIO4 adapter for connected message with namespace and sid")]
    public void SJS016()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("40/test,{\"sid\":\"123\"}");
        message.Should().BeEquivalentTo(new ConnectedMessage { Sid = "123", Namespace = "/test" });
    }

    [Fact(DisplayName = "SJS-017: Deserialize empty string should return null")]
    public void SJS017()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        serializer.Deserialize("").Should().BeNull();
    }

    [Fact(DisplayName = "SJS-018: Deserialize unsupported text should return null")]
    public void SJS018()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        serializer.Deserialize("unsupported text").Should().BeNull();
    }

    [Fact(DisplayName = "SJS-019: Deserialize ping message should return PingMessage")]
    public void SJS019()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("2");
        message.Should().BeOfType<PingMessage>();
    }

    [Fact(DisplayName = "SJS-020: Deserialize pong message should return PongMessage")]
    public void SJS020()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("3");
        message.Should().BeOfType<PongMessage>();
    }

    [Fact(DisplayName = "SJS-021: Deserialize disconnected message should parse namespace")]
    public void SJS021()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("41/test,");
        message.Should().BeEquivalentTo(new DisconnectedMessage { Namespace = "/test" });
    }

    [Fact(DisplayName = "SJS-022: Deserialize event message should parse event name")]
    public void SJS022()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("42[\"hello\",\"world\"]") as IEventMessage;
        message.Should().NotBeNull();
        message!.Event.Should().Be("hello");
        message.GetValue<string>(0).Should().Be("world");
    }

    [Fact(DisplayName = "SJS-023: Deserialize event message with namespace and id")]
    public void SJS023()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("42/test,2[\"event\"]") as IEventMessage;
        message.Should().NotBeNull();
        message!.Event.Should().Be("event");
        message.Id.Should().Be(2);
        message.Namespace.Should().Be("/test");
    }

    [Fact(DisplayName = "SJS-024: Deserialize ack message should parse id and namespace")]
    public void SJS024()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("43/test,1[\"nice\"]") as IDataMessage;
        message.Should().NotBeNull();
        message!.Id.Should().Be(1);
        message.Namespace.Should().Be("/test");
        message.GetValue<string>(0).Should().Be("nice");
    }

    [Fact(DisplayName = "SJS-025: Deserialize EIO3 error message should parse error string")]
    public void SJS025()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO3MessageAdapter());
        var message = serializer.Deserialize("44\"Authentication error\"");
        message.Should().BeEquivalentTo(new ErrorMessage
        {
            Namespace = null,
            Error = "Authentication error",
        });
    }

    [Fact(DisplayName = "SJS-026: Deserialize EIO4 error message should parse error from JSON")]
    public void SJS026()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("44/test,{\"message\":\"Authentication error\"}");
        message.Should().BeEquivalentTo(new ErrorMessage
        {
            Namespace = "/test",
            Error = "Authentication error",
        });
    }

    [Fact(DisplayName = "SJS-027: Deserialize binary ack message should parse byte count and properties")]
    public void SJS027()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("461-/test,2[{\"_placeholder\":true,\"num\":0}]");
        message.Should().BeEquivalentTo(new SystemJsonBinaryAckMessage
        {
            Id = 2,
            Namespace = "/test",
            BytesCount = 1,
            Bytes = new List<byte[]>(),
        }, options => options
            .IncludingAllRuntimeProperties()
            .Excluding(p => p.Name == nameof(SystemJsonAckMessage.DataItems))
            .Excluding(p => p.Name == nameof(SystemJsonAckMessage.RawText))
            .Excluding(p => p.Name == nameof(ISystemJsonAckMessage.JsonSerializerOptions)));
    }

    [Fact(DisplayName = "SJS-028: Deserialize binary event message with bytes should resolve placeholder")]
    public void SJS028()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = (IBinaryAckMessage)serializer.Deserialize("451-/test,30[\"event\",{\"Size\":666,\"Name\":\"NiuB\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]")!;
        message.Add(TestFile.NiuB.Bytes);
        var item = message.GetValue<TestFile>(0);
        item.Should().BeEquivalentTo(TestFile.NiuB);
    }

    [Fact(DisplayName = "SJS-029: Deserialize when decapsulation fails should return null")]
    public void SJS029()
    {
        var mockDecapsulator = new Mock<IDecapsulable>();
        mockDecapsulator.Setup(d => d.DecapsulateRawText(It.IsAny<string>()))
            .Returns(new DecapsulationResult { Success = false });
        var adapter = new SystemJsonEngineIO4MessageAdapter();
        var serializer = NewSerializer(mockDecapsulator.Object, adapter, new JsonSerializerOptions());
        serializer.Deserialize("anything").Should().BeNull();
    }

    [Theory(DisplayName = "SJS-030: Deserialize DataMessage RawText should always be the JSON array")]
    [InlineData("42[\"event\"]", "[\"event\"]")]
    [InlineData("421[\"event\"]", "[\"event\"]")]
    [InlineData("42/test,2[\"event\"]", "[\"event\"]")]
    [InlineData("43/test,1[\"nice\"]", "[\"nice\"]")]
    [InlineData("461-/test,2[{\"_placeholder\":true,\"num\":0}]", "[{\"_placeholder\":true,\"num\":0}]")]
    public void SJS030(string raw, string expected)
    {
        var serializer = NewSerializer(new JsonSerializerOptions());
        var message = (IDataMessage)serializer.Deserialize(raw)!;
        message.RawText.Should().Be(expected);
    }

    [Fact(DisplayName = "SJS-031: Serialize with packetId and null data should throw ArgumentNullException")]
    public void SJS031()
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Invoking(x => x.Serialize(null!, 1))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "SJS-032: Serialize with packetId and empty data should throw ArgumentException")]
    public void SJS032()
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Invoking(x => x.Serialize(new object[0], 1))
            .Should()
            .Throw<ArgumentException>();
    }

    [Theory(DisplayName = "SJS-033: Serialize with packetId should include namespace and id")]
    [InlineData(null, 1, "421[\"event\"]")]
    [InlineData("", 2, "422[\"event\"]")]
    [InlineData("/test", 3, "42/test,3[\"event\"]")]
    public void SJS033(string? ns, int id, string expected)
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Namespace = ns;
        var list = serializer.Serialize(new object[] { "event" }, id);
        list[0].Text.Should().Be(expected);
    }

    [Theory(DisplayName = "SJS-034: Serialize with packetId and bytes should include binary prefix")]
    [InlineData(null, 4, "451-4[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("", 5, "451-5[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("/test", 6, "451-/test,6[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    public void SJS034(string? ns, int id, string expected)
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Namespace = ns;
        var list = serializer.Serialize(new object[] { "event", TestFile.NiuB.Bytes }, id);
        list[0].Text.Should().Be(expected);
    }

    [Fact(DisplayName = "SJS-035: Serialize with CamelCase option should produce camelCase JSON")]
    public void SJS035()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var serializer = NewSerializer(options);
        var json = serializer.Serialize(new
        {
            User = "admin",
            Password = "123456",
        });
        json.Should().Be("{\"user\":\"admin\",\"password\":\"123456\"}");
    }

    [Theory(DisplayName = "SJS-036: SerializeAckData should produce ack prefix with correct namespace")]
    [InlineData(null, 1, "431[1,\"2\"]")]
    [InlineData("", 2, "432[1,\"2\"]")]
    [InlineData("/test", 3, "43/test,3[1,\"2\"]")]
    public void SJS036(string? ns, int id, string expected)
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Namespace = ns;
        var list = serializer.SerializeAckData(new object[] { 1, "2" }, id);
        list[0].Text.Should().Be(expected);
    }

    [Theory(DisplayName = "SJS-037: SerializeAckData with bytes should produce binary ack prefix")]
    [InlineData(null, 4, "461-4[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("", 5, "461-5[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("/test", 6, "461-/test,6[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    public void SJS037(string? ns, int id, string expected)
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Namespace = ns;
        var list = serializer.SerializeAckData(new object[] { "event", TestFile.NiuB.Bytes }, id);
        list[0].Text.Should().Be(expected);
    }

    [Fact(DisplayName = "SJS-038: Deserialize event message with multiple data items")]
    public void SJS038()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("42[\"hello\",{\"id\":1,\"name\":\"Alice\"},-123456]") as IEventMessage;
        message.Should().NotBeNull();
        message!.Event.Should().Be("hello");

        var item1 = message.GetValue<Dictionary<string, object>>(0);
        item1.Should().NotBeNull();

        var item2 = message.GetValue<int>(1);
        item2.Should().Be(-123456);
    }

    [Fact(DisplayName = "SJS-039: Deserialize event message with null value")]
    public void SJS039()
    {
        var serializer = NewSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("42[\"hello\",null]") as IEventMessage;
        message.Should().NotBeNull();
        message!.Event.Should().Be("hello");
    }

    [Fact(DisplayName = "SJS-040: Serialize single object should produce correct JSON")]
    public void SJS040()
    {
        var serializer = NewSerializer(new JsonSerializerOptions());
        var json = serializer.Serialize(new { id = 1, name = "Alice" });
        json.Should().Be("{\"id\":1,\"name\":\"Alice\"}");
    }
}
