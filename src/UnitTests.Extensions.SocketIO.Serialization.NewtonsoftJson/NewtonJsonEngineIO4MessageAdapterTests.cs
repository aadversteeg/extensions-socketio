using FluentAssertions;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Serialization.NewtonsoftJson;

namespace UnitTests.Extensions.SocketIO.Serialization.NewtonsoftJson;

public class NewtonJsonEngineIO4MessageAdapterTests
{
    private readonly NewtonJsonEngineIO4MessageAdapter _adapter = new NewtonJsonEngineIO4MessageAdapter();

    [Theory(DisplayName = "N4A-001: DeserializeConnectedMessage should parse sid and namespace")]
    [InlineData("{\"sid\":\"123\"}", "123", null)]
    [InlineData("/test,{\"sid\":\"123\"}", "123", "/test")]
    public void N4A001(string text, string sid, string? ns)
    {
        var message = _adapter.DeserializeConnectedMessage(text);
        message.Should()
            .BeEquivalentTo(new ConnectedMessage
            {
                Namespace = ns,
                Sid = sid,
            });
    }

    [Theory(DisplayName = "N4A-002: DeserializeErrorMessage should parse error message and namespace")]
    [InlineData("{\"message\":\"error message\"}", "error message", null)]
    [InlineData("/test,{\"message\":\"error message\"}", "error message", "/test")]
    public void N4A002(string text, string error, string? ns)
    {
        var message = _adapter.DeserializeErrorMessage(text);
        message.Should()
            .BeEquivalentTo(new ErrorMessage
            {
                Namespace = ns,
                Error = error,
            });
    }
}
