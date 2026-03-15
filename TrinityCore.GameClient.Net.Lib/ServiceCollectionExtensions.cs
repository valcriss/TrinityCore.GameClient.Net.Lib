using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TrinityCore.GameClient.Net.Behavior.Abstractions;
using TrinityCore.GameClient.Net.Behavior.Combat;
using TrinityCore.GameClient.Net.Behavior.Goap;
using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Real;
using TrinityCore.GameClient.Net.GameState;
using TrinityCore.GameClient.Net.GameState.Abstractions;
using TrinityCore.GameClient.Net.Navigation;
using TrinityCore.GameClient.Net.Navigation.Abstractions;
using TrinityCore.GameClient.Net.Navigation.Mmap;
using TrinityCore.GameClient.Net.Navigation.Stubs;
using TrinityCore.GameClient.Net.Transport.Abstractions;
using TrinityCore.GameClient.Net.Transport.Tcp;

namespace TrinityCore.GameClient.Net.Lib;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTrinityCoreGameClientCore(
        this IServiceCollection services,
        Action<TrinityCoreDataOptions>? configureData = null)
    {
        var dataOptions = new TrinityCoreDataOptions();
        configureData?.Invoke(dataOptions);

        services.AddSingleton(dataOptions);
        services.AddSingleton<IConnectionFactory, TcpConnectionFactory>();
        services.AddSingleton<IGameClient, RealGameClient>();
        services.AddSingleton<IGameStateStore, GameStateStore>();
        services.TryAddSingleton<IReadOnlyGameState>(sp => sp.GetRequiredService<IGameStateStore>());
        services.AddSingleton<IPathfinder, MmapPathfinder>();
        services.AddSingleton<IGameClientSession, GameClientSession>();
        return services;
    }

    public static IServiceCollection AddTrinityCoreBotDefaults(this IServiceCollection services)
    {
        services.TryAddSingleton<INavigationService, StubNavigationService>();
        services.TryAddSingleton<IPlanner, StubPlanner>();
        services.TryAddSingleton<ITargetSelector, PveNearestTargetSelector>();
        return services;
    }

    public static IServiceCollection AddTrinityCoreGameClientSkeleton(
        this IServiceCollection services,
        Action<TrinityCoreDataOptions>? configureData = null)
    {
        services.AddTrinityCoreGameClientCore(configureData);
        services.AddTrinityCoreBotDefaults();
        return services;
    }
}
