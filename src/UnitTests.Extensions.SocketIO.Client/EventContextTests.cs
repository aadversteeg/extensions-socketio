using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ave.Extensions.SocketIO.Client;
using Ave.Extensions.SocketIO.Messages;

namespace UnitTests.Extensions.SocketIO.Client;

public class EventContextTests
{
    [Fact(DisplayName = "ECT-001: GetValue should delegate to underlying message")]
    public void ECT001()
    {
        var mockMessage = new Mock<IDataMessage>();
        mockMessage.Setup(m => m.GetValue<string>(0)).Returns("hello");
        var mockClient = new Mock<IInternalSocketIOClient>();
        var context = new EventContext(mockMessage.Object, mockClient.Object);

        var result = context.GetValue<string>(0);

        result.Should().Be("hello");
    }

    [Fact(DisplayName = "ECT-002: GetValue with type should delegate to underlying message")]
    public void ECT002()
    {
        var mockMessage = new Mock<IDataMessage>();
        mockMessage.Setup(m => m.GetValue(typeof(int), 0)).Returns(42);
        var mockClient = new Mock<IInternalSocketIOClient>();
        var context = new EventContext(mockMessage.Object, mockClient.Object);

        var result = context.GetValue(typeof(int), 0);

        result.Should().Be(42);
    }

    [Fact(DisplayName = "ECT-003: RawText should delegate to underlying message")]
    public void ECT003()
    {
        var mockMessage = new Mock<IDataMessage>();
        mockMessage.Setup(m => m.RawText).Returns("[\"event\",\"data\"]");
        var mockClient = new Mock<IInternalSocketIOClient>();
        var context = new EventContext(mockMessage.Object, mockClient.Object);

        context.RawText.Should().Be("[\"event\",\"data\"]");
    }

    [Fact(DisplayName = "ECT-004: SendAckDataAsync should delegate to internal client")]
    public async Task ECT004()
    {
        var mockMessage = new Mock<IDataMessage>();
        mockMessage.Setup(m => m.Id).Returns(5);
        var mockClient = new Mock<IInternalSocketIOClient>();
        mockClient.Setup(c => c.SendAckDataAsync(5, It.IsAny<IEnumerable<object>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var context = new EventContext(mockMessage.Object, mockClient.Object);
        var data = new List<object> { "response" };

        await context.SendAckDataAsync(data);

        mockClient.Verify(c => c.SendAckDataAsync(5, data, CancellationToken.None), Times.Once);
    }
}
