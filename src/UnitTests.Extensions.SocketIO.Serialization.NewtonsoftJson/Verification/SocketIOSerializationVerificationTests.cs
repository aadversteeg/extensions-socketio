using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Serialization.Decapsulation;
using Ave.Extensions.SocketIO.Serialization.NewtonsoftJson;
using Moq;

namespace UnitTests.Extensions.SocketIO.Serialization.NewtonsoftJson.Verification;

public class SocketIOSerializationVerificationTests
{
    private readonly NewtonJsonSerializer _serializer;

    public SocketIOSerializationVerificationTests()
    {
        var decapsulator = new Decapsulator();
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
        };
        _serializer = new NewtonJsonSerializer(decapsulator, settings);

        var mockAdapter = new Mock<IEngineIOMessageAdapter>();
        _serializer.SetEngineIOMessageAdapter(mockAdapter.Object);
    }

    [Fact(DisplayName = "VSS-001: EVENT encoding with data should produce '42[\"event\",\"data\"]'")]
    public void VSS001()
    {
        var data = new object[] { "event", "data" };

        var messages = _serializer.Serialize(data);

        messages.Should().HaveCount(1);
        messages[0].Text.Should().Be("42[\"event\",\"data\"]");
    }

    [Fact(DisplayName = "VSS-002: EVENT with namespace should produce '42/admin,[\"event\",\"data\"]'")]
    public void VSS002()
    {
        _serializer.Namespace = "/admin";

        var data = new object[] { "event", "data" };

        var messages = _serializer.Serialize(data);

        messages.Should().HaveCount(1);
        messages[0].Text.Should().Be("42/admin,[\"event\",\"data\"]");
    }

    [Fact(DisplayName = "VSS-003: EVENT with ack id should include id in prefix")]
    public void VSS003()
    {
        var data = new object[] { "event" };

        var messages = _serializer.Serialize(data, 12);

        messages.Should().HaveCount(1);
        messages[0].Text.Should().Be("4212[\"event\"]");
    }

    [Fact(DisplayName = "VSS-004: ACK response should match event id")]
    public void VSS004()
    {
        var data = new object[] { "result" };

        var messages = _serializer.SerializeAckData(data, 7);

        messages.Should().HaveCount(1);
        messages[0].Text.Should().Be("437[\"result\"]");
    }

    [Fact(DisplayName = "VSS-005: BINARY_EVENT should use placeholder mechanism")]
    public void VSS005()
    {
        var data = new object[] { "event", new byte[] { 1, 2, 3 } };

        var messages = _serializer.Serialize(data);

        messages.Should().HaveCount(2);
        messages[0].Text.Should().Contain("_placeholder");
        messages[0].Text.Should().Contain("\"num\":0");
        messages[0].Text.Should().StartWith("451-");
        messages[1].Bytes.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
    }

    [Fact(DisplayName = "VSS-006: BINARY_ACK should use placeholder mechanism")]
    public void VSS006()
    {
        var data = new object[] { new byte[] { 10, 20 } };

        var messages = _serializer.SerializeAckData(data, 5);

        messages.Should().HaveCount(2);
        messages[0].Text.Should().Contain("_placeholder");
        messages[0].Text.Should().StartWith("461-");
        messages[0].Text.Should().Contain("5");
    }

    [Fact(DisplayName = "VSS-007: EVENT with namespace and ack id should produce correct prefix")]
    public void VSS007()
    {
        _serializer.Namespace = "/admin";

        var data = new object[] { "event", "data" };

        var messages = _serializer.Serialize(data, 5);

        messages.Should().HaveCount(1);
        messages[0].Text.Should().Be("42/admin,5[\"event\",\"data\"]");
    }

    [Fact(DisplayName = "VSS-008: ACK with namespace should produce correct prefix")]
    public void VSS008()
    {
        _serializer.Namespace = "/admin";

        var data = new object[] { "result" };

        var messages = _serializer.SerializeAckData(data, 3);

        messages.Should().HaveCount(1);
        messages[0].Text.Should().Be("43/admin,3[\"result\"]");
    }

    [Fact(DisplayName = "VSS-009: Full handshake JSON via NewtonJsonSerializer should populate all OpenedMessage fields")]
    public void VSS009()
    {
        var json = "{\"sid\":\"abc123\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":20000,\"maxPayload\":1000000}";

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            },
        };

        var opened = JsonConvert.DeserializeObject<Ave.Extensions.SocketIO.Messages.OpenedMessage>(json, settings);

        opened.Should().NotBeNull();
        opened!.Sid.Should().Be("abc123");
        opened.PingInterval.Should().Be(25000);
        opened.PingTimeout.Should().Be(20000);
        opened.MaxPayload.Should().Be(1000000);
        opened.Upgrades.Should().ContainSingle().Which.Should().Be("websocket");
        opened.Type.Should().Be(Ave.Extensions.SocketIO.Messages.MessageType.Opened);
    }
}
