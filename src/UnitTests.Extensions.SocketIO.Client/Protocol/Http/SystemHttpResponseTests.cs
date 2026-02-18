using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Ave.Extensions.SocketIO.Client.Protocol.Http;

namespace UnitTests.Extensions.SocketIO.Client.Protocol.Http;

public class SystemHttpResponseTests
{
    [Fact(DisplayName = "SHR-001: MediaType should return content type media type from response")]
    public void SHR001()
    {
        var httpResponse = new HttpResponseMessage();
        httpResponse.Content = new StringContent("test", Encoding.UTF8, "application/json");
        var sut = new SystemHttpResponse(httpResponse);

        sut.MediaType.Should().Be("application/json");
    }

    [Fact(DisplayName = "SHR-002: MediaType when no content type should return null")]
    public void SHR002()
    {
        var httpResponse = new HttpResponseMessage();
        httpResponse.Content = new ByteArrayContent(new byte[0]);
        httpResponse.Content.Headers.ContentType = null;
        var sut = new SystemHttpResponse(httpResponse);

        sut.MediaType.Should().BeNull();
    }

    [Fact(DisplayName = "SHR-003: ReadAsStringAsync should return response body as string")]
    public async Task SHR003()
    {
        var httpResponse = new HttpResponseMessage();
        httpResponse.Content = new StringContent("hello world");
        var sut = new SystemHttpResponse(httpResponse);

        var result = await sut.ReadAsStringAsync();

        result.Should().Be("hello world");
    }

    [Fact(DisplayName = "SHR-004: ReadAsByteArrayAsync should return response body as byte array")]
    public async Task SHR004()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var httpResponse = new HttpResponseMessage();
        httpResponse.Content = new ByteArrayContent(bytes);
        var sut = new SystemHttpResponse(httpResponse);

        var result = await sut.ReadAsByteArrayAsync();

        result.Should().BeEquivalentTo(bytes);
    }
}
