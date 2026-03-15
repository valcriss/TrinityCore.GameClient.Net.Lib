using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Models;
using TrinityCore.GameClient.Net.GameState.Abstractions;
using TrinityCore.GameClient.Net.Lib;

var settingsPath = Path.Combine(AppContext.BaseDirectory, "botsettings.json");
var settings = await BotSettings.LoadAsync(settingsPath);
var logsDirectory = Path.Combine(AppContext.BaseDirectory, settings.Logging.Directory);
Directory.CreateDirectory(logsDirectory);
var sampleLogPath = Path.Combine(logsDirectory, "sample.log");
var packetLogPath = Path.Combine(logsDirectory, "packets.log");
await ResetRunArtifactsAsync(sampleLogPath, packetLogPath);
if (settings.Logging.EnablePacketTrace)
{
    Trace.Listeners.Clear();
    Trace.Listeners.Add(new TextWriterTraceListener(packetLogPath));
    Trace.AutoFlush = true;
}

await using var services = new ServiceCollection()
    .AddTrinityCoreGameClientCore()
    .BuildServiceProvider();

var gameClient = services.GetRequiredService<IGameClient>();
var gameState = services.GetRequiredService<IGameStateStore>();
var gameSession = services.GetRequiredService<IGameClientSession>();

while (true)
{
    if (!Console.IsOutputRedirected)
    {
        Console.Clear();
    }

    Console.WriteLine("TrinityCore.GameClient.Net.Lib.Sample");
    Console.WriteLine($"Protocol cible: {PublicApi.SupportedProtocolVersion}");
    Console.WriteLine("1. Tester connexion auth");
    Console.WriteLine("2. Lister les realms");
    Console.WriteLine("3. Login complet (auth -> realm -> character -> enter world)");
    Console.WriteLine("4. Afficher config");
    Console.WriteLine("5. Logout (avec timeout configure)");
    Console.WriteLine("6. Afficher GameState");
    Console.WriteLine("7. Selection auto cible (premiere entite connue)");
    Console.WriteLine("8. Test deplacement simple (+X)");
    Console.WriteLine("0. Quitter");
    Console.Write("Choix: ");

    var choice = Console.ReadLine();
    try
    {
        switch (choice)
        {
            case "1":
                await TestAuthAsync(gameClient, settings, sampleLogPath);
                break;
            case "2":
                await ListRealmsAsync(gameClient, sampleLogPath);
                break;
            case "3":
                await FullLoginAsync(gameSession, settings, sampleLogPath);
                break;
            case "4":
                PrintSettings(settings);
                break;
            case "5":
                await LogoutAsync(gameSession, settings, sampleLogPath);
                break;
            case "6":
                PrintGameState(gameState);
                break;
            case "7":
                await SelectTargetAsync(gameClient, logPath: sampleLogPath);
                break;
            case "8":
                await TestMoveAsync(gameClient, gameState, settings, sampleLogPath);
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Choix invalide");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur: {ex.Message}");
        await WriteLogAsync(sampleLogPath, $"ERROR {ex}");
    }

    Console.WriteLine("\nEntree pour continuer...");
    Console.ReadLine();
}

static async Task TestAuthAsync(IGameClient gameClient, BotSettings settings, string logPath)
{
    await WriteLogAsync(logPath, "Auth test started");
    var ok = await gameClient.LoginAsync(
        new AuthServerInfo(settings.Connection.Host, settings.Connection.Port),
        new AuthServerCredentials(settings.Connection.Login, settings.Connection.Password));

    Console.WriteLine(ok ? "Auth OK" : "Auth KO");
    await WriteLogAsync(logPath, ok ? "Auth success" : "Auth failed");
}

static async Task ListRealmsAsync(IGameClient gameClient, string logPath)
{
    await WriteLogAsync(logPath, "Realm list requested");
    var realms = await gameClient.GetRealmsAsync();
    if (realms.Count == 0)
    {
        Console.WriteLine("Aucun realm recu");
        await WriteLogAsync(logPath, "No realms returned");
        return;
    }

    foreach (var realm in realms)
    {
        Console.WriteLine($"- [{realm.Id}] {realm.Name} ({realm.Address}:{realm.Port})");
    }
}

static async Task FullLoginAsync(IGameClientSession gameSession, BotSettings settings, string logPath)
{
    await WriteLogAsync(logPath, "Full login started");
    var worldOk = await gameSession.ConnectAsync(new GameClientSessionOptions(
        new AuthServerInfo(settings.Connection.Host, settings.Connection.Port),
        new AuthServerCredentials(settings.Connection.Login, settings.Connection.Password),
        settings.Connection.RealmName,
        settings.Connection.WorldHost,
        settings.Connection.WorldPort,
        settings.Connection.CharacterName,
        settings.Connection.LogoutTimeoutSeconds));

    var characterName = gameSession.CurrentCharacter?.Name ?? settings.Connection.CharacterName;
    Console.WriteLine(worldOk ? $"Entree monde OK ({characterName})" : "Entree monde KO");
    await WriteLogAsync(logPath, worldOk ? $"Full login success with {characterName}" : "Full login failed");
}

