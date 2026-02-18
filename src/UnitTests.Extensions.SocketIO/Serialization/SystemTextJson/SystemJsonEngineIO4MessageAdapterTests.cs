using FluentAssertions;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Serialization.SystemTextJson;

namespace UnitTests.Extensions.SocketIO.Serialization.SystemTextJson;

public class SystemJsonEngineIO4MessageAdapterTests
{
    private readonly SystemJsonEngineIO4MessageAdapter _adapter = new SystemJsonEngineIO4MessageAdapter();

    [Theory(DisplayName = "E4A-001: DeserializeConnectedMessage should parse sid and namespace correctly")]
    [InlineData("{\"sid\":\"123\"}", "123", null)]
    [InlineData("/test,{\"sid\":\"123\"}", "123", "/test")]
    public void E4A001(string text, string sid, string? ns)
    {
        var message = _adapter.DeserializeConnectedMessage(text);
        message.Should()
            .BeEquivalentTo(new ConnectedMessage
            {
                Namespace = ns,
                Sid = sid,
            });
    }

    [Theory(DisplayName = "E4A-002: DeserializeErrorMessage should parse error message and namespace correctly")]
    [InlineData("{\"message\":\"error message\"}", "error message", null)]
    [InlineData("/test,{\"message\":\"error message\"}", "error message", "/test")]
    public void E4A002(string text, string error, string? ns)
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
