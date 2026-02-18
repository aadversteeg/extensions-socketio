using System;
using FluentAssertions;
using Ave.Extensions.SocketIO.Client;

namespace UnitTests.Extensions.SocketIO.Client;

public class SocketIOClientOptionsTests
{
    [Fact(DisplayName = "OPT-001: Default EIO version should be V4")]
    public void OPT001()
    {
        var options = new SocketIOClientOptions();
        options.EIO.Should().Be(Ave.Extensions.SocketIO.EngineIOVersion.V4);
    }

    [Fact(DisplayName = "OPT-002: Default connection timeout should be 30 seconds")]
    public void OPT002()
    {
        var options = new SocketIOClientOptions();
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact(DisplayName = "OPT-003: Default reconnection should be true")]
    public void OPT003()
    {
        var options = new SocketIOClientOptions();
        options.Reconnection.Should().BeTrue();
    }

    [Fact(DisplayName = "OPT-004: Default reconnection attempts should be 10")]
    public void OPT004()
    {
        var options = new SocketIOClientOptions();
        options.ReconnectionAttempts.Should().Be(10);
    }

    [Fact(DisplayName = "OPT-005: Default reconnection delay max should be 5000")]
    public void OPT005()
    {
        var options = new SocketIOClientOptions();
        options.ReconnectionDelayMax.Should().Be(5000);
    }

    [Fact(DisplayName = "OPT-006: Default auto upgrade should be true")]
    public void OPT006()
    {
        var options = new SocketIOClientOptions();
        options.AutoUpgrade.Should().BeTrue();
    }

    [Fact(DisplayName = "OPT-007: Setting reconnection attempts to 0 should throw ArgumentException")]
    public void OPT007()
    {
        var options = new SocketIOClientOptions();
        var act = () => options.ReconnectionAttempts = 0;
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "OPT-008: Setting reconnection attempts to negative should throw ArgumentException")]
    public void OPT008()
    {
        var options = new SocketIOClientOptions();
        var act = () => options.ReconnectionAttempts = -1;
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "OPT-009: Setting reconnection attempts to 1 should succeed")]
    public void OPT009()
    {
        var options = new SocketIOClientOptions();
        options.ReconnectionAttempts = 1;
        options.ReconnectionAttempts.Should().Be(1);
    }

    [Theory(DisplayName = "OPT-010: Path should be normalized with leading and trailing slashes")]
    [InlineData("socket.io", "/socket.io/")]
    [InlineData("/socket.io", "/socket.io/")]
    [InlineData("socket.io/", "/socket.io/")]
    [InlineData("/socket.io/", "/socket.io/")]
    public void OPT010(string input, string expected)
    {
        var options = new SocketIOClientOptions();
        options.Path = input;
        options.Path.Should().Be(expected);
    }

    [Fact(DisplayName = "OPT-011: Default transport should be Polling")]
    public void OPT011()
    {
        var options = new SocketIOClientOptions();
        options.Transport.Should().Be(Ave.Extensions.SocketIO.TransportProtocol.Polling);
    }

    [Fact(DisplayName = "OPT-012: Default query should be null")]
    public void OPT012()
    {
        var options = new SocketIOClientOptions();
        options.Query.Should().BeNull();
    }

    [Fact(DisplayName = "OPT-013: Default extra headers should be null")]
    public void OPT013()
    {
        var options = new SocketIOClientOptions();
        options.ExtraHeaders.Should().BeNull();
    }

    [Fact(DisplayName = "OPT-014: Default auth should be null")]
    public void OPT014()
    {
        var options = new SocketIOClientOptions();
        options.Auth.Should().BeNull();
    }
}