static void PrintSettings(BotSettings settings)
{
    Console.WriteLine(JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
}

static async Task LogoutAsync(IGameClientSession gameSession, BotSettings settings, string logPath)
{
    await WriteLogAsync(logPath, $"Logout started timeout={settings.Connection.LogoutTimeoutSeconds}s");
    var ok = await gameSession.DisconnectAsync(settings.Connection.LogoutTimeoutSeconds);
    Console.WriteLine(ok ? "Logout OK" : "Logout annule/KO");
    await WriteLogAsync(logPath, ok ? "Logout success" : "Logout canceled or failed");
}

static async Task SelectTargetAsync(IGameClient gameClient, string logPath)
{
    await WriteLogAsync(logPath, "Select target started (auto)");
    var ok = await gameClient.SelectTargetAsync(0);
    Console.WriteLine(ok ? "Selection cible envoyee" : "Aucune cible disponible");
    await WriteLogAsync(logPath, ok ? "Select target success" : "Select target failed (no candidate)");
}

static async Task TestMoveAsync(IGameClient gameClient, IGameStateStore gameState, BotSettings settings, string logPath)
{
    var player = gameState.Snapshot.Player;
    if (player is null)
    {
        Console.WriteLine("Player snapshot indisponible");
        return;
    }

    var destination = new TrinityCore.GameClient.Net.Navigation.Abstractions.NavigationPoint(player.X + 8.0f, player.Y, player.Z);
    await WriteLogAsync(logPath, $"Move test start to ({destination.X:F2}, {destination.Y:F2}, {destination.Z:F2})");
    var ok = await gameClient.MoveToAsync(
        destination,
        settings.Navigation.MeleeRange,
        settings.Navigation.RepathIntervalMs,
        settings.Navigation.StuckTimeoutMs);
    Console.WriteLine(ok ? "Move test OK" : "Move test KO");
    await WriteLogAsync(logPath, ok ? "Move test success" : "Move test failed");
}

static void PrintGameState(IGameStateStore gameState)
{
    Console.WriteLine(JsonSerializer.Serialize(gameState.Snapshot, new JsonSerializerOptions { WriteIndented = true }));
}

static async Task WriteLogAsync(string logPath, string message)
{
    var line = $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}";
    await File.AppendAllTextAsync(logPath, line);
}

static async Task ResetRunArtifactsAsync(params string[] filePaths)
{
    foreach (var filePath in filePaths)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            continue;
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, string.Empty);
    }
}

internal sealed class BotSettings
{
    public required ConnectionSettings Connection { get; init; }
    public required CombatSettings Combat { get; init; }
    public required NavigationSettings Navigation { get; init; }
    public required BehaviorSettings Behavior { get; init; }
    public required LoggingSettings Logging { get; init; }

    public static async Task<BotSettings> LoadAsync(string path)
    {
        if (!File.Exists(path))
        {
            var defaults = CreateDefault();
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true }));
            return defaults;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var loaded = JsonSerializer.Deserialize<BotSettings>(json);
            if (loaded is not null)
            {
                return loaded;
            }
        }
        catch
        {
            // fallback to defaults
        }

        var defaultsFallback = CreateDefault();
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(defaultsFallback, new JsonSerializerOptions { WriteIndented = true }));
        return defaultsFallback;
    }

    private static BotSettings CreateDefault()
    {
        return new BotSettings
        {
            Connection = new ConnectionSettings
            {
                Host = "127.0.0.1",
                Port = 3724,
                Login = "test",
                Password = "test",
                RealmName = "TrinityCore",
                WorldHost = "127.0.0.1",
                WorldPort = 8085,
                LogoutTimeoutSeconds = 25,
                CharacterName = "test"
            },
            Combat = new CombatSettings
            {
                AggroRadius = 35,
                MaxTargetLevelDelta = -1,
                NoHpChangeTimeoutSeconds = 10
            },
            Navigation = new NavigationSettings
            {
                RepathIntervalMs = 750,
                MeleeRange = 2.5f,
                StuckTimeoutMs = 3000
            },
            Behavior = new BehaviorSettings
            {
                TickMs = 150,
                EnableReleaseOnDeath = true
            },
            Logging = new LoggingSettings
            {
                MinLevel = "Debug",
                Directory = "logs",
                EnablePacketTrace = false
            }
        };
    }
}

internal sealed class ConnectionSettings
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Login { get; init; }
    public required string Password { get; init; }
    public required string RealmName { get; init; }
    public required string WorldHost { get; init; }
    public required int WorldPort { get; init; }
    public required int LogoutTimeoutSeconds { get; init; }
    public required string CharacterName { get; init; }
}

internal sealed class CombatSettings
{
    public required int AggroRadius { get; init; }
    public required int MaxTargetLevelDelta { get; init; }
    public required int NoHpChangeTimeoutSeconds { get; init; }
}

internal sealed class NavigationSettings
{
    public required int RepathIntervalMs { get; init; }
    public required float MeleeRange { get; init; }
    public required int StuckTimeoutMs { get; init; }
}

internal sealed class BehaviorSettings
{
    public required int TickMs { get; init; }
    public required bool EnableReleaseOnDeath { get; init; }
}

internal sealed class LoggingSettings
{
    public required string MinLevel { get; init; }
    public required string Directory { get; init; }
    public required bool EnablePacketTrace { get; init; }
}
