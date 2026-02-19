using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Ave.Extensions.SocketIO.Server.EngineIO.Transport;

/// <summary>
/// Handles HTTP long-polling transport for Engine.IO sessions.
/// </summary>
public interface IPollingTransportHandler
{
    /// <summary>
    /// Handles a GET request (long-poll read) for the specified session.
    /// </summary>
    Task HandleGetAsync(HttpContext context, IEngineIOSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Handles a POST request (client data) for the specified session.
    /// </summary>
    Task HandlePostAsync(HttpContext context, IEngineIOSession session, CancellationToken cancellationToken);
}
