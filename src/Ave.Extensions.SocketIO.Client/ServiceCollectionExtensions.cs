using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ave.Extensions.SocketIO.Client.Infrastructure;
using Ave.Extensions.SocketIO.Client.Protocol.Http;
using Ave.Extensions.SocketIO.Client.Protocol.WebSocket;
using Ave.Extensions.SocketIO.Client.Session;
using Ave.Extensions.SocketIO.Client.Session.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.Http;
using Ave.Extensions.SocketIO.Client.Session.Http.EngineIOAdapter;
using Ave.Extensions.SocketIO.Client.Session.WebSocket;
using Ave.Extensions.SocketIO.Client.Session.WebSocket.EngineIOAdapter;
using Ave.Extensions.SocketIO.Serialization;
using Ave.Extensions.SocketIO.Serialization.Decapsulation;
using Ave.Extensions.SocketIO.Serialization.SystemTextJson;

namespace Ave.Extensions.SocketIO.Client;

/// <summary>
/// Extension methods for configuring Socket.IO client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Builds a service provider with default Socket.IO client services.
    /// </summary>
    internal static IServiceProvider BuildServiceProvider(IServiceCollection services, Action<IServiceCollection>? configure = null)
    {
        services.AddLogging();
        services
            .AddSingleton<IStopwatch, SystemStopwatch>()
            .AddSingleton<IRandom, SystemRandom>()
            .AddSingleton<IDecapsulable, Decapsulator>()
            .AddSingleton<IRetriable, RandomDelayRetryPolicy>()
            .AddSingleton<IDelay, TaskDelay>()
            .AddSingleton<IEventRunner, EventRunner>();

        services
            .AddEngineIOCompatibility()
            .AddHttpSession()
            .AddWebSocketSession()
            .AddSystemTextJson(new JsonSerializerOptions());

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private static IServiceCollection AddEngineIOCompatibility(this IServiceCollection services)
    {
        services.AddScoped<IPollingHandler, PollingHandler>();
        services.AddScoped<IEngineIOAdapterFactory, EngineIOAdapterFactory>();
        services.AddKeyedScoped<IHttpEngineIOAdapter, HttpEngineIO3Adapter>(EngineIOCompatibility.HttpEngineIO3);
        services.AddKeyedScoped<IHttpEngineIOAdapter, HttpEngineIO4Adapter>(EngineIOCompatibility.HttpEngineIO4);
        services.AddKeyedScoped<IWebSocketEngineIOAdapter, WebSocketEngineIO3Adapter>(EngineIOCompatibility.WebSocketEngineIO3);
        services.AddKeyedScoped<IWebSocketEngineIOAdapter, WebSocketEngineIO4Adapter>(EngineIOCompatibility.WebSocketEngineIO4);
        return services;
    }

    private static IServiceCollection AddWebSocketSession(this IServiceCollection services)
    {
        services.AddScoped<IWebSocketClient, SystemClientWebSocket>();
        services.AddScoped<IWebSocketAdapter, WebSocketAdapter>();
        services.AddScoped<IWebSocketClientAdapter, SystemClientWebSocketAdapter>();
        services.AddKeyedScoped<ISession, WebSocketSession>(TransportProtocol.WebSocket);
        services.AddSingleton(new WebSocketOptions());
        return services;
    }

    private static IServiceCollection AddHttpSession(this IServiceCollection services)
    {
        services.AddSingleton<IHttpClient, SystemHttpClient>();
        services.AddScoped<IHttpAdapter, HttpAdapter>();
        services.AddSingleton<HttpClient>();
        services.AddKeyedScoped<ISession, HttpSession>(TransportProtocol.Polling);
        return services;
    }

    /// <summary>
    /// Registers the System.Text.Json serializer with the specified options.
    /// </summary>
    public static IServiceCollection AddSystemTextJson(this IServiceCollection services, JsonSerializerOptions options)
    {
        services.AddKeyedSingleton<IEngineIOMessageAdapter, SystemJsonEngineIO3MessageAdapter>(EngineIOVersion.V3);
        services.AddKeyedSingleton<IEngineIOMessageAdapter, SystemJsonEngineIO4MessageAdapter>(EngineIOVersion.V4);
        services.AddSingleton<IEngineIOMessageAdapterFactory>(sp =>
            new EngineIOMessageAdapterFactory(version =>
                sp.GetRequiredKeyedService<IEngineIOMessageAdapter>(version)));
        services.AddSingleton<ISerializer, SystemJsonSerializer>();
        services.AddSingleton(options);
        return services;
    }
}
