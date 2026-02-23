using System.Reflection;
using Core.Server.Network;
using Core.Server.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Server.DependencyInjection;

public static class GameServerServiceCollectionExtensions
{
    public static IServiceCollection AddGameServerRuntime(this IServiceCollection services)
    {
        services.AddSingleton<PacketSystem>();
        services.AddSingleton<IPacketFactory>(sp => sp.GetRequiredService<PacketSystem>().Factory);
        services.AddSingleton<IPacketSizeRegistry>(sp => sp.GetRequiredService<PacketSystem>().Registry);
        services.AddSingleton<SessionManager>();
        return services;
    }

    public static IServiceCollection AddPacketHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetCustomAttribute<PacketHandlerAttribute>() != null);

        foreach (var handlerType in handlerTypes)
        {
            services.AddTransient(handlerType);
        }

        return services;
    }
}
