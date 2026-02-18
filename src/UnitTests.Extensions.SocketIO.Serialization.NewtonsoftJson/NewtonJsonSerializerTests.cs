using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Serialization.Decapsulation;
using Ave.Extensions.SocketIO.Serialization.NewtonsoftJson;
using UnitTests.Extensions.SocketIO.Serialization.NewtonsoftJson.Helpers;

namespace UnitTests.Extensions.SocketIO.Serialization.NewtonsoftJson;

public class NewtonJsonSerializerTests
{
    private readonly Decapsulator _realDecapsulator = new Decapsulator();

    private NewtonJsonSerializer NewSerializer(IEngineIOMessageAdapter adapter)
    {
        return NewSerializer(adapter, new JsonSerializerSettings());
    }

    private NewtonJsonSerializer NewSerializer(IEngineIOMessageAdapter adapter, JsonSerializerSettings settings)
    {
        return NewSerializer(_realDecapsulator, adapter, settings);
    }

    private NewtonJsonSerializer NewSerializer(JsonSerializerSettings settings)
    {
        return NewSerializer(new Mock<IEngineIOMessageAdapter>().Object, settings);
    }

    private static NewtonJsonSerializer NewSerializer(
        IDecapsulable decapsulator,
        IEngineIOMessageAdapter adapter,
        JsonSerializerSettings settings)
    {
        var serializer = new NewtonJsonSerializer(decapsulator, settings);
        serializer.SetEngineIOMessageAdapter(adapter);
        return serializer;
    }

