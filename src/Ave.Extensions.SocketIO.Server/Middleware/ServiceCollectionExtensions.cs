using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Serialization.Decapsulation;
using Ave.Extensions.SocketIO.Serialization.SystemTextJson;
using Ave.Extensions.SocketIO.Server.EngineIO;
using Ave.Extensions.SocketIO.Server.EngineIO.Codec;
using Ave.Extensions.SocketIO.Server.EngineIO.Transport;

namespace Ave.Extensions.SocketIO.Server.Middleware;

/// <summary>
/// Extension methods for registering Socket.IO server services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Socket.IO server services with the dependency injection container.
    /// </summary>
    public static IServiceCollection AddSocketIO(
        this IServiceCollection services,
        Action<SocketIOServerOptions>? configure = null)
    {
        var options = new SocketIOServerOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // Engine.IO
        services.AddSingleton<ISessionIdGenerator, SessionIdGenerator>();
        services.AddSingleton<IEngineIOSessionStore, EngineIOSessionStore>();

        // Payload codecs
        services.AddSingleton<EngineIO3PayloadCodec>();
        services.AddSingleton<EngineIO4PayloadCodec>();

        // WebSocket frame codecs
        services.AddSingleton<WebSocketFrameCodecV3>();
        services.AddSingleton<WebSocketFrameCodecV4>();

        // Transport handlers
        services.AddSingleton<IPollingTransportHandler, PollingTransportHandler>();
        services.AddSingleton<IWebSocketTransportHandler, WebSocketTransportHandler>();

        // Serialization (reuse from core library)
        services.AddSingleton<IDecapsulable, Decapsulator>();
        services.AddSingleton<ISerializer>(sp =>
        {
            var decapsulator = sp.GetRequiredService<IDecapsulable>();
            var jsonOptions = new JsonSerializerOptions();
            return new SystemJsonSerializer(decapsulator, jsonOptions);
        });

        // Socket.IO layer
        services.AddSingleton<SocketIOServer>();
        services.AddSingleton<ISocketIOServer>(sp => sp.GetRequiredService<SocketIOServer>());
        services.AddSingleton<MessageRouter>();
        services.AddSingleton<IMessageRouter>(sp => sp.GetRequiredService<MessageRouter>());

        services.AddLogging();

        return services;
    }
}
