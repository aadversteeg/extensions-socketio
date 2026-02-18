using System;
using FluentAssertions;
using Ave.Extensions.SocketIO.Client;

namespace UnitTests.Extensions.SocketIO.Client;

public class ConnectionExceptionTests
{
    [Fact(DisplayName = "CEX-001: ConnectionException with message should set message")]
    public void CEX001()
    {
        var ex = new ConnectionException("Connection failed");
        ex.Message.Should().Be("Connection failed");
    }

    [Fact(DisplayName = "CEX-002: ConnectionException with inner exception should set both")]
    public void CEX002()
    {
        var inner = new InvalidOperationException("Inner error");
        var ex = new ConnectionException("Connection failed", inner);
        ex.Message.Should().Be("Connection failed");
        ex.InnerException.Should().BeSameAs(inner);
    }
}
