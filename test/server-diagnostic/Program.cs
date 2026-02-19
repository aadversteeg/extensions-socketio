using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Server;
using Ave.Extensions.SocketIO.Server.Middleware;

var builder = WebApplication.CreateBuilder();
builder.Services.AddSocketIO();
builder.WebHost.UseUrls("http://localhost:19876");

var app = builder.Build();
app.UseSocketIO();

var server = app.Services.GetRequiredService<ISocketIOServer>();
server.OnConnection(socket =>
{
    Console.WriteLine("Connection received: " + socket.Id);
    return System.Threading.Tasks.Task.CompletedTask;
});

await app.StartAsync();
Console.WriteLine("Server started on port 19876");

using var client = new HttpClient();
try
{
    var resp = await client.GetAsync("http://localhost:19876/socket.io/?EIO=4&transport=polling");
    var body = await resp.Content.ReadAsStringAsync();
    Console.WriteLine("Polling response: " + resp.StatusCode + " -> " + body);
}
catch (Exception ex)
{
    Console.WriteLine("Polling error: " + ex.Message);
}

await app.StopAsync();
Console.WriteLine("Server stopped");
