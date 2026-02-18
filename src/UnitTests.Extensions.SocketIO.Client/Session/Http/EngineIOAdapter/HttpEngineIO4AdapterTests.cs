using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;
using Ave.Extensions.SocketIO.Protocol;
using Ave.Extensions.SocketIO.Serialization;

namespace UnitTests.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;

public class HttpEngineIO4AdapterTests
{
    private readonly HttpEngineIO4Adapter _sut;

    public HttpEngineIO4AdapterTests()
    {
        var mockStopwatch = new Mock<IStopwatch>();
        var mockHttpAdapter = new Mock<IHttpAdapter>();
        var mockRetryPolicy = new Mock<IRetriable>();
        var mockSerializer = new Mock<ISerializer>();
        var mockDelay = new Mock<IDelay>();
        var mockPollingHandler = new Mock<IPollingHandler>();

        _sut = new HttpEngineIO4Adapter(
            mockStopwatch.Object,
            mockHttpAdapter.Object,
            mockRetryPolicy.Object,
            mockSerializer.Object,
            mockDelay.Object,
            mockPollingHandler.Object);

        _sut.Options = new EngineIOAdapterOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
        };
    }

    [Fact(DisplayName = "HE4-001: ToHttpRequest with text should return content directly")]
    public void HE4001()
    {
        var result = _sut.ToHttpRequest("40");

        result.Method.Should().Be(RequestMethod.Post);
        result.BodyType.Should().Be(RequestBodyType.Text);
        result.BodyText.Should().Be("40");
    }

    [Fact(DisplayName = "HE4-002: ToHttpRequest with empty string should throw ArgumentException")]
    public void HE4002()
    {
        var act = () => _sut.ToHttpRequest(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "HE4-003: ToHttpRequest with bytes should return base64-encoded request")]
    public void HE4003()
    {
        var data = new byte[] { 1, 2, 3 };
        var bytes = new List<byte[]> { data };

        var result = _sut.ToHttpRequest(bytes);

        result.Method.Should().Be(RequestMethod.Post);
        result.BodyType.Should().Be(RequestBodyType.Text);
        result.BodyText.Should().StartWith("b");
        result.BodyText.Should().Be("b" + Convert.ToBase64String(data));
    }

    [Fact(DisplayName = "HE4-004: ToHttpRequest with empty bytes collection should throw ArgumentException")]
    public void HE4004()
    {
        var bytes = new List<byte[]>();

        var act = () => _sut.ToHttpRequest(bytes);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "HE4-005: ExtractMessagesFromText should parse text messages")]
    public void HE4005()
    {
        var text = "42[\"event\",\"data\"]";

        var messages = _sut.ExtractMessagesFromText(text).ToList();

        messages.Should().HaveCount(1);
        messages[0].Type.Should().Be(ProtocolMessageType.Text);
        messages[0].Text.Should().Be("42[\"event\",\"data\"]");
    }

    [Fact(DisplayName = "HE4-006: ExtractMessagesFromText should parse base64 binary messages")]
    public void HE4006()
    {
        var data = new byte[] { 1, 2, 3 };
        var text = "b" + Convert.ToBase64String(data);

        var messages = _sut.ExtractMessagesFromText(text).ToList();

        messages.Should().HaveCount(1);
        messages[0].Type.Should().Be(ProtocolMessageType.Bytes);
        messages[0].Bytes.Should().BeEquivalentTo(data);
    }

    [Fact(DisplayName = "HE4-007: ExtractMessagesFromText with mixed content should parse correctly")]
    public void HE4007()
    {
        var data = new byte[] { 1, 2, 3 };
        var text = "42[\"event\"]" + "\u001E" + "b" + Convert.ToBase64String(data);

        var messages = _sut.ExtractMessagesFromText(text).ToList();

        messages.Should().HaveCount(2);
        messages[0].Type.Should().Be(ProtocolMessageType.Text);
        messages[0].Text.Should().Be("42[\"event\"]");
        messages[1].Type.Should().Be(ProtocolMessageType.Bytes);
        messages[1].Bytes.Should().BeEquivalentTo(data);
    }

    [Fact(DisplayName = "HE4-008: ExtractMessagesFromBytes should return empty list")]
    public void HE4008()
    {
        var bytes = new byte[] { 1, 2, 3 };

        var messages = _sut.ExtractMessagesFromBytes(bytes).ToList();

        messages.Should().BeEmpty();
    }
}