    [Fact(DisplayName = "NJS-001: Serialize with null data should throw ArgumentNullException")]
    public void NJS001()
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Invoking(x => x.Serialize(null!))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "NJS-002: Serialize with empty data should throw ArgumentException")]
    public void NJS002()
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Invoking(x => x.Serialize(new object[0]))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact(DisplayName = "NJS-003: Serialize event name only should produce correct text")]
    public void NJS003()
    {
        var serializer = NewSerializer(new JsonSerializerSettings());
        var list = serializer.Serialize(new object[] { "event" });
        list[0].Text.Should().Be("42[\"event\"]");
        list[0].Type.Should().Be(ProtocolMessageType.Text);
    }

    [Fact(DisplayName = "NJS-004: Serialize event with null value should include null in JSON")]
    public void NJS004()
    {
        var serializer = NewSerializer(new JsonSerializerSettings());
        var list = serializer.Serialize(new object[] { "event", null! });
        list[0].Text.Should().Be("42[\"event\",null]");
    }

    [Fact(DisplayName = "NJS-005: Serialize event with string should produce correct text")]
    public void NJS005()
    {
        var serializer = NewSerializer(new JsonSerializerSettings());
        var list = serializer.Serialize(new object[] { "event", "hello, world!" });
        list[0].Text.Should().Be("42[\"event\",\"hello, world!\"]");
    }

    [Fact(DisplayName = "NJS-006: Serialize event with true should produce correct text")]
    public void NJS006()
    {
        var serializer = NewSerializer(new JsonSerializerSettings());
        var list = serializer.Serialize(new object[] { "event", true });
        list[0].Text.Should().Be("42[\"event\",true]");
    }

    [Fact(DisplayName = "NJS-007: Serialize event with false should produce correct text")]
    public void NJS007()
    {
        var serializer = NewSerializer(new JsonSerializerSettings());
        var list = serializer.Serialize(new object[] { "event", false });
        list[0].Text.Should().Be("42[\"event\",false]");
    }

    [Fact(DisplayName = "NJS-008: Serialize event with int max should produce correct text")]
    public void NJS008()
    {
        var serializer = NewSerializer(new JsonSerializerSettings());
        var list = serializer.Serialize(new object[] { "event", int.MaxValue });
        list[0].Text.Should().Be("42[\"event\",2147483647]");
    }

    [Fact(DisplayName = "NJS-009: Serialize event with object should produce correct text")]
    public void NJS009()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
        };
        var serializer = NewSerializer(settings);
        var list = serializer.Serialize(new object[] { "event", new { Id = 1, Name = "Alice" } });
        list[0].Text.Should().Be("42[\"event\",{\"id\":1,\"name\":\"Alice\"}]");
    }

    [Theory(DisplayName = "NJS-010: Serialize with namespace and no bytes should include namespace prefix")]
    [InlineData(null, "42[\"event\"]")]
    [InlineData("", "42[\"event\"]")]
    [InlineData("/test", "42/test,[\"event\"]")]
    public void NJS010(string? ns, string expected)
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Namespace = ns;
        var list = serializer.Serialize(new object[] { "event" });
        list[0].Text.Should().Be(expected);
    }

    [Theory(DisplayName = "NJS-011: Serialize with namespace and bytes should include binary prefix")]
    [InlineData(null, "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("", "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("/test", "451-/test,[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    public void NJS011(string? ns, string expected)
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Namespace = ns;
        var list = serializer.Serialize(new object[] { "event", TestFile.NiuB.Bytes });
        list[0].Text.Should().Be(expected);
    }

    [Fact(DisplayName = "NJS-012: Serialize with bytes should return text and binary protocol messages")]
    public void NJS012()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
        };
        var serializer = NewSerializer(settings);
        var list = serializer.Serialize(new object[] { "event", TestFile.IndexHtml });
        list.Should().HaveCount(2);
        list[0].Type.Should().Be(ProtocolMessageType.Text);
        list[1].Type.Should().Be(ProtocolMessageType.Bytes);
        list[1].Bytes.Should().BeEquivalentTo(TestFile.IndexHtml.Bytes);
    }

    [Fact(DisplayName = "NJS-013: Deserialize opened message should return OpenedMessage")]
    public void NJS013()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("0{\"sid\":\"123\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
        message.Should().BeEquivalentTo(new OpenedMessage
        {
            Sid = "123",
            Upgrades = new List<string> { "websocket" },
            PingInterval = 10000,
            PingTimeout = 5000,
        });
    }

    [Fact(DisplayName = "NJS-014: Deserialize EIO3 connected message with namespace")]
    public void NJS014()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO3MessageAdapter());
        var message = serializer.Deserialize("40/test,");
        message.Should().BeEquivalentTo(new ConnectedMessage { Namespace = "/test" });
    }

    [Fact(DisplayName = "NJS-015: Deserialize EIO4 connected message with sid")]
    public void NJS015()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("40{\"sid\":\"123\"}");
        message.Should().BeEquivalentTo(new ConnectedMessage { Sid = "123" });
    }

    [Fact(DisplayName = "NJS-016: Deserialize empty string should return null")]
    public void NJS016()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        serializer.Deserialize("").Should().BeNull();
    }

    [Fact(DisplayName = "NJS-017: Deserialize unsupported text should return null")]
    public void NJS017()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        serializer.Deserialize("unsupported text").Should().BeNull();
    }

    [Fact(DisplayName = "NJS-018: Deserialize ping should return PingMessage")]
    public void NJS018()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        serializer.Deserialize("2").Should().BeOfType<PingMessage>();
    }

    [Fact(DisplayName = "NJS-019: Deserialize pong should return PongMessage")]
    public void NJS019()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        serializer.Deserialize("3").Should().BeOfType<PongMessage>();
    }

    [Fact(DisplayName = "NJS-020: Deserialize disconnected message should parse namespace")]
    public void NJS020()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("41/test,");
        message.Should().BeEquivalentTo(new DisconnectedMessage { Namespace = "/test" });
    }

    [Fact(DisplayName = "NJS-021: Deserialize event message should parse event name and data")]
    public void NJS021()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("42[\"hello\",\"world\"]") as IEventMessage;
        message.Should().NotBeNull();
        message!.Event.Should().Be("hello");
        message.GetValue<string>(0).Should().Be("world");
    }

    [Fact(DisplayName = "NJS-022: Deserialize event message with namespace and id")]
    public void NJS022()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("42/test,2[\"event\"]") as IEventMessage;
        message.Should().NotBeNull();
        message!.Event.Should().Be("event");
        message.Id.Should().Be(2);
        message.Namespace.Should().Be("/test");
    }

    [Fact(DisplayName = "NJS-023: Deserialize ack message should parse id and namespace")]
    public void NJS023()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("43/test,1[\"nice\"]") as IDataMessage;
        message.Should().NotBeNull();
        message!.Id.Should().Be(1);
        message.Namespace.Should().Be("/test");
        message.GetValue<string>(0).Should().Be("nice");
    }

    [Fact(DisplayName = "NJS-024: Deserialize EIO3 error message")]
    public void NJS024()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO3MessageAdapter());
        var message = serializer.Deserialize("44\"Authentication error\"");
        message.Should().BeEquivalentTo(new ErrorMessage
        {
            Namespace = null,
            Error = "Authentication error",
        });
    }

    [Fact(DisplayName = "NJS-025: Deserialize EIO4 error message with namespace")]
    public void NJS025()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize("44/test,{\"message\":\"Authentication error\"}");
        message.Should().BeEquivalentTo(new ErrorMessage
        {
            Namespace = "/test",
            Error = "Authentication error",
        });
    }

    [Fact(DisplayName = "NJS-026: Deserialize binary event message with bytes should resolve placeholder")]
    public void NJS026()
    {
        var serializer = NewSerializer(new NewtonJsonEngineIO4MessageAdapter());
        var message = (IBinaryAckMessage)serializer.Deserialize("451-/test,30[\"event\",{\"Size\":666,\"Name\":\"NiuB\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]")!;
        message.Add(TestFile.NiuB.Bytes);
        var item = message.GetValue<TestFile>(0);
        item.Should().BeEquivalentTo(TestFile.NiuB);
    }

    [Theory(DisplayName = "NJS-027: Serialize with packetId should include namespace and id")]
    [InlineData(null, 1, "421[\"event\"]")]
    [InlineData("", 2, "422[\"event\"]")]
    [InlineData("/test", 3, "42/test,3[\"event\"]")]
    public void NJS027(string? ns, int id, string expected)
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Namespace = ns;
        var list = serializer.Serialize(new object[] { "event" }, id);
        list[0].Text.Should().Be(expected);
    }

    [Theory(DisplayName = "NJS-028: SerializeAckData should produce ack prefix")]
    [InlineData(null, 1, "431[1,\"2\"]")]
    [InlineData("", 2, "432[1,\"2\"]")]
    [InlineData("/test", 3, "43/test,3[1,\"2\"]")]
    public void NJS028(string? ns, int id, string expected)
    {
        var serializer = NewSerializer(new Mock<IEngineIOMessageAdapter>().Object);
        serializer.Namespace = ns;
        var list = serializer.SerializeAckData(new object[] { 1, "2" }, id);
        list[0].Text.Should().Be(expected);
    }

    [Fact(DisplayName = "NJS-029: Serialize with CamelCase option should produce camelCase JSON")]
    public void NJS029()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
        };
        var serializer = NewSerializer(settings);
        var json = serializer.Serialize(new
        {
            User = "admin",
            Password = "123456",
        });
        json.Should().Be("{\"user\":\"admin\",\"password\":\"123456\"}");
    }

    [Fact(DisplayName = "NJS-030: Deserialize when decapsulation fails should return null")]
    public void NJS030()
    {
        var mockDecapsulator = new Mock<IDecapsulable>();
        mockDecapsulator.Setup(d => d.DecapsulateRawText(It.IsAny<string>()))
            .Returns(new DecapsulationResult { Success = false });
        var adapter = new NewtonJsonEngineIO4MessageAdapter();
        var serializer = NewSerializer(mockDecapsulator.Object, adapter, new JsonSerializerSettings());
        serializer.Deserialize("anything").Should().BeNull();
    }

    [Theory(DisplayName = "NJS-031: Deserialize DataMessage RawText should be the JSON array")]
    [InlineData("42[\"event\"]", "[\"event\"]")]
    [InlineData("421[\"event\"]", "[\"event\"]")]
    [InlineData("42/test,2[\"event\"]", "[\"event\"]")]
    [InlineData("43/test,1[\"nice\"]", "[\"nice\"]")]
    public void NJS031(string raw, string expected)
    {
        var serializer = NewSerializer(new JsonSerializerSettings());
        var message = (IDataMessage)serializer.Deserialize(raw)!;
        message.RawText.Should().Be(expected);
    }

    [Fact(DisplayName = "NJS-032: Serialize single object should produce correct JSON")]
    public void NJS032()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
        };
        var serializer = NewSerializer(settings);
        var json = serializer.Serialize(new { Id = 1, Name = "Alice" });
        json.Should().Be("{\"id\":1,\"name\":\"Alice\"}");
    }
}
