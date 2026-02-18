using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ave.Extensions.SocketIO.Serialization.SystemTextJson;

namespace Ave.Extensions.SocketIO.Serialization.NewtonsoftJson
{
    /// <summary>
    /// Extension methods for registering Newtonsoft.Json serialization services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Newtonsoft.Json serializer with the specified settings.
        /// </summary>
        public static IServiceCollection AddNewtonsoftJsonSerializer(this IServiceCollection services, JsonSerializerSettings settings)
        {
            services.AddKeyedSingleton<IEngineIOMessageAdapter, NewtonJsonEngineIO3MessageAdapter>(EngineIOVersion.V3);
            services.AddKeyedSingleton<IEngineIOMessageAdapter, NewtonJsonEngineIO4MessageAdapter>(EngineIOVersion.V4);
            services.AddSingleton<IEngineIOMessageAdapterFactory>(sp =>
                new EngineIOMessageAdapterFactory(version =>
                    sp.GetRequiredKeyedService<IEngineIOMessageAdapter>(version)));
            services.AddSingleton<ISerializer, NewtonJsonSerializer>();
            services.AddSingleton(settings);
            return services;
        }

        /// <summary>
        /// Registers the Newtonsoft.Json serializer with default settings.
        /// </summary>
        public static IServiceCollection AddNewtonsoftJsonSerializer(this IServiceCollection services)
        {
            return services.AddNewtonsoftJsonSerializer(new JsonSerializerSettings());
        }
    }
}
