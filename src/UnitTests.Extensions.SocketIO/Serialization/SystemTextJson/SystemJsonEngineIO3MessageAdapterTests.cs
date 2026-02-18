using FluentAssertions;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Serialization.SystemTextJson;

namespace UnitTests.Extensions.SocketIO.Serialization.SystemTextJson;

public class SystemJsonEngineIO3MessageAdapterTests
{
    private readonly SystemJsonEngineIO3MessageAdapter _adapter = new SystemJsonEngineIO3MessageAdapter();

    [Theory(DisplayName = "E3A-001: DeserializeConnectedMessage should parse namespace correctly")]
    [InlineData("", null)]
    [InlineData("/nsp,", "/nsp")]
    public void E3A001(string text, string? ns)
    {
        var message = _adapter.DeserializeConnectedMessage(text);
        message.Should()
            .BeEquivalentTo(new ConnectedMessage
            {
                Namespace = ns,
                Sid = null,
            });
    }

    [Theory(DisplayName = "E3A-002: DeserializeErrorMessage should parse error string correctly")]
    [InlineData("\"error message\"", "error message")]
    [InlineData("\"\\\"Authentication error\\\"\"", "\"Authentication error\"")]
    public void E3A002(string text, string error)
    {
        var message = _adapter.DeserializeErrorMessage(text);
        message.Should()
            .BeEquivalentTo(new ErrorMessage
            {
                Namespace = null,
                Error = error,
            });
    }
}
