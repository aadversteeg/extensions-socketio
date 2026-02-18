using FluentAssertions;
using Ave.Extensions.SocketIO.Messages;
using Ave.Extensions.SocketIO.Serialization.NewtonsoftJson;

namespace UnitTests.Extensions.SocketIO.Serialization.NewtonsoftJson;

public class NewtonJsonEngineIO3MessageAdapterTests
{
    private readonly NewtonJsonEngineIO3MessageAdapter _adapter = new NewtonJsonEngineIO3MessageAdapter();

    [Theory(DisplayName = "N3A-001: DeserializeConnectedMessage should parse namespace correctly")]
    [InlineData("", null)]
    [InlineData("/nsp,", "/nsp")]
    public void N3A001(string text, string? ns)
    {
        var message = _adapter.DeserializeConnectedMessage(text);
        message.Should()
            .BeEquivalentTo(new ConnectedMessage
            {
                Namespace = ns,
                Sid = null,
            });
    }

    [Theory(DisplayName = "N3A-002: DeserializeErrorMessage should parse error string correctly")]
    [InlineData("\"error message\"", "error message")]
    [InlineData("\"\\\"Authentication error\\\"\"", "\"Authentication error\"")]
    public void N3A002(string text, string error)
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
