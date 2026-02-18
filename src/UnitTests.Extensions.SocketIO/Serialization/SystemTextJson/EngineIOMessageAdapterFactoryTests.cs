using System;
using FluentAssertions;
using Moq;
using Ave.Extensions.SocketIO;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Serialization.SystemTextJson;

namespace UnitTests.Extensions.SocketIO.Serialization.SystemTextJson;

public class EngineIOMessageAdapterFactoryTests
{
    [Fact(DisplayName = "FAC-001: Create with V3 should return the V3 adapter")]
    public void FAC001()
    {
        var v3Mock = new Mock<IEngineIOMessageAdapter>();
        var v4Mock = new Mock<IEngineIOMessageAdapter>();
        var factory = new EngineIOMessageAdapterFactory(version =>
            version == EngineIOVersion.V3 ? v3Mock.Object : v4Mock.Object);

        var result = factory.Create(EngineIOVersion.V3);

        result.Should().BeSameAs(v3Mock.Object);
    }

    [Fact(DisplayName = "FAC-002: Create with V4 should return the V4 adapter")]
    public void FAC002()
    {
        var v3Mock = new Mock<IEngineIOMessageAdapter>();
        var v4Mock = new Mock<IEngineIOMessageAdapter>();
        var factory = new EngineIOMessageAdapterFactory(version =>
            version == EngineIOVersion.V3 ? v3Mock.Object : v4Mock.Object);

        var result = factory.Create(EngineIOVersion.V4);

        result.Should().BeSameAs(v4Mock.Object);
    }
}
