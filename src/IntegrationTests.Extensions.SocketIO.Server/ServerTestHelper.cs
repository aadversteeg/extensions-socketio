using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Server;
using Ave.Extensions.SocketIO.Server.Middleware;

namespace IntegrationTests.Extensions.SocketIO.Server;

public class ServerTestHelper : IAsyncDisposable
{
    private WebApplication? _app;

    public int Port { get; private set; }
    public Uri ServerUri => new($"http://localhost:{Port}");
    public ISocketIOServer Server { get; private set; } = null!;

    public static async Task<ServerTestHelper> CreateAndStartAsync(
        Action<SocketIOServerOptions>? configureOptions = null,
        Action<ISocketIOServer>? configureServer = null)
    {
        var helper = new ServerTestHelper();
        helper.Port = GetFreePort();

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSocketIO(configureOptions);
        builder.Services.AddCors(corsOptions =>
        {
            corsOptions.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });
        builder.WebHost.UseUrls($"http://localhost:{helper.Port}");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        helper._app = builder.Build();
        helper._app.UseCors();
        helper._app.UseSocketIO();

        helper.Server = helper._app.Services.GetRequiredService<ISocketIOServer>();
        configureServer?.Invoke(helper.Server);

        await helper._app.StartAsync().ConfigureAwait(false);
        await helper.WaitForReady().ConfigureAwait(false);

        return helper;
    }

    public async ValueTask DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync().ConfigureAwait(false);
            await _app.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private async Task WaitForReady()
    {
        using var httpClient = new HttpClient();
        var pollUrl = $"http://localhost:{Port}/socket.io/?EIO=4&transport=polling";
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await httpClient.GetAsync(pollUrl).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Server not ready yet
            }

            await Task.Delay(100).ConfigureAwait(false);
        }

        throw new TimeoutException($"Server did not become ready within 10 seconds on port {Port}");
    }
}
