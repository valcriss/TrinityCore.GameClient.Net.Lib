using Microsoft.Extensions.DependencyInjection;
using TrinityCore.GameClient.Net.Behavior.Abstractions;
using TrinityCore.GameClient.Net.Behavior.Combat;
using TrinityCore.GameClient.Net.Behavior.Goap;
using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Real;
using TrinityCore.GameClient.Net.GameState;
using TrinityCore.GameClient.Net.GameState.Abstractions;
using TrinityCore.GameClient.Net.Navigation.Abstractions;
using TrinityCore.GameClient.Net.Navigation.Mmap;
using TrinityCore.GameClient.Net.Navigation.Stubs;
using TrinityCore.GameClient.Net.Transport.Abstractions;
using TrinityCore.GameClient.Net.Transport.Tcp;

namespace TrinityCore.GameClient.Net.Lib;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTrinityCoreGameClientSkeleton(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionFactory, TcpConnectionFactory>();
        services.AddSingleton<IGameClient, RealGameClient>();
        services.AddSingleton<IGameStateStore, GameStateStore>();
        services.AddSingleton<IPathfinder, MmapPathfinder>();
        services.AddSingleton<INavigationService, StubNavigationService>();
        services.AddSingleton<IPlanner, StubPlanner>();
        services.AddSingleton<ITargetSelector, PveNearestTargetSelector>();
        return services;
    }
}
