using Microsoft.AspNetCore.Builder;

namespace Ave.Extensions.SocketIO.Server.Middleware;

/// <summary>
/// Extension methods for adding Socket.IO middleware to the ASP.NET Core pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Socket.IO middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseSocketIO(this IApplicationBuilder app)
    {
        app.UseWebSockets();
        app.UseMiddleware<SocketIOMiddleware>();
        return app;
    }
}
