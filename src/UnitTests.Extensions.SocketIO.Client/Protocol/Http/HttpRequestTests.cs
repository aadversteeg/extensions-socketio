using FluentAssertions;
using Ave.Extensions.SocketIO.Client.Protocol.Http;

namespace UnitTests.Extensions.SocketIO.Client.Protocol.Http;

public class HttpRequestTests
{
    [Fact(DisplayName = "HRQ-001: Default values should be correct")]
    public void HRQ001()
    {
        var request = new HttpRequest();

        request.Uri.Should().BeNull();
        request.Method.Should().Be(RequestMethod.Get);
        request.BodyType.Should().Be(RequestBodyType.Text);
        request.Headers.Should().NotBeNull().And.BeEmpty();
        request.BodyBytes.Should().BeNull();
        request.BodyText.Should().BeNull();
        request.IsConnect.Should().BeFalse();
    }
}
