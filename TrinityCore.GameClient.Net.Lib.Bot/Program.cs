using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TrinityCore.GameClient.Net.Behavior.Abstractions;
using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Models;
using TrinityCore.GameClient.Net.GameState.Abstractions;
using TrinityCore.GameClient.Net.GameState.Model;
using TrinityCore.GameClient.Net.Lib;
using TrinityCore.GameClient.Net.Navigation.Abstractions;

var settingsPath = Path.Combine(AppContext.BaseDirectory, "botsettings.json");
var settings = await BotSettings.LoadAsync(settingsPath);
var logsDirectory = Path.Combine(AppContext.BaseDirectory, settings.Logging.Directory);
Directory.CreateDirectory(logsDirectory);
var botLogPath = Path.Combine(logsDirectory, "bot.log");
var followGroundCsvPath = Path.Combine(logsDirectory, "follow-ground.csv");
var runSummaryJsonlPath = Path.Combine(logsDirectory, "run-summary.jsonl");

if (settings.Behavior.FollowNearestPlayer)
{
    await EnsureFollowGroundCsvHeaderAsync(followGroundCsvPath);
}

if (settings.Logging.EnablePacketTrace)
{
    Trace.Listeners.Clear();
    Trace.Listeners.Add(new TextWriterTraceListener(Path.Combine(logsDirectory, "bot-packets.log")));
    Trace.AutoFlush = true;
}

await using var services = new ServiceCollection()
    .AddTrinityCoreGameClientSkeleton()
    .BuildServiceProvider();

var gameClient = services.GetRequiredService<IGameClient>();
var gameState = services.GetRequiredService<IGameStateStore>();
var targetSelector = services.GetRequiredService<ITargetSelector>();
var pathfinder = services.GetRequiredService<IPathfinder>();

Console.WriteLine($"TrinityCore Bot - Protocol: {PublicApi.SupportedProtocolVersion}");

if (!await FullLoginAsync(gameClient, settings, botLogPath))
{
    Console.WriteLine("Login bot KO");
    return;
}

Console.WriteLine("Bot en marche.");
await WriteLogAsync(botLogPath, "BOT STARTED");

var options = new TargetSelectionOptions(settings.Combat.AggroRadius, settings.Combat.MaxTargetLevelDelta);
var meleeState = new MeleeCombatState();
var deathRecoveryState = new DeathRecoveryState();
var runMetrics = new RunMetrics(DateTimeOffset.UtcNow);
var anomalyState = new BotAnomalyState();
var factionTemplates = LoadFactionTemplateStore();
var explorationState = new ExplorationState();
var activityState = new BotActivityState();

var playerAtStart = gameState.Snapshot.Player;
if (settings.Behavior.EnableStartupMoveTest && playerAtStart is not null)
{
    var moveDestination = new TrinityCore.GameClient.Net.Navigation.Abstractions.NavigationPoint(
        playerAtStart.X + settings.Behavior.StartMoveXOffset,
        playerAtStart.Y,
        playerAtStart.Z);
    var moveOk = await gameClient.MoveToAsync(
        moveDestination,
        settings.Behavior.ArrivalRadius,
        settings.Navigation.RepathIntervalMs,
        settings.Navigation.StuckTimeoutMs);
    await WriteLogAsync(botLogPath, moveOk ? "MOVE TEST OK" : "MOVE TEST KO");
}

using var cts = settings.Behavior.RunDurationSeconds > 0
    ? new CancellationTokenSource(TimeSpan.FromSeconds(settings.Behavior.RunDurationSeconds))
    : new CancellationTokenSource();

try
{
    var followHoldZone = false;
    while (!cts.IsCancellationRequested)
    {
        var snapshot = gameState.Snapshot;
        if (snapshot.Player is not null)
        {
            var deathHandled = await RunPhaseWithWatchdogAsync(
                "DEATH_RECOVERY",
                settings,
                botLogPath,
                ct => RunDeathRecoveryTickAsync(
                    gameClient,
                    snapshot,
                    settings,
                    meleeState,
                    deathRecoveryState,
                    runMetrics,
                    botLogPath,
                    ct),
                false,
                cts.Token);
            await DetectAndLogAnomaliesAsync(snapshot, settings, meleeState, deathRecoveryState, runMetrics, anomalyState, botLogPath);
            if (deathHandled)
            {
                runMetrics.RecordTick(DateTimeOffset.UtcNow, false);
                await Task.Delay(settings.Behavior.TickMs, cts.Token);
                continue;
            }

            if (settings.Behavior.FollowNearestPlayer)
            {
                followHoldZone = await RunPhaseWithWatchdogAsync(
                    "FOLLOW",
                    settings,
                    botLogPath,
                    ct => FollowNearestPlayerAsync(gameClient, pathfinder, snapshot, settings, botLogPath, followHoldZone, ct),
                    followHoldZone,
                    cts.Token);
                await WriteFollowGroundCsvAsync(pathfinder, snapshot, settings, followGroundCsvPath);
                await DetectAndLogAnomaliesAsync(snapshot, settings, meleeState, deathRecoveryState, runMetrics, anomalyState, botLogPath);
                await TrackBotActivityAsync(snapshot, settings, meleeState, deathRecoveryState, explorationState, activityState, botLogPath);
                await Task.Delay(settings.Behavior.TickMs, cts.Token);
                continue;
            }

            var combatTickOk = await RunPhaseWithWatchdogAsync(
                "COMBAT_TICK",
                settings,
                botLogPath,
                async ct =>
                {
                    await RunMeleeCombatTickAsync(
                        gameClient,
                        targetSelector,
                        snapshot,
                        options,
                        settings,
                        factionTemplates,
                        meleeState,
                        runMetrics,
                        botLogPath,
                        ct);
                    return true;
                },
                false,
                cts.Token);
            if (combatTickOk)
            {
                var exploreTickOk = await RunPhaseWithWatchdogAsync(
                    "EXPLORE_TICK",
                    settings,
                    botLogPath,
                    async ct =>
                    {
                        await RunExplorationTickAsync(
                            gameClient,
                            pathfinder,
                            snapshot,
                            settings,
                            factionTemplates,
                            meleeState,
                            explorationState,
                        botLogPath,
                        ct);
                        return true;
                    },
                    false,
                    cts.Token);
                if (!exploreTickOk)
                {
                    _ = await gameClient.StopMovementAsync(cts.Token);
                    if (explorationState.LastDestination is NavigationPoint blocked)
                    {
                        var blockedCell = ToGridCell(blocked.X, blocked.Y, settings.Exploration.GridCellSize);
                        explorationState.BlacklistedCells[blockedCell] =
                            DateTimeOffset.UtcNow.AddSeconds(Math.Max(2, settings.Exploration.BlacklistSeconds));
                    }

                    explorationState.IsActive = false;
                    explorationState.LastDestination = null;
                    explorationState.NoProgressSinceUtc = null;
                    explorationState.LastObservedPosition = null;
                    await WriteLogAsync(botLogPath, "EXPLORE_WATCHDOG_ABORT timeout_or_blocked");
                }
            }
            await DetectAndLogAnomaliesAsync(snapshot, settings, meleeState, deathRecoveryState, runMetrics, anomalyState, botLogPath);
            await TrackBotActivityAsync(snapshot, settings, meleeState, deathRecoveryState, explorationState, activityState, botLogPath);
        }
        else
        {
            await DetectAndLogAnomaliesAsync(snapshot, settings, meleeState, deathRecoveryState, runMetrics, anomalyState, botLogPath);
            await TrackBotActivityAsync(snapshot, settings, meleeState, deathRecoveryState, explorationState, activityState, botLogPath);
        }

        runMetrics.RecordTick(DateTimeOffset.UtcNow, meleeState.AttackActive || meleeState.EngagedTargetGuid != 0);
        await Task.Delay(settings.Behavior.TickMs, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // normal stop
}
finally
{
    runMetrics.Finish(DateTimeOffset.UtcNow);
    try
    {
        _ = await gameClient.StopMeleeAttackAsync();
        _ = await gameClient.LogoutAsync(settings.Connection.LogoutTimeoutSeconds);
    }
    catch (OperationCanceledException)
    {
        await WriteLogAsync(botLogPath, "Logout canceled");
    }
    var summary = runMetrics.BuildSummary();
    Console.WriteLine(summary);
    await WriteLogAsync(botLogPath, summary);
    await File.AppendAllTextAsync(runSummaryJsonlPath, runMetrics.BuildSummaryJsonLine() + Environment.NewLine);
    await WriteLogAsync(botLogPath, "BOT STOPPED");
}

static async Task<bool> FullLoginAsync(IGameClient gameClient, BotSettings settings, string logPath)
{
    await WriteLogAsync(logPath, "Full login started");
    var authOk = await gameClient.LoginAsync(
        new AuthServerInfo(settings.Connection.Host, settings.Connection.Port),
        new AuthServerCredentials(settings.Connection.Login, settings.Connection.Password));
    if (!authOk)
    {
        await WriteLogAsync(logPath, "Auth failed");
        return false;
    }

    var realms = await gameClient.GetRealmsAsync();
    var realm = realms.FirstOrDefault(r => r.Name.Equals(settings.Connection.RealmName, StringComparison.OrdinalIgnoreCase))
                ?? realms.FirstOrDefault();
    if (realm is null)
    {
        await WriteLogAsync(logPath, "No realm");
        return false;
    }

    var realmForConnection = new RealmInfo(
        realm.Id,
        realm.Name,
        settings.Connection.WorldHost,
        settings.Connection.WorldPort);
    if (!await gameClient.ConnectRealmAsync(realmForConnection))
    {
        await WriteLogAsync(logPath, "Realm connect failed");
        return false;
    }

    var characters = await gameClient.GetCharactersAsync();
    var character = characters.FirstOrDefault(c => c.Name.Equals(settings.Connection.CharacterName, StringComparison.OrdinalIgnoreCase))
                    ?? characters.FirstOrDefault();
    if (character is null)
    {
        await WriteLogAsync(logPath, "No character");
        return false;
    }

    var worldOk = await gameClient.EnterWorldAsync(character);
    await WriteLogAsync(logPath, worldOk ? $"Enter world OK ({character.Name})" : "Enter world KO");
    return worldOk;
}

static async Task WriteLogAsync(string logPath, string message)
{
    var line = $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}";
    await File.AppendAllTextAsync(logPath, line);
}

static async Task<T> RunPhaseWithWatchdogAsync<T>(
    string phaseName,
    BotSettings settings,
    string logPath,
    Func<CancellationToken, Task<T>> phase,
    T timeoutFallback,
    CancellationToken cancellationToken)
{
    var timeoutMs = Math.Max(0, settings.Behavior.PhaseWatchdogTimeoutMs);
    var warnMs = Math.Max(100, settings.Behavior.PhaseWatchdogWarnMs);
    var blockedLogIntervalMs = Math.Max(250, settings.Behavior.PhaseWatchdogBlockedLogIntervalMs);
    var pollMs = Math.Max(100, Math.Min(500, blockedLogIntervalMs / 2));
    var nextBlockedLogAtMs = warnMs;
    var stopwatch = Stopwatch.StartNew();
    using var phaseCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    if (timeoutMs > 0)
    {
        phaseCts.CancelAfter(timeoutMs);
    }

    var phaseTask = phase(phaseCts.Token);
    while (true)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (phaseTask.IsCompleted)
        {
            var result = await phaseTask;
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            if (elapsedMs >= warnMs)
            {
                await WriteLogAsync(logPath, $"BOT_TICK_PHASE_SLOW phase={phaseName} elapsedMs={elapsedMs}");
            }

            return result;
        }

        await Task.Delay(pollMs, cancellationToken);
        var elapsed = stopwatch.ElapsedMilliseconds;
        if (elapsed >= nextBlockedLogAtMs)
        {
            await WriteLogAsync(logPath, $"BOT_TICK_PHASE_BLOCKED phase={phaseName} elapsedMs={elapsed}");
            nextBlockedLogAtMs += blockedLogIntervalMs;
        }

        if (timeoutMs > 0 && elapsed >= timeoutMs)
        {
            phaseCts.Cancel();
            // Hard timeout: do not await the underlying task, it may ignore cancellation and block the tick.
            _ = phaseTask.ContinueWith(
                t =>
                {
                    _ = t.Exception;
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

            await WriteLogAsync(logPath, $"BOT_TICK_PHASE_TIMEOUT phase={phaseName} timeoutMs={timeoutMs} elapsedMs={elapsed}");
            return timeoutFallback;
        }
    }
}

static async Task EnsureFollowGroundCsvHeaderAsync(string csvPath)
{
    if (File.Exists(csvPath))
    {
        return;
    }

    const string header =
        "timestamp_utc," +
        "target_guid," +
        "target_distance," +
        "target_raw_x,target_raw_y,target_raw_z," +
        "target_ground_x,target_ground_y,target_ground_z," +
        "target_raw_minus_ground_z," +
        "me_x,me_y,me_z," +
        "me_ground_x,me_ground_y,me_ground_z," +
        "me_raw_minus_ground_z" +
        "\n";
    await File.WriteAllTextAsync(csvPath, header);
}

static async Task WriteFollowGroundCsvAsync(
    IPathfinder pathfinder,
    WorldSnapshot snapshot,
    BotSettings settings,
    string csvPath)
{
    if (!settings.Behavior.FollowNearestPlayer || snapshot.Player is null)
    {
        return;
    }

    var me = snapshot.Player;
    var nearestPlayer = snapshot.NearbyUnits
        .Where(u => u.IsPlayer)
        .Where(u => u.X.HasValue && u.Y.HasValue && u.Z.HasValue)
        .Select(u => new
        {
            Unit = u,
            Distance = MathF.Sqrt(
                ((u.X!.Value - me.X) * (u.X.Value - me.X)) +
                ((u.Y!.Value - me.Y) * (u.Y.Value - me.Y)) +
                ((u.Z!.Value - me.Z) * (u.Z.Value - me.Z)))
        })
        .OrderBy(u => u.Distance)
        .FirstOrDefault();

    if (nearestPlayer is null)
    {
        return;
    }

    var targetRaw = new NavigationPoint(
        nearestPlayer.Unit.X!.Value,
        nearestPlayer.Unit.Y!.Value,
        nearestPlayer.Unit.Z!.Value);
    var targetGround = pathfinder.ProjectToSurface(settings.Navigation.FollowProjectionMapId, targetRaw);
    var meRaw = new NavigationPoint(me.X, me.Y, me.Z);
    var meGround = pathfinder.ProjectToSurface(settings.Navigation.FollowProjectionMapId, meRaw);

    var targetRawMinusGroundZ = targetRaw.Z - targetGround.Z;
    var meRawMinusGroundZ = meRaw.Z - meGround.Z;

    static string F(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
    static string D(float value) => value.ToString("F3", CultureInfo.InvariantCulture);

    var line =
        $"{DateTimeOffset.UtcNow:O}," +
        $"{nearestPlayer.Unit.Guid}," +
        $"{D(nearestPlayer.Distance)}," +
        $"{F(targetRaw.X)},{F(targetRaw.Y)},{F(targetRaw.Z)}," +
        $"{F(targetGround.X)},{F(targetGround.Y)},{F(targetGround.Z)}," +
        $"{F(targetRawMinusGroundZ)}," +
        $"{F(meRaw.X)},{F(meRaw.Y)},{F(meRaw.Z)}," +
        $"{F(meGround.X)},{F(meGround.Y)},{F(meGround.Z)}," +
        $"{F(meRawMinusGroundZ)}" +
        "\n";

    await File.AppendAllTextAsync(csvPath, line);
}

static async Task<bool> FollowNearestPlayerAsync(
    IGameClient gameClient,
    IPathfinder pathfinder,
    WorldSnapshot snapshot,
    BotSettings settings,
    string logPath,
    bool inHoldZone,
    CancellationToken cancellationToken)
{
    var me = snapshot.Player;
    if (me is null)
    {
        return inHoldZone;
    }

    var nearestPlayer = snapshot.NearbyUnits
        .Where(u => u.IsPlayer)
        .Where(u => u.X.HasValue && u.Y.HasValue && u.Z.HasValue)
        .Select(u => new
        {
            Unit = u,
            Distance = MathF.Sqrt(
                ((u.X!.Value - me.X) * (u.X.Value - me.X)) +
                ((u.Y!.Value - me.Y) * (u.Y.Value - me.Y)) +
                ((u.Z!.Value - me.Z) * (u.Z.Value - me.Z)))
        })
        .OrderBy(u => u.Distance)
        .FirstOrDefault();

    if (nearestPlayer is null)
    {
        var total = snapshot.NearbyUnits.Count;
        var typedPlayers = snapshot.NearbyUnits.Count(u => u.IsPlayer);
        var typedCreatures = snapshot.NearbyUnits.Count(u => u.IsCreature);
        var withPosition = snapshot.NearbyUnits.Count(u => u.X.HasValue && u.Y.HasValue && u.Z.HasValue);
        var withLevel = snapshot.NearbyUnits.Count(u => u.Level.HasValue);
        await WriteLogAsync(
            logPath,
            $"FOLLOW no player candidate total={total} typedPlayers={typedPlayers} typedCreatures={typedCreatures} withPosition={withPosition} withLevel={withLevel}");
        return inHoldZone;
    }

    var rawTarget = new NavigationPoint(
        nearestPlayer.Unit.X!.Value,
        nearestPlayer.Unit.Y!.Value,
        nearestPlayer.Unit.Z!.Value);
    var projectedGround = pathfinder.ProjectToSurface(settings.Navigation.FollowProjectionMapId, rawTarget);
    var rawMinusGround = rawTarget.Z - projectedGround.Z;
    var meMinusGround = me.Z - projectedGround.Z;
    var followLog =
        $"FOLLOW guid={nearestPlayer.Unit.Guid} dist={nearestPlayer.Distance:F3} " +
        $"targetRaw=({rawTarget.X:F3},{rawTarget.Y:F3},{rawTarget.Z:F3}) " +
        $"targetGround=({projectedGround.X:F3},{projectedGround.Y:F3},{projectedGround.Z:F3}) " +
        $"targetRawMinusGroundZ={rawMinusGround:F3} meMinusGroundZ={meMinusGround:F3}";
    Console.WriteLine(followLog);
    await WriteLogAsync(logPath, followLog);

    if (nearestPlayer.Distance <= settings.Behavior.FollowMinDistance)
    {
        if (!inHoldZone)
        {
            var stopped = await gameClient.StopMovementAsync(cancellationToken);
            await WriteLogAsync(
                logPath,
                stopped
                    ? $"FOLLOW stop dist={nearestPlayer.Distance:F3} threshold={settings.Behavior.FollowMinDistance:F3}"
                    : $"FOLLOW stop skipped dist={nearestPlayer.Distance:F3} threshold={settings.Behavior.FollowMinDistance:F3}");
        }

        return true;
    }

    if (inHoldZone && nearestPlayer.Distance < settings.Behavior.FollowResumeDistance)
    {
        // Keep idle while target is still within the hold band to avoid stop/start oscillation.
        return true;
    }

    if (inHoldZone)
    {
        await WriteLogAsync(logPath, $"FOLLOW resume dist={nearestPlayer.Distance:F3}");
    }

    var moveOk = await gameClient.MoveToAsync(
        projectedGround,
        settings.Behavior.ArrivalRadius,
        settings.Navigation.RepathIntervalMs,
        settings.Navigation.StuckTimeoutMs,
        cancellationToken);
    await WriteLogAsync(logPath, moveOk ? "FOLLOW move OK" : "FOLLOW move KO");
    return false;
}

static async Task<bool> RunDeathRecoveryTickAsync(
    IGameClient gameClient,
    WorldSnapshot snapshot,
    BotSettings settings,
    MeleeCombatState meleeState,
    DeathRecoveryState deathState,
    RunMetrics metrics,
    string logPath,
    CancellationToken cancellationToken)
{
    var player = snapshot.Player;
    if (player is null)
    {
        return false;
    }

    var now = DateTimeOffset.UtcNow;
    var isGhost = player.IsGhost;
    // Keep death/life transitions authoritative to server state flags.
    // HP fields can be delayed around resurrect and should not block recovery.
    var isDeadHint = player.IsDeadHint || isGhost;
    var isDeadCorpse = isDeadHint && !isGhost;
    var hasDeathPosition = deathState.DeathX.HasValue && deathState.DeathY.HasValue && deathState.DeathZ.HasValue;
    var corpseCandidates = snapshot.NearbyUnits
        .Where(u => u.IsCorpse)
        .Where(u => u.X.HasValue && u.Y.HasValue && u.Z.HasValue)
        .Select(u => new
        {
            Unit = u,
            DistanceToPlayer = Distance3D(player.X, player.Y, player.Z, u.X!.Value, u.Y!.Value, u.Z!.Value),
            DistanceToDeathPosition = hasDeathPosition
                ? Distance3D(
                    deathState.DeathX!.Value,
                    deathState.DeathY!.Value,
                    deathState.DeathZ!.Value,
                    u.X!.Value,
                    u.Y!.Value,
                    u.Z!.Value)
                : (float?)null
        })
        .ToArray();
    var nearestOwnedCorpse = corpseCandidates
        .Where(x => x.Unit.OwnerGuid == player.Guid)
        .OrderBy(x => x.DistanceToPlayer)
        .FirstOrDefault();
    var corpseNearDeathPositionMaxDistance = MathF.Max(30f, settings.Death.CorpseSearchRadius * 2f);
    var nearestFallbackCorpse = hasDeathPosition
        ? corpseCandidates
            .Where(x => x.DistanceToDeathPosition.HasValue && x.DistanceToDeathPosition.Value <= corpseNearDeathPositionMaxDistance)
            .OrderBy(x => x.DistanceToDeathPosition)
            .ThenBy(x => x.DistanceToPlayer)
            .FirstOrDefault()
        : corpseCandidates
            .OrderBy(x => x.DistanceToPlayer)
            .FirstOrDefault();
    var nearestCorpse = nearestOwnedCorpse ?? nearestFallbackCorpse;
    var corpseCandidateCount = corpseCandidates.Length;
    var nearestCorpseDistance = nearestCorpse?.DistanceToPlayer;
    var nearestCorpseGuid = nearestCorpse?.Unit.Guid ?? 0UL;
    var nearestCorpseOwnerGuid = nearestCorpse?.Unit.OwnerGuid ?? 0UL;
    var nearestCorpseOwned = nearestCorpse is not null && nearestCorpse.Unit.OwnerGuid == player.Guid;
    float? deathPositionDistance = hasDeathPosition
        ? Distance3D(player.X, player.Y, player.Z, deathState.DeathX!.Value, deathState.DeathY!.Value, deathState.DeathZ!.Value)
        : null;

    if (deathState.LastObservedHp != player.HealthPercent ||
        deathState.LastObservedIsGhost != isGhost ||
        deathState.LastObservedIsDeadHint != isDeadHint ||
        deathState.LastObservedNearestCorpseGuid != nearestCorpseGuid)
    {
        await WriteLogAsync(
            logPath,
            $"DEATH_STATE_CHANGE hp={player.HealthPercent} isDeadHint={isDeadHint} isGhost={isGhost} " +
            $"corpseGuid={nearestCorpseGuid} corpseOwner={nearestCorpseOwnerGuid} corpseOwned={nearestCorpseOwned} " +
            $"corpseCandidates={corpseCandidateCount} corpseNearDeathMax={corpseNearDeathPositionMaxDistance:F1} " +
            $"corpseDist={(nearestCorpseDistance?.ToString("F2", CultureInfo.InvariantCulture) ?? "n/a")} " +
            $"flowActive={deathState.IsDeadFlowActive} repopSent={deathState.RepopSent} " +
            $"repopAttempts={deathState.RepopAttemptsForThisDeath} reclaimAttempts={deathState.ReclaimAttemptsForThisDeath}");
        deathState.LastObservedHp = player.HealthPercent;
        deathState.LastObservedIsGhost = isGhost;
        deathState.LastObservedIsDeadHint = isDeadHint;
        deathState.LastObservedNearestCorpseGuid = nearestCorpseGuid;
    }

    if (!isDeadHint && !isGhost)
    {
        if (deathState.ReclaimAwaitingOutcome && deathState.LastReclaimSentAtUtc.HasValue)
        {
            var ackMs = (long)(now - deathState.LastReclaimSentAtUtc.Value).TotalMilliseconds;
            await WriteLogAsync(logPath, $"DEATH_RECLAIM_ACCEPTED ackMs={ackMs}");
            deathState.ReclaimAwaitingOutcome = false;
            deathState.ConsecutiveReclaimRejects = 0;
            deathState.LastReclaimSentAtUtc = null;
        }

        if (deathState.IsDeadFlowActive)
        {
            metrics.Resurrections++;
            await WriteLogAsync(
                logPath,
                $"DEATH_RECOVERED hp={player.HealthPercent} isDeadHint={isDeadHint} isGhost={isGhost}");
            ResetDeathFlowState(deathState);
        }

        return false;
    }

    if (!deathState.IsDeadFlowActive)
    {
        deathState.IsDeadFlowActive = true;
        deathState.DiedAtUtc = now;
        deathState.RepopSent = false;
        deathState.RepopSentAtUtc = null;
        deathState.LastRepopAttemptAtUtc = null;
        deathState.LastReclaimAttemptAtUtc = null;
        deathState.LastStatusLogAtUtc = now;
        deathState.AutoResWaitLogged = false;
        deathState.RepopAttemptsForThisDeath = 0;
        deathState.ReclaimAttemptsForThisDeath = 0;
        deathState.DeathX = player.X;
        deathState.DeathY = player.Y;
        deathState.DeathZ = player.Z;
        metrics.Deaths++;
        await ResetCombatEngagementAsync(
            gameClient,
            meleeState,
            now,
            player.HealthPercent,
            blacklistGuid: null,
            blacklistSeconds: 0,
            cancellationToken);
        await WriteLogAsync(logPath, $"DEATH_DETECTED hp={player.HealthPercent} isDeadHint={isDeadHint} isGhost={isGhost}");
        await WriteLogAsync(
            logPath,
            $"DEATH_POSITION_CAPTURED x={player.X.ToString("F3", CultureInfo.InvariantCulture)} " +
            $"y={player.Y.ToString("F3", CultureInfo.InvariantCulture)} z={player.Z.ToString("F3", CultureInfo.InvariantCulture)}");
    }

    if (!deathState.LastStatusLogAtUtc.HasValue ||
        now - deathState.LastStatusLogAtUtc.Value >= TimeSpan.FromSeconds(1))
    {
        var sinceDeathMs = deathState.DiedAtUtc.HasValue ? (long)(now - deathState.DiedAtUtc.Value).TotalMilliseconds : -1;
        var sinceRepopMs = deathState.RepopSentAtUtc.HasValue ? (long)(now - deathState.RepopSentAtUtc.Value).TotalMilliseconds : -1;
        var phase = isGhost ? "ghost" : "corpse";
        await WriteLogAsync(
            logPath,
            $"DEATH_STATUS phase={phase} hp={player.HealthPercent} isDeadHint={isDeadHint} isGhost={isGhost} repopSent={deathState.RepopSent} " +
            $"sinceDeathMs={sinceDeathMs} sinceRepopMs={sinceRepopMs} repopAttempts={deathState.RepopAttemptsForThisDeath} " +
            $"reclaimAttempts={deathState.ReclaimAttemptsForThisDeath} corpseGuid={nearestCorpseGuid} " +
            $"corpseDist={(nearestCorpseDistance?.ToString("F2", CultureInfo.InvariantCulture) ?? "n/a")} " +
            $"deathPosDist={(deathPositionDistance?.ToString("F2", CultureInfo.InvariantCulture) ?? "n/a")}");
        deathState.LastStatusLogAtUtc = now;
    }

    if (isDeadCorpse)
    {
        var releaseDelay = TimeSpan.FromMilliseconds(Math.Max(0, settings.Death.ReleaseDelayMs));
        var repopRetry = TimeSpan.FromMilliseconds(Math.Max(250, settings.Death.DeadCorpseRepopRetryIntervalMs));
        var initialReleaseReady = !deathState.RepopSent &&
                                  deathState.DiedAtUtc.HasValue &&
                                  now - deathState.DiedAtUtc.Value >= releaseDelay;
        var retryReleaseReady = deathState.RepopSent &&
                                deathState.LastRepopAttemptAtUtc.HasValue &&
                                now - deathState.LastRepopAttemptAtUtc.Value >= repopRetry;

        if (initialReleaseReady || retryReleaseReady)
        {
            var repopOk = await gameClient.RequestRepopAsync(cancellationToken);
            deathState.LastRepopAttemptAtUtc = now;
            deathState.RepopAttemptsForThisDeath++;
            if (repopOk)
            {
                deathState.RepopSent = true;
                deathState.RepopSentAtUtc ??= now;
                deathState.AutoResWaitLogged = false;
                metrics.Releases++;
                await WriteLogAsync(
                    logPath,
                    $"DEATH_REPOP_SENT attempt={deathState.RepopAttemptsForThisDeath} hp={player.HealthPercent} isGhost={isGhost}");
            }
            else
            {
                await WriteLogAsync(logPath, $"DEATH_REPOP_FAILED attempt={deathState.RepopAttemptsForThisDeath}");
            }
        }

        return true;
    }

    if (isGhost)
    {
        if (!deathState.RepopSent)
        {
            deathState.RepopSent = true;
            deathState.RepopSentAtUtc ??= now;
            await WriteLogAsync(logPath, "DEATH_GHOST_WITHOUT_REPOP_REQUEST server_side_release_detected=true");
        }

        var autoResWaitActive = deathState.RepopSentAtUtc.HasValue &&
            now - deathState.RepopSentAtUtc.Value < TimeSpan.FromMilliseconds(Math.Max(0, settings.Death.AutoResWaitAfterRepopMs));
        if (autoResWaitActive)
        {
            if (!deathState.AutoResWaitLogged)
            {
                await WriteLogAsync(
                    logPath,
                    $"DEATH_WAIT_AUTO_RES ms={settings.Death.AutoResWaitAfterRepopMs} hp={player.HealthPercent} isGhost={isGhost}");
                deathState.AutoResWaitLogged = true;
            }
        }

        var corpseInRange = nearestCorpse is not null &&
                            nearestCorpse.DistanceToPlayer <= Math.Max(1f, settings.Death.CorpseSearchRadius);
        if (!corpseInRange)
        {
            var moveInterval = TimeSpan.FromMilliseconds(Math.Max(250, settings.Death.GhostCorpseMoveCommandIntervalMs));
            if (nearestCorpse is not null)
            {
                if (!deathState.LastGhostMoveAttemptAtUtc.HasValue ||
                    now - deathState.LastGhostMoveAttemptAtUtc.Value >= moveInterval)
                {
                    deathState.LastGhostMoveAttemptAtUtc = now;
                    deathState.GhostMoveAttemptsForThisDeath++;
                    var corpsePoint = new NavigationPoint(
                        nearestCorpse.Unit.X!.Value,
                        nearestCorpse.Unit.Y!.Value,
                        nearestCorpse.Unit.Z!.Value);
                    await WriteLogAsync(
                        logPath,
                        $"DEATH_GHOST_MOVE_START attempt={deathState.GhostMoveAttemptsForThisDeath} " +
                        $"corpseGuid={nearestCorpse.Unit.Guid} corpseOwner={nearestCorpse.Unit.OwnerGuid} corpseOwned={nearestCorpseOwned} corpseDist={nearestCorpse.DistanceToPlayer:F2}");
                    var moveOk = await gameClient.MoveToAsync(
                        corpsePoint,
                        Math.Max(0.5f, settings.Death.GhostCorpseApproachArrivalRadius),
                        settings.Navigation.RepathIntervalMs,
                        Math.Max(500, settings.Death.GhostCorpseMoveStuckTimeoutMs),
                        cancellationToken);
                    await WriteLogAsync(
                        logPath,
                        moveOk
                            ? $"DEATH_GHOST_MOVE_OK attempt={deathState.GhostMoveAttemptsForThisDeath} corpseGuid={nearestCorpse.Unit.Guid}"
                            : $"DEATH_GHOST_MOVE_KO attempt={deathState.GhostMoveAttemptsForThisDeath} corpseGuid={nearestCorpse.Unit.Guid}");
                }
            }
            else if (hasDeathPosition && deathPositionDistance.HasValue && deathPositionDistance.Value > Math.Max(1f, settings.Death.CorpseSearchRadius))
            {
                if (!deathState.LastGhostMoveAttemptAtUtc.HasValue ||
                    now - deathState.LastGhostMoveAttemptAtUtc.Value >= moveInterval)
                {
                    deathState.LastGhostMoveAttemptAtUtc = now;
                    deathState.GhostMoveAttemptsForThisDeath++;
                    var deathPoint = new NavigationPoint(
                        deathState.DeathX!.Value,
                        deathState.DeathY!.Value,
                        deathState.DeathZ!.Value);
                    await WriteLogAsync(
                        logPath,
                        $"DEATH_GHOST_MOVE_START attempt={deathState.GhostMoveAttemptsForThisDeath} " +
                        $"target=death_position deathPosDist={deathPositionDistance.Value.ToString("F2", CultureInfo.InvariantCulture)}");
                    var moveOk = await gameClient.MoveToAsync(
                        deathPoint,
                        Math.Max(0.5f, settings.Death.GhostCorpseApproachArrivalRadius),
                        settings.Navigation.RepathIntervalMs,
                        Math.Max(500, settings.Death.GhostCorpseMoveStuckTimeoutMs),
                        cancellationToken);
                    await WriteLogAsync(
                        logPath,
                        moveOk
                            ? $"DEATH_GHOST_MOVE_OK attempt={deathState.GhostMoveAttemptsForThisDeath} target=death_position"
                            : $"DEATH_GHOST_MOVE_KO attempt={deathState.GhostMoveAttemptsForThisDeath} target=death_position");
                }
            }

            await WriteLogAsync(
                logPath,
                $"DEATH_RECLAIM_WAIT reason=no_corpse_in_range searchRadius={settings.Death.CorpseSearchRadius:F1} " +
                $"corpseGuid={nearestCorpseGuid} corpseOwner={nearestCorpseOwnerGuid} corpseOwned={nearestCorpseOwned} " +
                $"corpseCandidates={corpseCandidateCount} corpseNearDeathMax={corpseNearDeathPositionMaxDistance:F1} " +
                $"corpseDist={(nearestCorpseDistance?.ToString("F2", CultureInfo.InvariantCulture) ?? "n/a")} " +
                $"deathPosDist={(deathPositionDistance?.ToString("F2", CultureInfo.InvariantCulture) ?? "n/a")}");
            return true;
        }

        var reclaimInterval = TimeSpan.FromMilliseconds(Math.Max(250, settings.Death.ReclaimAttemptIntervalMs));
        if (deathState.ReclaimAwaitingOutcome && deathState.LastReclaimSentAtUtc.HasValue)
        {
            var rejectDecision = TimeSpan.FromMilliseconds(Math.Max(500, settings.Death.ReclaimRejectDecisionMs));
            if (now - deathState.LastReclaimSentAtUtc.Value >= rejectDecision)
            {
                deathState.ReclaimAwaitingOutcome = false;
                deathState.ConsecutiveReclaimRejects++;
                var waitedMs = (long)(now - deathState.LastReclaimSentAtUtc.Value).TotalMilliseconds;
                await WriteLogAsync(
                    logPath,
                    $"DEATH_RECLAIM_REJECTED waitedMs={waitedMs} rejects={deathState.ConsecutiveReclaimRejects}");
            }
        }

        var backoffPow = MathF.Pow(
            Math.Max(1.0f, settings.Death.ReclaimBackoffMultiplier),
            Math.Max(0, deathState.ConsecutiveReclaimRejects));
        var adaptiveIntervalMs = (int)Math.Clamp(
            settings.Death.ReclaimAttemptIntervalMs * backoffPow,
            Math.Max(250, settings.Death.ReclaimAttemptIntervalMs),
            Math.Max(settings.Death.ReclaimAttemptIntervalMs, settings.Death.ReclaimMaxIntervalMs));
        reclaimInterval = TimeSpan.FromMilliseconds(adaptiveIntervalMs);
        if (autoResWaitActive)
        {
            await WriteLogAsync(
                logPath,
                $"DEATH_RECLAIM_WAIT reason=auto_res_wait ms={settings.Death.AutoResWaitAfterRepopMs} " +
                $"corpseGuid={nearestCorpseGuid} corpseDist={nearestCorpseDistance?.ToString("F2", CultureInfo.InvariantCulture) ?? "n/a"}");
            return true;
        }

        if (!deathState.LastReclaimAttemptAtUtc.HasValue ||
            now - deathState.LastReclaimAttemptAtUtc.Value >= reclaimInterval)
        {
            var reclaimOk = await gameClient.ReclaimCorpseAsync(corpseGuid: nearestCorpseGuid, cancellationToken);
            deathState.LastReclaimAttemptAtUtc = now;
            await WriteLogAsync(
                logPath,
                reclaimOk
                    ? $"DEATH_RECLAIM_SENT attempt={deathState.ReclaimAttemptsForThisDeath + 1} corpseGuid={nearestCorpseGuid} corpseOwner={nearestCorpseOwnerGuid} corpseOwned={nearestCorpseOwned} corpseDist={nearestCorpse!.DistanceToPlayer:F2} intervalMs={adaptiveIntervalMs}"
                    : $"DEATH_RECLAIM_FAILED corpseGuid={nearestCorpseGuid} intervalMs={adaptiveIntervalMs}");

            if (reclaimOk)
            {
                deathState.ReclaimAttemptsForThisDeath++;
                metrics.ReclaimAttempts++;
                deathState.LastReclaimSentAtUtc = now;
                deathState.ReclaimAwaitingOutcome = true;
            }
            else
            {
                deathState.ReclaimAwaitingOutcome = false;
                deathState.ConsecutiveReclaimRejects++;
            }
        }
        return true;
    }

    return true;
}

static void ResetDeathFlowState(DeathRecoveryState deathState)
{
    deathState.IsDeadFlowActive = false;
    deathState.RepopSent = false;
    deathState.DiedAtUtc = null;
    deathState.RepopSentAtUtc = null;
    deathState.LastRepopAttemptAtUtc = null;
    deathState.LastReclaimAttemptAtUtc = null;
    deathState.LastStatusLogAtUtc = null;
    deathState.AutoResWaitLogged = false;
    deathState.RepopAttemptsForThisDeath = 0;
    deathState.ReclaimAttemptsForThisDeath = 0;
    deathState.LastGhostMoveAttemptAtUtc = null;
    deathState.GhostMoveAttemptsForThisDeath = 0;
    deathState.LastObservedNearestCorpseGuid = 0;
    deathState.DeathX = null;
    deathState.DeathY = null;
    deathState.DeathZ = null;
    deathState.ReclaimAwaitingOutcome = false;
    deathState.LastReclaimSentAtUtc = null;
    deathState.ConsecutiveReclaimRejects = 0;
}

static async Task RunMeleeCombatTickAsync(
    IGameClient gameClient,
    ITargetSelector targetSelector,
    WorldSnapshot snapshot,
    TargetSelectionOptions options,
    BotSettings settings,
    FactionTemplateStore factionTemplates,
    MeleeCombatState state,
    RunMetrics metrics,
    string logPath,
    CancellationToken cancellationToken)
{
    var now = DateTimeOffset.UtcNow;
    PurgeBlacklist(state, now);
    PurgeApproachFailures(state, now, settings);
    PurgeRecentKillAttributions(state, now);

    var player = snapshot.Player;
    if (player is null)
    {
        return;
    }

    UpdateTargetMotionSamples(snapshot, state, now);

    var hostileWithPosition = snapshot.NearbyUnits
        .Where(u => u.X.HasValue && u.Y.HasValue && u.Z.HasValue)
        .Where(u => IsTargetAttackable(u, player, factionTemplates, settings, state, now))
        .Where(u => DistanceSquared(player, u) <= settings.Combat.AggroRadius * settings.Combat.AggroRadius)
        .ToArray();
    if (hostileWithPosition.Length > 0 &&
        !state.AttackActive &&
        state.EngagedTargetGuid == 0 &&
        now - state.LastActionAtUtc >= TimeSpan.FromSeconds(settings.Combat.IdleRecoverySeconds))
    {
        state.BlacklistedTargets.Clear();
        state.EngagedTargetGuid = 0;
        state.EngagedSinceUtc = null;
        state.EngagedMissingSinceUtc = null;
        state.AttackActive = false;
        state.AttackingGuid = 0;
        state.LastActionAtUtc = now;
        await WriteLogAsync(logPath, $"COMBAT_RECOVER hostile={hostileWithPosition.Length} reason=idle_timeout");
    }

    NearbyUnitSnapshot? selectedTarget = null;
    NearbyUnitSnapshot? attackingTarget = null;
    if (state.AttackingGuid != 0)
    {
        attackingTarget = snapshot.NearbyUnits.FirstOrDefault(u => u.Guid == state.AttackingGuid);
    }

    if (state.AttackActive && state.AttackingGuid != 0)
    {
        if (attackingTarget is null)
        {
            if (state.EngagedHadProgress &&
                now - state.LastCombatProgressAtUtc <= TimeSpan.FromSeconds(6) &&
                state.LastSwingStartAtUtc.HasValue &&
                now - state.LastSwingStartAtUtc.Value <= TimeSpan.FromSeconds(8))
            {
                await RegisterKillAsync(metrics, state, state.AttackingGuid, now, logPath, "target_missing_after_engagement");
            }

            await ResetCombatEngagementAsync(
                gameClient,
                state,
                now,
                player.HealthPercent,
                blacklistGuid: null,
                blacklistSeconds: 0,
                cancellationToken);
            await WriteLogAsync(logPath, $"COMBAT_DISENGAGE reason=attack_target_missing guid={state.AttackingGuid}");
            state.LastActionAtUtc = now;
            return;
        }

        if (!IsTargetAttackable(attackingTarget, player, factionTemplates, settings, state, now))
        {
            if (IsTargetDead(attackingTarget))
            {
                await RegisterKillAsync(metrics, state, attackingTarget.Guid, now, logPath, "dead_observed_attacking");
            }
            else
            {
                metrics.InvalidResetDeadHint++;
            }

            await ResetCombatEngagementAsync(
                gameClient,
                state,
                now,
                player.HealthPercent,
                attackingTarget.Guid,
                blacklistSeconds: settings.Combat.BlacklistSecondsAfterStuck,
                cancellationToken);
            await WriteLogAsync(
                logPath,
                $"COMBAT_DISENGAGE reason=attack_target_invalid guid={attackingTarget.Guid} dead={IsTargetDead(attackingTarget)}");
            state.LastActionAtUtc = now;
            return;
        }
    }

    if (state.EngagedTargetGuid != 0)
    {
        selectedTarget = snapshot.NearbyUnits.FirstOrDefault(u => u.Guid == state.EngagedTargetGuid);
    }
    if (selectedTarget is not null)
    {
        state.LastEngagedTargetSeenAtUtc = now;
    }

    if (selectedTarget is null && snapshot.Target is not null)
    {
        var targetedUnit = snapshot.NearbyUnits.FirstOrDefault(u => u.Guid == snapshot.Target.Guid);
        if (targetedUnit is not null &&
            IsTargetAttackable(targetedUnit, player, factionTemplates, settings, state, now))
        {
            selectedTarget = targetedUnit;
        }
    }

    if (selectedTarget is not null && !IsTargetAttackable(selectedTarget, player, factionTemplates, settings, state, now))
    {
        if (IsTargetDead(selectedTarget))
        {
            await RegisterKillAsync(metrics, state, selectedTarget.Guid, now, logPath, "dead_observed_selected");
        }
        else
        {
            metrics.InvalidResetDeadHint++;
        }

        await ResetCombatEngagementAsync(
            gameClient,
            state,
            now,
            player.HealthPercent,
            selectedTarget.Guid,
            blacklistSeconds: settings.Combat.BlacklistSecondsAfterStuck,
            cancellationToken);
        await WriteLogAsync(
            logPath,
            $"COMBAT_DISENGAGE reason=selected_target_invalid guid={selectedTarget.Guid} dead={IsTargetDead(selectedTarget)}");
        state.LastActionAtUtc = now;
        selectedTarget = null;
    }

    if (selectedTarget is null)
    {
        if (state.EngagedTargetGuid != 0 && !state.EngagedMissingSinceUtc.HasValue)
        {
            state.EngagedMissingSinceUtc = now;
        }

        if (state.EngagedTargetGuid != 0 && state.EngagedMissingSinceUtc.HasValue)
        {
            var missingFor = now - state.EngagedMissingSinceUtc.Value;
            if (missingFor < TimeSpan.FromSeconds(settings.Combat.MissingTargetGraceSeconds))
            {
                return;
            }

            var lostGuid = state.EngagedTargetGuid;
            _ = await gameClient.StopMovementAsync(cancellationToken);
            await ResetCombatEngagementAsync(
                gameClient,
                state,
                now,
                player.HealthPercent,
                lostGuid,
                blacklistSeconds: settings.Combat.BlacklistSecondsAfterStuck,
                cancellationToken);
            state.LastActionAtUtc = now;
            metrics.TargetLostResets++;
            await WriteLogAsync(
                logPath,
                $"COMBAT_TARGET_LOST_RESET guid={lostGuid} missingMs={(long)missingFor.TotalMilliseconds}");
        }

        selectedTarget = SelectCombatTarget(snapshot, player, factionTemplates, settings, state, now, targetSelector, options);
        if (selectedTarget is null)
        {
            if (state.AttackActive)
            {
                _ = await gameClient.StopMeleeAttackAsync(cancellationToken);
                state.AttackActive = false;
                state.AttackingGuid = 0;
                await WriteLogAsync(logPath, "COMBAT_IDLE no_valid_target attack_stopped");
                state.LastActionAtUtc = now;
            }

            return;
        }

        if (state.EngagedTargetGuid != selectedTarget.Guid)
        {
            state.EngagedTargetGuid = selectedTarget.Guid;
            state.EngagedSinceUtc = now;
            state.EngagedMissingSinceUtc = null;
            state.LastCombatProgressAtUtc = now;
            state.EngagedHadProgress = false;
        }

        if (snapshot.Target?.Guid != selectedTarget.Guid)
        {
            var selectOk = await gameClient.SelectTargetAsync(selectedTarget.Guid, cancellationToken);
            if (!selectOk)
            {
                await WriteLogAsync(logPath, $"COMBAT_SELECT_FAIL guid={selectedTarget.Guid}");
                return;
            }

            await WriteLogAsync(logPath, $"COMBAT_SELECT guid={selectedTarget.Guid} level={selectedTarget.Level?.ToString() ?? "?"}");
            state.LastActionAtUtc = now;
        }
    }

    if (!selectedTarget.X.HasValue || !selectedTarget.Y.HasValue || !selectedTarget.Z.HasValue)
    {
        await WriteLogAsync(logPath, $"COMBAT_SKIP guid={selectedTarget.Guid} reason=missing_position");
        return;
    }

    if (selectedTarget.HealthPercent.HasValue && selectedTarget.HealthPercent.Value <= 0)
    {
        await ResetCombatEngagementAsync(
            gameClient,
            state,
            now,
            player.HealthPercent,
            selectedTarget.Guid,
            blacklistSeconds: settings.Combat.BlacklistSecondsAfterStuck,
            cancellationToken);
        await RegisterKillAsync(metrics, state, selectedTarget.Guid, now, logPath, "hp_zero");
        state.LastTargetGuid = selectedTarget.Guid;
        state.LastTargetHpPercent = 0;
        state.LastActionAtUtc = now;
        return;
    }

    if (selectedTarget.IsDeadHint)
    {
        await ResetCombatEngagementAsync(
            gameClient,
            state,
            now,
            player.HealthPercent,
            selectedTarget.Guid,
            blacklistSeconds: settings.Combat.BlacklistSecondsAfterStuck,
            cancellationToken);
        await RegisterKillAsync(metrics, state, selectedTarget.Guid, now, logPath, "dead_hint");
        state.LastActionAtUtc = now;
        return;
    }

    if (state.EngagedTargetGuid == selectedTarget.Guid &&
        !selectedTarget.HealthPercent.HasValue &&
        selectedTarget.TargetGuid == 0 &&
        now - state.LastCombatProgressAtUtc >= TimeSpan.FromSeconds(settings.Combat.UnknownTargetHpNoDamageTimeoutSeconds))
    {
        await ResetCombatEngagementAsync(
            gameClient,
            state,
            now,
            player.HealthPercent,
            selectedTarget.Guid,
            blacklistSeconds: settings.Combat.BlacklistSecondsAfterStuck,
            cancellationToken);
        metrics.InvalidResetUnknownHp++;
        state.LastActionAtUtc = now;
        await WriteLogAsync(logPath, $"COMBAT_TARGET_INVALID_RESET guid={selectedTarget.Guid} reason=unknown_hp_no_target");
        return;
    }

    var targetDistance = Distance3D(
        player.X,
        player.Y,
        player.Z,
        selectedTarget.X.Value,
        selectedTarget.Y.Value,
        selectedTarget.Z.Value);
    var hardLeashDistance = settings.Combat.AggroRadius * settings.Combat.LeashResetDistanceMultiplier;
    if (state.EngagedTargetGuid == selectedTarget.Guid &&
        targetDistance >= hardLeashDistance &&
        (!state.EngagedSinceUtc.HasValue || now - state.EngagedSinceUtc.Value >= TimeSpan.FromSeconds(2)))
    {
        await ResetCombatEngagementAsync(
            gameClient,
            state,
            now,
            player.HealthPercent,
            selectedTarget.Guid,
            blacklistSeconds: settings.Combat.BlacklistSecondsAfterStuck,
            cancellationToken);
        metrics.InvalidResetLeash++;
        await WriteLogAsync(
            logPath,
            $"COMBAT_TARGET_INVALID_RESET guid={selectedTarget.Guid} reason=leash_distance dist={targetDistance:F3} hard={hardLeashDistance:F3}");
        return;
    }
    var headingToTarget = MathF.Atan2(selectedTarget.Y.Value - player.Y, selectedTarget.X.Value - player.X);
    var facingErrorRad = NormalizeAngleRadians(headingToTarget - player.Orientation);
    var facingErrorDeg = facingErrorRad * (180f / MathF.PI);
    var absFacingErrorDeg = MathF.Abs(facingErrorDeg);
    var facingLabel = absFacingErrorDeg <= 25f
        ? "front"
        : absFacingErrorDeg >= 140f
            ? "back"
            : facingErrorDeg > 0f
                ? "left"
                : "right";
    if (now - state.LastFacingTelemetryAtUtc >= TimeSpan.FromMilliseconds(500))
    {
        await WriteLogAsync(
            logPath,
            $"COMBAT_FACING guid={selectedTarget.Guid} dist={targetDistance:F3} playerO={player.Orientation:F3} targetO={headingToTarget:F3} errDeg={facingErrorDeg:F1} dir={facingLabel}");
        state.LastFacingTelemetryAtUtc = now;
    }

    var preferredMeleeDistance = MathF.Min(
        settings.Navigation.MeleeRange - 0.05f,
        MathF.Max(settings.Navigation.PreferredMeleeDistance, settings.Navigation.MinMeleeDistance));
    var preferredMeleeTolerance = MathF.Max(0.10f, settings.Navigation.PreferredMeleeTolerance);
    var needBackstepDistance = preferredMeleeDistance - preferredMeleeTolerance;
    if (targetDistance < needBackstepDistance &&
        now - state.LastRepositionAtUtc >= TimeSpan.FromMilliseconds(settings.Navigation.RepositionCooldownMs))
    {
        var dx = player.X - selectedTarget.X.Value;
        var dy = player.Y - selectedTarget.Y.Value;
        var length = MathF.Sqrt((dx * dx) + (dy * dy));
        if (length < 0.001f)
        {
            dx = 1.0f;
            dy = 0.0f;
            length = 1.0f;
        }

        var nx = dx / length;
        var ny = dy / length;
        var desiredDistance = preferredMeleeDistance;
        var repositionPoint = new NavigationPoint(
            selectedTarget.X.Value + (nx * desiredDistance),
            selectedTarget.Y.Value + (ny * desiredDistance),
            player.Z);
        var repositionOk = await RunMoveStepAsync(
            gameClient,
            repositionPoint,
            0.25f,
            settings,
            settings.Navigation.RepositionStepTimeoutMs,
            cancellationToken);
        state.LastRepositionAtUtc = now;
        state.LastActionAtUtc = now;
        if (repositionOk)
        {
            _ = await gameClient.FaceTowardsAsync(
                new NavigationPoint(selectedTarget.X.Value, selectedTarget.Y.Value, selectedTarget.Z.Value),
                cancellationToken);
        }
        await WriteLogAsync(
            logPath,
            repositionOk
                ? $"COMBAT_REPOSITION_OK guid={selectedTarget.Guid} dist={targetDistance:F3}"
                : $"COMBAT_REPOSITION_KO guid={selectedTarget.Guid} dist={targetDistance:F3}");
        return;
    }

    var chaseStartDistance = MathF.Min(
        settings.Navigation.MeleeRange + settings.Navigation.ChaseStartBuffer,
        preferredMeleeDistance + preferredMeleeTolerance);
    var chaseDestination = GetChaseDestination(selectedTarget, player, state, now, settings, out var predicted);
    if (predicted && now - state.LastPredictionLogAtUtc >= TimeSpan.FromMilliseconds(900))
    {
        await WriteLogAsync(
            logPath,
            $"COMBAT_TARGET_PREDICT guid={selectedTarget.Guid} raw=({selectedTarget.X.Value:F3},{selectedTarget.Y.Value:F3},{selectedTarget.Z.Value:F3}) pred=({chaseDestination.X:F3},{chaseDestination.Y:F3},{chaseDestination.Z:F3})");
        state.LastPredictionLogAtUtc = now;
    }

    if (targetDistance > chaseStartDistance)
    {
        if (now - state.LastChaseCommandAtUtc < TimeSpan.FromMilliseconds(settings.Navigation.ChaseCommandIntervalMs))
        {
            return;
        }

        var failureCountBeforeAttempt = GetApproachFailureCount(state, selectedTarget.Guid, now, settings);
        var adaptiveArrivalRadius = settings.Navigation.MeleeRange +
                                    MathF.Min(
                                        settings.Navigation.ChaseAdaptiveArrivalMaxBonus,
                                        failureCountBeforeAttempt * settings.Navigation.ChaseAdaptiveArrivalPerFailure);
        var moveOk = await RunChaseStepAsync(
            gameClient,
            selectedTarget,
            chaseDestination,
            adaptiveArrivalRadius,
            settings.Navigation.ChaseStepTimeoutMs,
            settings,
            cancellationToken);
        if (!moveOk && predicted)
        {
            var rawDestination = new NavigationPoint(selectedTarget.X.Value, selectedTarget.Y.Value, selectedTarget.Z.Value);
            var retryOk = await RunChaseStepAsync(
                gameClient,
                selectedTarget,
                rawDestination,
                settings.Navigation.MeleeRange + settings.Navigation.ChaseRetryArrivalBuffer,
                settings.Navigation.ChaseRetryTimeoutMs,
                settings,
                cancellationToken);
            await WriteLogAsync(
                logPath,
                retryOk
                    ? $"COMBAT_APPROACH_RETRY_OK guid={selectedTarget.Guid} dist={targetDistance:F3}"
                    : $"COMBAT_APPROACH_RETRY_KO guid={selectedTarget.Guid} dist={targetDistance:F3}");
            if (retryOk)
            {
                metrics.ApproachRetryOk++;
            }
            else
            {
                metrics.ApproachRetryKo++;
            }
            moveOk = retryOk;
        }
        state.LastChaseCommandAtUtc = now;
        state.LastChaseAtUtc = now;
        state.LastActionAtUtc = now;
        RegisterApproachAttempt(state, selectedTarget.Guid, moveOk, now, settings);
        await WriteLogAsync(
            logPath,
            moveOk
                ? $"COMBAT_APPROACH_OK guid={selectedTarget.Guid} dist={targetDistance:F3}"
                : $"COMBAT_APPROACH_KO guid={selectedTarget.Guid} dist={targetDistance:F3}");
        if (moveOk)
        {
            metrics.ApproachOk++;
        }
        else
        {
            metrics.ApproachKo++;
        }
        if (!moveOk &&
            TryGetApproachFailureCount(state, selectedTarget.Guid, now, settings, out var failureCount) &&
            failureCount >= settings.Combat.MaxApproachFailures)
        {
            await ResetCombatEngagementAsync(
                gameClient,
                state,
                now,
                player.HealthPercent,
                selectedTarget.Guid,
                blacklistSeconds: settings.Combat.BlacklistSecondsAfterStuck,
                cancellationToken);
            metrics.InvalidResetApproachFailures++;
            await WriteLogAsync(
                logPath,
                $"COMBAT_TARGET_INVALID_RESET guid={selectedTarget.Guid} reason=approach_failures count={failureCount}");
        }
    }
    else if (!state.AttackActive || state.AttackingGuid != selectedTarget.Guid)
    {
        var swingTarget = snapshot.NearbyUnits.FirstOrDefault(u => u.Guid == selectedTarget.Guid) ?? selectedTarget;
        if (!IsTargetAttackable(swingTarget, player, factionTemplates, settings, state, now) ||
            !swingTarget.X.HasValue || !swingTarget.Y.HasValue || !swingTarget.Z.HasValue)
        {
            await WriteLogAsync(logPath, $"COMBAT_SWING_ABORT guid={selectedTarget.Guid} reason=final_validation");
            return;
        }

        var swingDistance = Distance3D(
            player.X,
            player.Y,
            player.Z,
            swingTarget.X.Value,
            swingTarget.Y.Value,
            swingTarget.Z.Value);
        if (swingDistance > settings.Navigation.MeleeRange + 0.75f)
        {
            await WriteLogAsync(
                logPath,
                $"COMBAT_SWING_ABORT guid={selectedTarget.Guid} reason=distance dist={swingDistance:F3} limit={(settings.Navigation.MeleeRange + 0.75f):F3}");
            return;
        }

        var attackOk = await gameClient.StartMeleeAttackAsync(selectedTarget.Guid, cancellationToken);
        if (attackOk)
        {
            state.AttackActive = true;
            state.AttackingGuid = selectedTarget.Guid;
            state.LastSwingStartAtUtc = now;
            state.LastActionAtUtc = now;
            metrics.SwingStartOk++;
            await WriteLogAsync(logPath, $"COMBAT_SWING_START guid={selectedTarget.Guid} dist={targetDistance:F3}");
        }
        else
        {
            metrics.SwingStartFail++;
            await WriteLogAsync(logPath, $"COMBAT_SWING_START_FAIL guid={selectedTarget.Guid} dist={targetDistance:F3}");
        }
    }
    else if (targetDistance > preferredMeleeDistance + preferredMeleeTolerance &&
             now - state.LastChaseAtUtc >= TimeSpan.FromSeconds(settings.Navigation.CloseRangeNudgeSeconds))
    {
        var nudgeOk = await RunChaseStepAsync(
            gameClient,
            selectedTarget,
            chaseDestination,
            settings.Navigation.MeleeRange,
            settings.Navigation.ChaseStepTimeoutMs,
            settings,
            cancellationToken);
        if (nudgeOk)
        {
            state.LastChaseAtUtc = now;
            state.LastActionAtUtc = now;
            await WriteLogAsync(logPath, $"COMBAT_CLOSE_NUDGE guid={selectedTarget.Guid} dist={targetDistance:F3}");
        }
    }
    else if (state.AttackActive &&
             targetDistance >= settings.Navigation.MinMeleeDistance &&
             now - state.LastFaceNudgeAtUtc >= TimeSpan.FromMilliseconds(settings.Navigation.FaceNudgeCooldownMs) &&
             now - state.LastCombatProgressAtUtc >= TimeSpan.FromMilliseconds(settings.Navigation.FaceNudgeNoProgressMs) &&
             (absFacingErrorDeg >= settings.Navigation.FaceRefreshBackAngleDeg ||
              targetDistance <= settings.Navigation.MeleeRange + 0.25f))
    {
        if (snapshot.Target?.Guid != selectedTarget.Guid)
        {
            _ = await gameClient.SelectTargetAsync(selectedTarget.Guid, cancellationToken);
        }

        _ = await gameClient.FaceTowardsAsync(
            new NavigationPoint(selectedTarget.X.Value, selectedTarget.Y.Value, selectedTarget.Z.Value),
            cancellationToken);
        var faceNudgeOk = await gameClient.StartMeleeAttackAsync(selectedTarget.Guid, cancellationToken);
        state.LastFaceNudgeAtUtc = now;
        if (faceNudgeOk)
        {
            state.LastActionAtUtc = now;
            await WriteLogAsync(logPath, $"COMBAT_FACE_REFRESH guid={selectedTarget.Guid} dist={targetDistance:F3} errDeg={facingErrorDeg:F1} dir={facingLabel}");
        }
    }

    if (state.LastTargetGuid != selectedTarget.Guid)
    {
        state.LastTargetGuid = selectedTarget.Guid;
        state.LastTargetHpPercent = selectedTarget.HealthPercent;
        state.LastTargetHpChangeAtUtc = now;
        state.LastPlayerHpPercent = player.HealthPercent;
        state.LastCombatProgressAtUtc = now;
    }
    else
    {
        var targetHpChanged = selectedTarget.HealthPercent.HasValue && selectedTarget.HealthPercent != state.LastTargetHpPercent;
        var tookDamage = player.HealthPercent < state.LastPlayerHpPercent;
        if (targetHpChanged || tookDamage)
        {
            if (targetHpChanged)
            {
                if (state.LastTargetHpPercent.HasValue &&
                    selectedTarget.HealthPercent.HasValue &&
                    selectedTarget.HealthPercent.Value > state.LastTargetHpPercent.Value)
                {
                    var hpJump = selectedTarget.HealthPercent.Value - state.LastTargetHpPercent.Value;
                    if (state.EngagedHadProgress && hpJump >= settings.Combat.TargetResetHpJumpPercent)
                    {
                        await ResetCombatEngagementAsync(
                            gameClient,
                            state,
                            now,
                            player.HealthPercent,
                            selectedTarget.Guid,
                            blacklistSeconds: settings.Combat.BlacklistSecondsAfterStuck,
                            cancellationToken);
                        metrics.InvalidResetHpReset++;
                        await WriteLogAsync(
                            logPath,
                            $"COMBAT_TARGET_INVALID_RESET guid={selectedTarget.Guid} reason=hp_reset jump={hpJump}");
                        return;
                    }
                }

                state.LastTargetHpPercent = selectedTarget.HealthPercent;
                state.LastTargetHpChangeAtUtc = now;
            }

            state.LastPlayerHpPercent = player.HealthPercent;
            state.LastCombatProgressAtUtc = now;
            state.EngagedHadProgress = true;
            await WriteLogAsync(
                logPath,
                $"COMBAT_PROGRESS guid={selectedTarget.Guid} targetHp={(selectedTarget.HealthPercent?.ToString() ?? "n/a")} playerHp={player.HealthPercent}");
            state.LastActionAtUtc = now;
        }
        else
        {
            state.LastPlayerHpPercent = player.HealthPercent;
            var effectiveTimeoutSeconds = state.EngagedHadProgress
                ? settings.Combat.FinisherNoProgressTimeoutSeconds
                : (selectedTarget.HealthPercent.HasValue
                    ? settings.Combat.NoHpChangeTimeoutSeconds
                    : settings.Combat.UnknownTargetHpNoDamageTimeoutSeconds);
            if (state.AttackActive &&
                state.LastSwingStartAtUtc.HasValue &&
                now - state.LastSwingStartAtUtc.Value >= TimeSpan.FromSeconds(settings.Combat.StuckCheckGraceSeconds) &&
                now - state.LastCombatProgressAtUtc >= TimeSpan.FromSeconds(effectiveTimeoutSeconds) &&
                 targetDistance <= settings.Navigation.StuckCheckMaxDistance &&
                (!state.EngagedSinceUtc.HasValue || now - state.EngagedSinceUtc.Value >= TimeSpan.FromSeconds(settings.Combat.MinEngagementSeconds)))
            {
                await WriteLogAsync(
                    logPath,
                    $"COMBAT_STUCK_SWITCH guid={selectedTarget.Guid} timeout={effectiveTimeoutSeconds}s");
                metrics.StuckSwitches++;

                await ResetCombatEngagementAsync(
                    gameClient,
                    state,
                    now,
                    player.HealthPercent,
                    selectedTarget.Guid,
                    blacklistSeconds: settings.Combat.BlacklistSecondsAfterStuck,
                    cancellationToken);
                state.LastActionAtUtc = now;
            }
        }
    }
}

static async Task RunExplorationTickAsync(
    IGameClient gameClient,
    IPathfinder pathfinder,
    WorldSnapshot snapshot,
    BotSettings settings,
    FactionTemplateStore factionTemplates,
    MeleeCombatState meleeState,
    ExplorationState explorationState,
    string logPath,
    CancellationToken cancellationToken)
{
    if (!settings.Exploration.Enabled)
    {
        return;
    }

    var player = snapshot.Player;
    if (player is null)
    {
        return;
    }

    var now = DateTimeOffset.UtcNow;
    MarkVisited(explorationState, player.X, player.Y, settings.Exploration.GridCellSize, now);
    PurgeExploreMemory(explorationState, settings.Exploration, now);
    if (explorationState.IsActive)
    {
        if (explorationState.LastObservedPosition is ExplorationPositionSample lastPos)
        {
            var progress = Distance3D(player.X, player.Y, player.Z, lastPos.X, lastPos.Y, lastPos.Z);
            if (progress < settings.Exploration.MinProgressDistance)
            {
                explorationState.NoProgressSinceUtc ??= now;
                if (now - explorationState.NoProgressSinceUtc.Value >= TimeSpan.FromMilliseconds(settings.Exploration.NoProgressTimeoutMs))
                {
                    _ = await gameClient.StopMovementAsync(cancellationToken);
                    if (explorationState.LastDestination is NavigationPoint blocked)
                    {
                        var blockedCell = ToGridCell(blocked.X, blocked.Y, settings.Exploration.GridCellSize);
                        explorationState.BlacklistedCells[blockedCell] = now.AddSeconds(Math.Max(2, settings.Exploration.BlacklistSeconds));
                    }

                    explorationState.IsActive = false;
                    explorationState.LastDestination = null;
                    explorationState.NoProgressSinceUtc = null;
                    await WriteLogAsync(logPath, $"EXPLORE_ABORT_NO_PROGRESS timeoutMs={settings.Exploration.NoProgressTimeoutMs}");
                }
            }
            else
            {
                explorationState.NoProgressSinceUtc = null;
            }
        }

        explorationState.LastObservedPosition = new ExplorationPositionSample(player.X, player.Y, player.Z, now);
    }
    else
    {
        explorationState.LastObservedPosition = null;
        explorationState.NoProgressSinceUtc = null;
    }

    if (meleeState.AttackActive || meleeState.EngagedTargetGuid != 0)
    {
        if (explorationState.IsActive)
        {
            explorationState.IsActive = false;
            explorationState.LastDestination = null;
            await WriteLogAsync(logPath, "EXPLORE_STOP reason=combat_active");
        }

        return;
    }

    var hasAttackable = snapshot.NearbyUnits.Any(u => IsTargetAttackable(u, player, factionTemplates, settings, meleeState, now));
    if (hasAttackable)
    {
        hasAttackable = snapshot.NearbyUnits
            .Where(u => u.X.HasValue && u.Y.HasValue && u.Z.HasValue)
            .Where(u => IsTargetAttackable(u, player, factionTemplates, settings, meleeState, now))
            .Any(u => DistanceSquared(player, u) <= settings.Combat.AggroRadius * settings.Combat.AggroRadius);
    }
    if (hasAttackable)
    {
        if (explorationState.IsActive)
        {
            explorationState.IsActive = false;
            explorationState.LastDestination = null;
            await WriteLogAsync(logPath, "EXPLORE_STOP reason=target_available");
        }

        return;
    }

    if (now - meleeState.LastActionAtUtc < TimeSpan.FromMilliseconds(Math.Max(1000, settings.Exploration.IdleBeforeExploreMs)))
    {
        return;
    }

    if (explorationState.LastAttemptAtUtc.HasValue &&
        now - explorationState.LastAttemptAtUtc.Value < TimeSpan.FromMilliseconds(Math.Max(500, settings.Exploration.ExploreCommandIntervalMs)))
    {
        return;
    }

    if (!TryPickExploreDestination(pathfinder, player, settings, explorationState, now, out var destination, out var score))
    {
        await WriteLogAsync(logPath, "EXPLORE_NO_DESTINATION");
        explorationState.LastAttemptAtUtc = now;
        return;
    }

    explorationState.IsActive = true;
    explorationState.LastAttemptAtUtc = now;
    explorationState.LastDestination = destination;
    await WriteLogAsync(
        logPath,
        $"EXPLORE_MOVE_START to=({destination.X:F3},{destination.Y:F3},{destination.Z:F3}) score={score:F2}");

    var moveOk = await RunMoveStepAsync(
        gameClient,
        destination,
        settings.Exploration.ArrivalRadius,
        settings,
        settings.Exploration.StepTimeoutMs,
        cancellationToken);

    if (moveOk)
    {
        await WriteLogAsync(logPath, "EXPLORE_MOVE_OK");
        MarkVisited(explorationState, destination.X, destination.Y, settings.Exploration.GridCellSize, now);
        explorationState.LastObservedPosition = new ExplorationPositionSample(player.X, player.Y, player.Z, now);
        explorationState.NoProgressSinceUtc = null;
    }
    else
    {
        _ = await gameClient.StopMovementAsync(cancellationToken);
        await WriteLogAsync(logPath, "EXPLORE_MOVE_KO");
        var cell = ToGridCell(destination.X, destination.Y, settings.Exploration.GridCellSize);
        explorationState.BlacklistedCells[cell] = now.AddSeconds(Math.Max(2, settings.Exploration.BlacklistSeconds));
        explorationState.IsActive = false;
        explorationState.LastDestination = null;
        explorationState.NoProgressSinceUtc = null;
    }
}

static async Task TrackBotActivityAsync(
    WorldSnapshot snapshot,
    BotSettings settings,
    MeleeCombatState meleeState,
    DeathRecoveryState deathRecoveryState,
    ExplorationState explorationState,
    BotActivityState activityState,
    string logPath)
{
    var now = DateTimeOffset.UtcNow;
    var player = snapshot.Player;
    string mode;
    string detail;

    if (player is null)
    {
        mode = "NO_PLAYER";
        detail = "snapshot_player=null";
    }
    else
    {
        var isDeadFlow = deathRecoveryState.IsDeadFlowActive || player.IsDeadHint || player.IsGhost;
        if (isDeadFlow)
        {
            mode = "DEATH_RECOVERY";
            detail = $"ghost={player.IsGhost} deadHint={player.IsDeadHint}";
        }
        else if (meleeState.AttackActive)
        {
            mode = "COMBAT_ATTACK";
            detail = $"target={meleeState.AttackingGuid}";
        }
        else if (meleeState.EngagedTargetGuid != 0)
        {
            mode = "COMBAT_CHASE";
            detail = $"target={meleeState.EngagedTargetGuid}";
        }
        else if (explorationState.IsActive)
        {
            mode = "EXPLORE_MOVE";
            if (explorationState.LastDestination is NavigationPoint d)
            {
                detail = $"to=({d.X:F3},{d.Y:F3},{d.Z:F3})";
            }
            else
            {
                detail = "destination=unknown";
            }
        }
        else
        {
            mode = "IDLE";
            detail = "no_target_no_explore";
        }
    }

    var actionLabel = $"{mode} {detail}";
    if (!string.Equals(activityState.LastActionLabel, actionLabel, StringComparison.Ordinal))
    {
        activityState.LastActionLabel = actionLabel;
        activityState.LastActionAtUtc = now;
        activityState.LastNoActionLogAtUtc = now;
        await WriteLogAsync(logPath, $"BOT_ACTION mode={mode} detail={detail}");
        return;
    }

    var watchMode = mode is "IDLE" or "EXPLORE_MOVE" or "COMBAT_CHASE" or "NO_PLAYER";
    var noActionThreshold = TimeSpan.FromSeconds(Math.Max(3, settings.Behavior.NoActionLogSeconds));
    if (watchMode &&
        now - activityState.LastActionAtUtc >= noActionThreshold &&
        now - activityState.LastNoActionLogAtUtc >= TimeSpan.FromSeconds(2))
    {
        if (player is not null)
        {
            await WriteLogAsync(
                logPath,
                $"BOT_NO_ACTION idleSec={(now - activityState.LastActionAtUtc).TotalSeconds:F1} " +
                $"lastAction=\"{activityState.LastActionLabel}\" " +
                $"pos=({player.X:F3},{player.Y:F3},{player.Z:F3})");
        }
        else
        {
            await WriteLogAsync(
                logPath,
                $"BOT_NO_ACTION idleSec={(now - activityState.LastActionAtUtc).TotalSeconds:F1} " +
                $"lastAction=\"{activityState.LastActionLabel}\" pos=unknown");
        }

        activityState.LastNoActionLogAtUtc = now;
    }
}

static bool TryPickExploreDestination(
    IPathfinder pathfinder,
    PlayerSnapshot player,
    BotSettings settings,
    ExplorationState state,
    DateTimeOffset now,
    out NavigationPoint destination,
    out float score)
{
    destination = new NavigationPoint(player.X, player.Y, player.Z);
    score = float.MinValue;
    var found = false;
    var mapId = settings.Navigation.FollowProjectionMapId;
    var minRadius = MathF.Max(2.0f, settings.Exploration.StepMinDistance);
    var maxRadius = MathF.Max(minRadius + 1.0f, settings.Exploration.StepMaxDistance);
    var cellSize = MathF.Max(1.0f, settings.Exploration.GridCellSize);

    for (var i = 0; i < Math.Max(6, settings.Exploration.CandidateCount); i++)
    {
        var angle = (float)(Random.Shared.NextDouble() * Math.PI * 2.0);
        var radius = minRadius + ((float)Random.Shared.NextDouble() * (maxRadius - minRadius));
        var raw = new NavigationPoint(
            player.X + (MathF.Cos(angle) * radius),
            player.Y + (MathF.Sin(angle) * radius),
            player.Z);
        var projected = pathfinder.ProjectToSurface(mapId, raw);
        var dist = Distance3D(player.X, player.Y, player.Z, projected.X, projected.Y, projected.Z);
        if (dist < minRadius * 0.8f)
        {
            continue;
        }

        var startPoint = new NavigationPoint(player.X, player.Y, player.Z);
        var path = pathfinder.FindPath(mapId, startPoint, projected);
        if (path.Count < 2)
        {
            continue;
        }

        if (!pathfinder.HasLineOfSight(mapId, startPoint, projected))
        {
            var pathDistance = 0f;
            for (var p = 1; p < path.Count; p++)
            {
                pathDistance += Distance3D(path[p - 1].X, path[p - 1].Y, path[p - 1].Z, path[p].X, path[p].Y, path[p].Z);
            }

            if (pathDistance > (settings.Exploration.StepMaxDistance * 2.5f))
            {
                continue;
            }
        }

        var cell = ToGridCell(projected.X, projected.Y, cellSize);
        if (state.BlacklistedCells.TryGetValue(cell, out var blockedUntil) && blockedUntil > now)
        {
            continue;
        }

        var lastVisitedAt = state.VisitedCells.TryGetValue(cell, out var at) ? at : DateTimeOffset.MinValue;
        var unvisitedAgeSec = lastVisitedAt == DateTimeOffset.MinValue
            ? settings.Exploration.VisitDecaySeconds
            : (float)Math.Clamp((now - lastVisitedAt).TotalSeconds, 0.0, settings.Exploration.VisitDecaySeconds);
        var localScore = (unvisitedAgeSec * 2.0f) + dist;
        if (!found || localScore > score)
        {
            found = true;
            score = localScore;
            destination = projected;
        }
    }

    return found;
}

static void MarkVisited(ExplorationState state, float x, float y, float cellSize, DateTimeOffset now)
{
    var cell = ToGridCell(x, y, cellSize);
    state.VisitedCells[cell] = now;
}

static void PurgeExploreMemory(ExplorationState state, ExplorationSettings settings, DateTimeOffset now)
{
    if (state.VisitedCells.Count > 0)
    {
        var ttl = TimeSpan.FromSeconds(Math.Max(30, settings.VisitDecaySeconds * 4));
        var expiredVisits = state.VisitedCells
            .Where(x => now - x.Value > ttl)
            .Select(x => x.Key)
            .ToArray();
        foreach (var cell in expiredVisits)
        {
            state.VisitedCells.Remove(cell);
        }
    }

    if (state.BlacklistedCells.Count > 0)
    {
        var expiredBlacklist = state.BlacklistedCells
            .Where(x => x.Value <= now)
            .Select(x => x.Key)
            .ToArray();
        foreach (var cell in expiredBlacklist)
        {
            state.BlacklistedCells.Remove(cell);
        }
    }
}

static ExplorationGridCell ToGridCell(float x, float y, float cellSize)
{
    var size = MathF.Max(1.0f, cellSize);
    var gx = (int)MathF.Floor(x / size);
    var gy = (int)MathF.Floor(y / size);
    return new ExplorationGridCell(gx, gy);
}

static async Task ResetCombatEngagementAsync(
    IGameClient gameClient,
    MeleeCombatState state,
    DateTimeOffset now,
    int playerHealthPercent,
    ulong? blacklistGuid,
    int blacklistSeconds,
    CancellationToken cancellationToken)
{
    if (state.AttackActive)
    {
        _ = await gameClient.StopMeleeAttackAsync(cancellationToken);
    }

    state.AttackActive = false;
    state.AttackingGuid = 0;
    if (blacklistGuid.HasValue && blacklistGuid.Value != 0 && blacklistSeconds > 0)
    {
        state.BlacklistedTargets[blacklistGuid.Value] = now.AddSeconds(blacklistSeconds);
    }

    state.LastTargetGuid = 0;
    state.LastTargetHpPercent = null;
    state.LastTargetHpChangeAtUtc = now;
    state.LastPlayerHpPercent = playerHealthPercent;
    state.LastCombatProgressAtUtc = now;
    state.LastSwingStartAtUtc = null;
    state.EngagedTargetGuid = 0;
    state.EngagedSinceUtc = null;
    state.EngagedMissingSinceUtc = null;
    state.EngagedHadProgress = false;
}

static void RegisterApproachAttempt(
    MeleeCombatState state,
    ulong guid,
    bool success,
    DateTimeOffset now,
    BotSettings settings)
{
    if (guid == 0)
    {
        return;
    }

    if (success)
    {
        state.ApproachFailures.Remove(guid);
        return;
    }

    if (!state.ApproachFailures.TryGetValue(guid, out var entry))
    {
        state.ApproachFailures[guid] = new ApproachFailureState(1, now);
        return;
    }

    var window = TimeSpan.FromSeconds(settings.Combat.ApproachFailureWindowSeconds);
    if (now - entry.FirstFailureAtUtc > window)
    {
        state.ApproachFailures[guid] = new ApproachFailureState(1, now);
        return;
    }

    state.ApproachFailures[guid] = entry with { Count = entry.Count + 1 };
}

static bool IsTargetDead(NearbyUnitSnapshot target)
{
    return target.IsDeadHint || (target.HealthPercent.HasValue && target.HealthPercent.Value <= 0);
}

static async Task RegisterKillAsync(
    RunMetrics metrics,
    MeleeCombatState state,
    ulong guid,
    DateTimeOffset now,
    string logPath,
    string reason)
{
    if (guid == 0)
    {
        return;
    }

    if (state.RecentKillAttributions.TryGetValue(guid, out var seenUntil) && seenUntil > now)
    {
        return;
    }

    state.RecentKillAttributions[guid] = now.AddSeconds(20);
    metrics.Kills++;
    if (reason.StartsWith("dead_", StringComparison.Ordinal))
    {
        metrics.KillsObservedDead++;
    }
    else
    {
        metrics.KillsAttributed++;
    }

    await WriteLogAsync(logPath, $"COMBAT_KILL guid={guid} reason={reason}");
}

static bool TryGetApproachFailureCount(
    MeleeCombatState state,
    ulong guid,
    DateTimeOffset now,
    BotSettings settings,
    out int count)
{
    count = 0;
    if (!state.ApproachFailures.TryGetValue(guid, out var entry))
    {
        return false;
    }

    var window = TimeSpan.FromSeconds(settings.Combat.ApproachFailureWindowSeconds);
    if (now - entry.FirstFailureAtUtc > window)
    {
        state.ApproachFailures.Remove(guid);
        return false;
    }

    count = entry.Count;
    return true;
}

static int GetApproachFailureCount(
    MeleeCombatState state,
    ulong guid,
    DateTimeOffset now,
    BotSettings settings)
{
    return TryGetApproachFailureCount(state, guid, now, settings, out var count)
        ? count
        : 0;
}

static async Task<bool> RunChaseStepAsync(
    IGameClient gameClient,
    NearbyUnitSnapshot target,
    NavigationPoint chaseDestination,
    float arrivalRadius,
    int timeoutMs,
    BotSettings settings,
    CancellationToken cancellationToken)
{
    if (!target.X.HasValue || !target.Y.HasValue || !target.Z.HasValue)
    {
        return false;
    }

    return await RunMoveStepAsync(
        gameClient,
        chaseDestination,
        arrivalRadius,
        settings,
        timeoutMs,
        cancellationToken);
}

static void UpdateTargetMotionSamples(WorldSnapshot snapshot, MeleeCombatState state, DateTimeOffset now)
{
    foreach (var unit in snapshot.NearbyUnits)
    {
        if (!unit.X.HasValue || !unit.Y.HasValue || !unit.Z.HasValue)
        {
            continue;
        }

        var sample = new MotionSample(unit.X.Value, unit.Y.Value, unit.Z.Value, now);
        if (!state.TargetMotionSamples.TryGetValue(unit.Guid, out var pair))
        {
            state.TargetMotionSamples[unit.Guid] = new MotionSamplePair(sample, sample);
            continue;
        }

        if (MathF.Abs(pair.Latest.X - sample.X) < 0.001f &&
            MathF.Abs(pair.Latest.Y - sample.Y) < 0.001f &&
            MathF.Abs(pair.Latest.Z - sample.Z) < 0.001f)
        {
            state.TargetMotionSamples[unit.Guid] = pair with { Latest = sample };
            continue;
        }

        state.TargetMotionSamples[unit.Guid] = new MotionSamplePair(pair.Latest, sample);
    }
}

static NavigationPoint GetChaseDestination(
    NearbyUnitSnapshot target,
    PlayerSnapshot player,
    MeleeCombatState state,
    DateTimeOffset now,
    BotSettings settings,
    out bool predicted)
{
    predicted = false;
    var raw = new NavigationPoint(target.X!.Value, target.Y!.Value, target.Z!.Value);
    if (!state.TargetMotionSamples.TryGetValue(target.Guid, out var pair))
    {
        return raw;
    }

    var dtSeconds = (float)(pair.Latest.AtUtc - pair.Previous.AtUtc).TotalSeconds;
    if (dtSeconds < 0.05f)
    {
        return raw;
    }

    var vx = (pair.Latest.X - pair.Previous.X) / dtSeconds;
    var vy = (pair.Latest.Y - pair.Previous.Y) / dtSeconds;
    var vz = (pair.Latest.Z - pair.Previous.Z) / dtSeconds;
    var speed = MathF.Sqrt((vx * vx) + (vy * vy) + (vz * vz));
    if (speed < settings.Navigation.TargetPredictionMinSpeed ||
        speed > settings.Navigation.TargetPredictionMaxSpeed)
    {
        return raw;
    }

    var ageMs = (float)(now - pair.Latest.AtUtc).TotalMilliseconds;
    if (ageMs < 0 || ageMs > settings.Navigation.TargetPredictionMaxAgeMs)
    {
        return raw;
    }

    var ageLeadSeconds = ageMs / 1000f;
    var distanceToTarget = MathF.Sqrt(
        ((pair.Latest.X - player.X) * (pair.Latest.X - player.X)) +
        ((pair.Latest.Y - player.Y) * (pair.Latest.Y - player.Y)) +
        ((pair.Latest.Z - player.Z) * (pair.Latest.Z - player.Z)));
    var distanceLeadSeconds = distanceToTarget / MathF.Max(1.5f, settings.Navigation.MeleeRange * 2.0f);
    var leadSeconds = Math.Clamp(
        MathF.Max(MathF.Max(ageLeadSeconds, settings.Navigation.TargetPredictionMinLeadSeconds), distanceLeadSeconds * 0.10f),
        settings.Navigation.TargetPredictionMinLeadSeconds,
        settings.Navigation.TargetPredictionMaxLeadSeconds);

    var px = pair.Latest.X + (vx * leadSeconds);
    var py = pair.Latest.Y + (vy * leadSeconds);
    var pz = pair.Latest.Z + (vz * leadSeconds);
    var predictedDistance = MathF.Sqrt(
        ((px - pair.Latest.X) * (px - pair.Latest.X)) +
        ((py - pair.Latest.Y) * (py - pair.Latest.Y)) +
        ((pz - pair.Latest.Z) * (pz - pair.Latest.Z)));
    if (predictedDistance > settings.Navigation.TargetPredictionMaxDistance)
    {
        return raw;
    }

    predicted = true;
    return new NavigationPoint(px, py, pz);
}

static async Task<bool> RunMoveStepAsync(
    IGameClient gameClient,
    NavigationPoint destination,
    float arrivalRadius,
    BotSettings settings,
    int timeoutMs,
    CancellationToken cancellationToken)
{
    using var chaseCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    chaseCts.CancelAfter(timeoutMs);
    try
    {
        return await gameClient.MoveToAsync(
            destination,
            arrivalRadius,
            settings.Navigation.RepathIntervalMs,
            settings.Navigation.StuckTimeoutMs,
            chaseCts.Token);
    }
    catch (OperationCanceledException) when (chaseCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
    {
        return false;
    }
}

static NearbyUnitSnapshot? SelectCombatTarget(
    WorldSnapshot snapshot,
    PlayerSnapshot player,
    FactionTemplateStore factionTemplates,
    BotSettings settings,
    MeleeCombatState state,
    DateTimeOffset now,
    ITargetSelector targetSelector,
    TargetSelectionOptions options)
{
    var preferred = targetSelector.Select(snapshot, options);
    if (preferred is not null &&
        IsTargetAttackable(preferred, player, factionTemplates, settings, state, now))
    {
        return preferred;
    }

    var maxDistanceSq = settings.Combat.AggroRadius * settings.Combat.AggroRadius;
    var maxLevel = player.Level + settings.Combat.MaxTargetLevelDelta;
    var currentPlayer = player;
    var attacker = snapshot.NearbyUnits
        .Where(u => IsTargetAttackable(u, player, factionTemplates, settings, state, now))
        .Where(u => u.TargetGuid == player.Guid)
        .Where(u => u.X.HasValue && u.Y.HasValue && u.Z.HasValue)
        .Select(u => new
        {
            Unit = u,
            DistanceSq = DistanceSquared(currentPlayer, u)
        })
        .Where(x => x.DistanceSq <= maxDistanceSq)
        .OrderBy(x => x.DistanceSq)
        .ThenBy(x => x.Unit.Guid)
        .Select(x => x.Unit)
        .FirstOrDefault();
    if (attacker is not null)
    {
        return attacker;
    }

    return snapshot.NearbyUnits
        .Where(u => IsTargetAttackable(u, player, factionTemplates, settings, state, now))
        .Where(u => u.X.HasValue && u.Y.HasValue && u.Z.HasValue)
        .Select(u => new
        {
            Unit = u,
            DistanceSq = DistanceSquared(player: currentPlayer, target: u)
        })
        .Where(x => x.DistanceSq <= maxDistanceSq)
        .Where(x => !x.Unit.Level.HasValue || x.Unit.Level.Value <= maxLevel)
        .OrderBy(x => x.DistanceSq)
        .ThenBy(x => x.Unit.Guid)
        .Select(x => x.Unit)
        .FirstOrDefault();
}

static bool IsTargetAttackable(
    NearbyUnitSnapshot target,
    PlayerSnapshot player,
    FactionTemplateStore factionTemplates,
    BotSettings settings,
    MeleeCombatState state,
    DateTimeOffset now)
{
    if (!target.IsCreature || target.Guid == 0)
    {
        return false;
    }

    if (!target.IsAttackableHint)
    {
        return false;
    }

    // For PvE grind MVP, skip player-owned summons/pets/mount-like creatures.
    // Wild PvE creatures typically have no owner.
    if (target.OwnerGuid != 0)
    {
        return false;
    }

    if (target.IsDeadHint)
    {
        return false;
    }

    if (player.FactionTemplateId.HasValue && target.FactionTemplateId.HasValue)
    {
        if (factionTemplates.TryGet(player.FactionTemplateId.Value, out var playerFactionTemplate) &&
            factionTemplates.TryGet(target.FactionTemplateId.Value, out var targetFactionTemplate))
        {
            // Mirror Trinity faction-template checks: friendly in either direction means non-attackable.
            if (playerFactionTemplate.IsFriendlyTo(targetFactionTemplate) ||
                targetFactionTemplate.IsFriendlyTo(playerFactionTemplate))
            {
                return false;
            }
        }
    }

    if (target.Level.HasValue && target.Level.Value > player.Level + settings.Combat.MaxTargetLevelDelta)
    {
        return false;
    }

    if (target.HealthPercent.HasValue && target.HealthPercent.Value <= 0)
    {
        return false;
    }

    return !state.BlacklistedTargets.TryGetValue(target.Guid, out var blacklistUntil) || blacklistUntil <= now;
}

static void PurgeBlacklist(MeleeCombatState state, DateTimeOffset now)
{
    if (state.BlacklistedTargets.Count == 0)
    {
        return;
    }

    var expired = state.BlacklistedTargets
        .Where(x => x.Value <= now)
        .Select(x => x.Key)
        .ToArray();
    foreach (var guid in expired)
    {
        state.BlacklistedTargets.Remove(guid);
    }
}

static void PurgeApproachFailures(MeleeCombatState state, DateTimeOffset now, BotSettings settings)
{
    if (state.ApproachFailures.Count == 0)
    {
        return;
    }

    var window = TimeSpan.FromSeconds(settings.Combat.ApproachFailureWindowSeconds);
    var expired = state.ApproachFailures
        .Where(x => now - x.Value.FirstFailureAtUtc > window)
        .Select(x => x.Key)
        .ToArray();
    foreach (var guid in expired)
    {
        state.ApproachFailures.Remove(guid);
    }
}

static void PurgeRecentKillAttributions(MeleeCombatState state, DateTimeOffset now)
{
    if (state.RecentKillAttributions.Count == 0)
    {
        return;
    }

    var expired = state.RecentKillAttributions
        .Where(x => x.Value <= now)
        .Select(x => x.Key)
        .ToArray();
    foreach (var guid in expired)
    {
        state.RecentKillAttributions.Remove(guid);
    }
}

static async Task DetectAndLogAnomaliesAsync(
    WorldSnapshot snapshot,
    BotSettings settings,
    MeleeCombatState meleeState,
    DeathRecoveryState deathState,
    RunMetrics metrics,
    BotAnomalyState anomalyState,
    string logPath)
{
    var now = DateTimeOffset.UtcNow;
    var player = snapshot.Player;
    if (player is null)
    {
        await LogAnomalyAsync(
            metrics,
            anomalyState,
            logPath,
            "missing_player_snapshot",
            "ANOMALY_MISSING_PLAYER snapshot_player=null",
            now,
            TimeSpan.FromSeconds(5));
        return;
    }

    if (player.HealthPercent < 0 || player.HealthPercent > 100)
    {
        await LogAnomalyAsync(
            metrics,
            anomalyState,
            logPath,
            "invalid_player_hp",
            $"ANOMALY_PLAYER_HP_INVALID hp={player.HealthPercent}",
            now,
            TimeSpan.FromSeconds(2));
    }

    if (player.IsGhost && !player.IsDeadHint)
    {
        await LogAnomalyAsync(
            metrics,
            anomalyState,
            logPath,
            "ghost_deadhint_mismatch",
            $"ANOMALY_GHOST_DEADHINT_MISMATCH isGhost={player.IsGhost} isDeadHint={player.IsDeadHint}",
            now,
            TimeSpan.FromSeconds(2));
    }

    if (anomalyState.LastPlayerSample is { } previous)
    {
        var dt = (float)(now - previous.AtUtc).TotalSeconds;
        if (dt > 0.02f)
        {
            var distance = Distance3D(player.X, player.Y, player.Z, previous.X, previous.Y, previous.Z);
            var speed = distance / dt;
            if (distance >= 8.0f && speed >= 20.0f)
            {
                await LogAnomalyAsync(
                    metrics,
                    anomalyState,
                    logPath,
                    "position_jump",
                    $"ANOMALY_POSITION_JUMP dist={distance:F3} dt={dt:F3}s speed={speed:F2}mps from=({previous.X:F3},{previous.Y:F3},{previous.Z:F3}) to=({player.X:F3},{player.Y:F3},{player.Z:F3})",
                    now,
                    TimeSpan.FromMilliseconds(700));
            }
        }
    }

    var movementIntent = false;
    var intentReason = string.Empty;
    if (meleeState.LastChaseCommandAtUtc > now - TimeSpan.FromMilliseconds(Math.Max(1500, settings.Navigation.ChaseCommandIntervalMs * 3)))
    {
        movementIntent = true;
        intentReason = "melee_chase";
    }
    else if (meleeState.LastRepositionAtUtc > now - TimeSpan.FromMilliseconds(Math.Max(1500, settings.Navigation.RepositionCooldownMs * 2)))
    {
        movementIntent = true;
        intentReason = "melee_reposition";
    }
    else if (deathState.LastGhostMoveAttemptAtUtc.HasValue &&
             now - deathState.LastGhostMoveAttemptAtUtc.Value < TimeSpan.FromMilliseconds(Math.Max(2000, settings.Death.GhostCorpseMoveCommandIntervalMs * 2)))
    {
        movementIntent = true;
        intentReason = "ghost_move";
    }

    if (movementIntent && anomalyState.LastPlayerSample is { } movementPrevious)
    {
        var progress = Distance3D(player.X, player.Y, player.Z, movementPrevious.X, movementPrevious.Y, movementPrevious.Z);
        if (progress < 0.12f)
        {
            anomalyState.NoProgressSinceUtc ??= now;
            anomalyState.LastNoProgressReason = intentReason;
            var noProgressFor = now - anomalyState.NoProgressSinceUtc.Value;
            if (noProgressFor >= TimeSpan.FromSeconds(3))
            {
                await LogAnomalyAsync(
                    metrics,
                    anomalyState,
                    logPath,
                    "movement_no_progress",
                    $"ANOMALY_MOVEMENT_NO_PROGRESS reason={anomalyState.LastNoProgressReason} durationMs={(long)noProgressFor.TotalMilliseconds} progress={progress:F3}",
                    now,
                    TimeSpan.FromMilliseconds(900));
            }
        }
        else
        {
            anomalyState.NoProgressSinceUtc = null;
            anomalyState.LastNoProgressReason = string.Empty;
        }
    }
    else
    {
        anomalyState.NoProgressSinceUtc = null;
        anomalyState.LastNoProgressReason = string.Empty;
    }

    if (meleeState.AttackActive && meleeState.AttackingGuid != 0)
    {
        var attackTarget = snapshot.NearbyUnits.FirstOrDefault(u => u.Guid == meleeState.AttackingGuid);
        if (attackTarget is null)
        {
            await LogAnomalyAsync(
                metrics,
                anomalyState,
                logPath,
                "attack_target_missing",
                $"ANOMALY_ATTACK_TARGET_MISSING guid={meleeState.AttackingGuid}",
                now,
                TimeSpan.FromSeconds(1));
        }
        else
        {
            if (attackTarget.IsDeadHint || (attackTarget.HealthPercent.HasValue && attackTarget.HealthPercent.Value <= 0))
            {
                await LogAnomalyAsync(
                    metrics,
                    anomalyState,
                    logPath,
                    "attacking_dead_target",
                    $"ANOMALY_ATTACKING_DEAD_TARGET guid={attackTarget.Guid} hp={(attackTarget.HealthPercent?.ToString() ?? "n/a")} deadHint={attackTarget.IsDeadHint}",
                    now,
                    TimeSpan.FromSeconds(1));
            }

            if (attackTarget.X.HasValue && attackTarget.Y.HasValue && attackTarget.Z.HasValue)
            {
                var targetDistance = Distance3D(player.X, player.Y, player.Z, attackTarget.X.Value, attackTarget.Y.Value, attackTarget.Z.Value);
                var meleeHardDistance = MathF.Max(6.0f, settings.Navigation.MeleeRange + 2.5f);
                if (targetDistance > meleeHardDistance)
                {
                    await LogAnomalyAsync(
                        metrics,
                        anomalyState,
                        logPath,
                        "attack_distance_too_far",
                        $"ANOMALY_ATTACK_DISTANCE_TOO_FAR guid={attackTarget.Guid} dist={targetDistance:F3} threshold={meleeHardDistance:F3}",
                        now,
                        TimeSpan.FromMilliseconds(800));
                }
            }
        }
    }

    if (player.IsGhost)
    {
        var corpseCandidates = snapshot.NearbyUnits
            .Where(u => u.IsCorpse)
            .Count(u => u.X.HasValue && u.Y.HasValue && u.Z.HasValue);
        if (corpseCandidates == 0)
        {
            anomalyState.GhostWithoutCorpseSinceUtc ??= now;
            var ghostNoCorpseFor = now - anomalyState.GhostWithoutCorpseSinceUtc.Value;
            if (ghostNoCorpseFor >= TimeSpan.FromSeconds(6))
            {
                await LogAnomalyAsync(
                    metrics,
                    anomalyState,
                    logPath,
                    "ghost_without_corpse",
                    $"ANOMALY_GHOST_WITHOUT_CORPSE durationMs={(long)ghostNoCorpseFor.TotalMilliseconds}",
                    now,
                    TimeSpan.FromSeconds(2));
            }
        }
        else
        {
            anomalyState.GhostWithoutCorpseSinceUtc = null;
        }
    }
    else
    {
        anomalyState.GhostWithoutCorpseSinceUtc = null;
    }

    if (deathState.ReclaimAwaitingOutcome &&
        deathState.LastReclaimSentAtUtc.HasValue &&
        now - deathState.LastReclaimSentAtUtc.Value >= TimeSpan.FromMilliseconds(Math.Max(2000, settings.Death.ReclaimRejectDecisionMs * 3)))
    {
        await LogAnomalyAsync(
            metrics,
            anomalyState,
            logPath,
            "reclaim_no_outcome",
            $"ANOMALY_RECLAIM_NO_OUTCOME waitMs={(long)(now - deathState.LastReclaimSentAtUtc.Value).TotalMilliseconds} rejects={deathState.ConsecutiveReclaimRejects}",
            now,
            TimeSpan.FromSeconds(2));
    }

    anomalyState.LastPlayerSample = new PlayerMotionSample(player.X, player.Y, player.Z, now);
}

static async Task LogAnomalyAsync(
    RunMetrics metrics,
    BotAnomalyState anomalyState,
    string logPath,
    string key,
    string message,
    DateTimeOffset now,
    TimeSpan cooldown)
{
    if (anomalyState.LastAnomalyLogAtUtc.TryGetValue(key, out var lastAt) &&
        now - lastAt < cooldown)
    {
        return;
    }

    anomalyState.LastAnomalyLogAtUtc[key] = now;
    metrics.AnomalyEvents++;
    await WriteLogAsync(logPath, message);
}

static FactionTemplateStore LoadFactionTemplateStore()
{
    var start = new DirectoryInfo(AppContext.BaseDirectory);
    for (var i = 0; i < 8 && start is not null; i++)
    {
        var candidate = Path.Combine(start.FullName, "data", "dbc", "FactionTemplate.dbc");
        if (File.Exists(candidate))
        {
            try
            {
                return FactionTemplateStore.LoadFromDbc(candidate);
            }
            catch
            {
                return FactionTemplateStore.Empty;
            }
        }

        start = start.Parent;
    }

    return FactionTemplateStore.Empty;
}

static float Distance3D(float ax, float ay, float az, float bx, float by, float bz)
{
    var dx = ax - bx;
    var dy = ay - by;
    var dz = az - bz;
    return MathF.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
}

static float DistanceSquared(PlayerSnapshot player, NearbyUnitSnapshot target)
{
    var dx = player.X - target.X!.Value;
    var dy = player.Y - target.Y!.Value;
    var dz = player.Z - target.Z!.Value;
    return (dx * dx) + (dy * dy) + (dz * dz);
}

static float NormalizeAngleRadians(float angle)
{
    const float twoPi = MathF.PI * 2f;
    while (angle > MathF.PI)
    {
        angle -= twoPi;
    }

    while (angle < -MathF.PI)
    {
        angle += twoPi;
    }

    return angle;
}

file sealed class MeleeCombatState
{
    public ulong LastTargetGuid { get; set; }
    public int? LastTargetHpPercent { get; set; }
    public DateTimeOffset LastTargetHpChangeAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int LastPlayerHpPercent { get; set; } = 100;
    public DateTimeOffset LastCombatProgressAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSwingStartAtUtc { get; set; }
    public bool AttackActive { get; set; }
    public ulong AttackingGuid { get; set; }
    public ulong EngagedTargetGuid { get; set; }
    public DateTimeOffset? EngagedSinceUtc { get; set; }
    public DateTimeOffset? EngagedMissingSinceUtc { get; set; }
    public bool EngagedHadProgress { get; set; }
    public DateTimeOffset LastChaseAtUtc { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset LastChaseCommandAtUtc { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset LastActionAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastRepositionAtUtc { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset LastFaceNudgeAtUtc { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset LastFacingTelemetryAtUtc { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset LastPredictionLogAtUtc { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset? LastEngagedTargetSeenAtUtc { get; set; }
    public Dictionary<ulong, DateTimeOffset> BlacklistedTargets { get; } = new();
    public Dictionary<ulong, MotionSamplePair> TargetMotionSamples { get; } = new();
    public Dictionary<ulong, ApproachFailureState> ApproachFailures { get; } = new();
    public Dictionary<ulong, DateTimeOffset> RecentKillAttributions { get; } = new();
}

file sealed class DeathRecoveryState
{
    public bool IsDeadFlowActive { get; set; }
    public bool RepopSent { get; set; }
    public DateTimeOffset? DiedAtUtc { get; set; }
    public DateTimeOffset? RepopSentAtUtc { get; set; }
    public DateTimeOffset? LastRepopAttemptAtUtc { get; set; }
    public DateTimeOffset? LastReclaimAttemptAtUtc { get; set; }
    public DateTimeOffset? LastStatusLogAtUtc { get; set; }
    public bool AutoResWaitLogged { get; set; }
    public int LastObservedHp { get; set; } = int.MinValue;
    public bool? LastObservedIsGhost { get; set; }
    public bool? LastObservedIsDeadHint { get; set; }
    public ulong LastObservedNearestCorpseGuid { get; set; }
    public int RepopAttemptsForThisDeath { get; set; }
    public int ReclaimAttemptsForThisDeath { get; set; }
    public DateTimeOffset? LastGhostMoveAttemptAtUtc { get; set; }
    public int GhostMoveAttemptsForThisDeath { get; set; }
    public float? DeathX { get; set; }
    public float? DeathY { get; set; }
    public float? DeathZ { get; set; }
    public DateTimeOffset? LastReclaimSentAtUtc { get; set; }
    public bool ReclaimAwaitingOutcome { get; set; }
    public int ConsecutiveReclaimRejects { get; set; }
}

file sealed class ExplorationState
{
    public bool IsActive { get; set; }
    public DateTimeOffset? LastAttemptAtUtc { get; set; }
    public NavigationPoint? LastDestination { get; set; }
    public ExplorationPositionSample? LastObservedPosition { get; set; }
    public DateTimeOffset? NoProgressSinceUtc { get; set; }
    public Dictionary<ExplorationGridCell, DateTimeOffset> VisitedCells { get; } = new();
    public Dictionary<ExplorationGridCell, DateTimeOffset> BlacklistedCells { get; } = new();
}

file sealed class BotAnomalyState
{
    public PlayerMotionSample? LastPlayerSample { get; set; }
    public DateTimeOffset? NoProgressSinceUtc { get; set; }
    public string LastNoProgressReason { get; set; } = string.Empty;
    public DateTimeOffset? GhostWithoutCorpseSinceUtc { get; set; }
    public Dictionary<string, DateTimeOffset> LastAnomalyLogAtUtc { get; } = new(StringComparer.Ordinal);
}

file sealed class BotActivityState
{
    public string LastActionLabel { get; set; } = string.Empty;
    public DateTimeOffset LastActionAtUtc { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset LastNoActionLogAtUtc { get; set; } = DateTimeOffset.MinValue;
}

file sealed class RunMetrics
{
    private DateTimeOffset _lastTickAtUtc;
    private DateTimeOffset _endedAtUtc;
    private double _combatSeconds;

    public RunMetrics(DateTimeOffset startedAtUtc)
    {
        StartedAtUtc = startedAtUtc;
        _lastTickAtUtc = startedAtUtc;
        _endedAtUtc = startedAtUtc;
    }

    public DateTimeOffset StartedAtUtc { get; }
    public int Kills { get; set; }
    public int ApproachOk { get; set; }
    public int ApproachKo { get; set; }
    public int ApproachRetryOk { get; set; }
    public int ApproachRetryKo { get; set; }
    public int StuckSwitches { get; set; }
    public int SwingStartOk { get; set; }
    public int SwingStartFail { get; set; }
    public int TargetLostResets { get; set; }
    public int InvalidResetDeadHint { get; set; }
    public int InvalidResetUnknownHp { get; set; }
    public int InvalidResetLeash { get; set; }
    public int InvalidResetHpReset { get; set; }
    public int InvalidResetApproachFailures { get; set; }
    public int Deaths { get; set; }
    public int Releases { get; set; }
    public int ReclaimAttempts { get; set; }
    public int Resurrections { get; set; }
    public int AnomalyEvents { get; set; }
    public int KillsObservedDead { get; set; }
    public int KillsAttributed { get; set; }

    public void RecordTick(DateTimeOffset now, bool inCombat)
    {
        if (now <= _lastTickAtUtc)
        {
            return;
        }

        if (inCombat)
        {
            _combatSeconds += (now - _lastTickAtUtc).TotalSeconds;
        }

        _lastTickAtUtc = now;
    }

    public void Finish(DateTimeOffset endedAtUtc)
    {
        _endedAtUtc = endedAtUtc;
    }

    public string BuildSummary()
    {
        var snapshot = BuildSnapshot();
        static string F1(double value) => value.ToString("F1", CultureInfo.InvariantCulture);
        static string F2(double value) => value.ToString("F2", CultureInfo.InvariantCulture);
        return
            $"RUN_SUMMARY " +
            $"durationSec={F1(snapshot.DurationSec)} " +
            $"kills={snapshot.Kills} kpm={F2(snapshot.KillsPerMinute)} " +
            $"combatUptimePct={F1(snapshot.CombatUptimePct)} " +
            $"approachOk={snapshot.ApproachOk} approachKo={snapshot.ApproachKo} approachSuccessPct={F1(snapshot.ApproachSuccessPct)} " +
            $"approachRetryOk={snapshot.ApproachRetryOk} approachRetryKo={snapshot.ApproachRetryKo} " +
            $"swingStartOk={snapshot.SwingStartOk} swingStartFail={snapshot.SwingStartFail} " +
            $"stuckSwitches={snapshot.StuckSwitches} targetLostResets={snapshot.TargetLostResets} " +
            $"deaths={snapshot.Deaths} releases={snapshot.Releases} reclaimAttempts={snapshot.ReclaimAttempts} resurrections={snapshot.Resurrections} " +
            $"killsObservedDead={snapshot.KillsObservedDead} killsAttributed={snapshot.KillsAttributed} " +
            $"anomalies={snapshot.AnomalyEvents} " +
            $"invalidResets={snapshot.InvalidResetsTotal} " +
            $"invalid_deadHint={snapshot.InvalidResetDeadHint} invalid_unknownHp={snapshot.InvalidResetUnknownHp} " +
            $"invalid_leash={snapshot.InvalidResetLeash} invalid_hpReset={snapshot.InvalidResetHpReset} invalid_approachFailures={snapshot.InvalidResetApproachFailures}";
    }

    public string BuildSummaryJsonLine()
    {
        var snapshot = BuildSnapshot();
        return JsonSerializer.Serialize(snapshot);
    }

    private RunSummarySnapshot BuildSnapshot()
    {
        var totalSeconds = Math.Max(0.01, (_endedAtUtc - StartedAtUtc).TotalSeconds);
        var killsPerMin = Kills / (totalSeconds / 60.0);
        var combatUptimePct = Math.Clamp((_combatSeconds / totalSeconds) * 100.0, 0.0, 100.0);
        var totalApproach = ApproachOk + ApproachKo;
        var approachSuccessPct = totalApproach > 0
            ? (ApproachOk * 100.0 / totalApproach)
            : 0.0;
        var totalInvalidResets = InvalidResetDeadHint +
                                 InvalidResetUnknownHp +
                                 InvalidResetLeash +
                                 InvalidResetHpReset +
                                 InvalidResetApproachFailures;
        return new RunSummarySnapshot(
            StartedAtUtc,
            _endedAtUtc,
            totalSeconds,
            Kills,
            killsPerMin,
            combatUptimePct,
            ApproachOk,
            ApproachKo,
            approachSuccessPct,
            ApproachRetryOk,
            ApproachRetryKo,
            SwingStartOk,
            SwingStartFail,
            StuckSwitches,
            TargetLostResets,
            Deaths,
            Releases,
            ReclaimAttempts,
            Resurrections,
            KillsObservedDead,
            KillsAttributed,
            AnomalyEvents,
            totalInvalidResets,
            InvalidResetDeadHint,
            InvalidResetUnknownHp,
            InvalidResetLeash,
            InvalidResetHpReset,
            InvalidResetApproachFailures);
    }
}

file readonly record struct RunSummarySnapshot(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    double DurationSec,
    int Kills,
    double KillsPerMinute,
    double CombatUptimePct,
    int ApproachOk,
    int ApproachKo,
    double ApproachSuccessPct,
    int ApproachRetryOk,
    int ApproachRetryKo,
    int SwingStartOk,
    int SwingStartFail,
    int StuckSwitches,
    int TargetLostResets,
    int Deaths,
    int Releases,
    int ReclaimAttempts,
    int Resurrections,
    int KillsObservedDead,
    int KillsAttributed,
    int AnomalyEvents,
    int InvalidResetsTotal,
    int InvalidResetDeadHint,
    int InvalidResetUnknownHp,
    int InvalidResetLeash,
    int InvalidResetHpReset,
    int InvalidResetApproachFailures);

file readonly record struct MotionSample(float X, float Y, float Z, DateTimeOffset AtUtc);
file readonly record struct MotionSamplePair(MotionSample Previous, MotionSample Latest);
file readonly record struct ApproachFailureState(int Count, DateTimeOffset FirstFailureAtUtc);
file readonly record struct PlayerMotionSample(float X, float Y, float Z, DateTimeOffset AtUtc);
file readonly record struct ExplorationGridCell(int X, int Y);
file readonly record struct ExplorationPositionSample(float X, float Y, float Z, DateTimeOffset AtUtc);
file readonly record struct FactionTemplateEntryLite(
    int Id,
    int Faction,
    uint Flags,
    uint FactionGroup,
    uint FriendGroup,
    uint EnemyGroup,
    uint Enemy0,
    uint Enemy1,
    uint Enemy2,
    uint Enemy3,
    uint Friend0,
    uint Friend1,
    uint Friend2,
    uint Friend3)
{
    public bool IsFriendlyTo(FactionTemplateEntryLite other)
    {
        if (other.Faction != 0)
        {
            if (Enemy0 == (uint)other.Faction || Enemy1 == (uint)other.Faction || Enemy2 == (uint)other.Faction || Enemy3 == (uint)other.Faction)
            {
                return false;
            }

            if (Friend0 == (uint)other.Faction || Friend1 == (uint)other.Faction || Friend2 == (uint)other.Faction || Friend3 == (uint)other.Faction)
            {
                return true;
            }
        }

        return ((FriendGroup & other.FactionGroup) != 0) || ((FactionGroup & other.FriendGroup) != 0);
    }

    public bool IsHostileTo(FactionTemplateEntryLite other)
    {
        if (other.Faction != 0)
        {
            if (Enemy0 == (uint)other.Faction || Enemy1 == (uint)other.Faction || Enemy2 == (uint)other.Faction || Enemy3 == (uint)other.Faction)
            {
                return true;
            }

            if (Friend0 == (uint)other.Faction || Friend1 == (uint)other.Faction || Friend2 == (uint)other.Faction || Friend3 == (uint)other.Faction)
            {
                return false;
            }
        }

        return (EnemyGroup & other.FactionGroup) != 0;
    }
}

file sealed class FactionTemplateStore
{
    public static FactionTemplateStore Empty { get; } = new([]);

    private readonly Dictionary<int, FactionTemplateEntryLite> _templates;

    private FactionTemplateStore(Dictionary<int, FactionTemplateEntryLite> templates)
    {
        _templates = templates;
    }

    public bool TryGet(int id, out FactionTemplateEntryLite entry) => _templates.TryGetValue(id, out entry);

    public static FactionTemplateStore LoadFromDbc(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);
        var magic = br.ReadUInt32();
        const uint WdbcMagic = 0x43424457; // "WDBC"
        if (magic != WdbcMagic)
        {
            return Empty;
        }

        var recordCount = br.ReadInt32();
        var fieldCount = br.ReadInt32();
        var recordSize = br.ReadInt32();
        var stringBlockSize = br.ReadInt32();
        if (recordCount <= 0 || fieldCount < 14 || recordSize < (fieldCount * 4))
        {
            return Empty;
        }

        var templates = new Dictionary<int, FactionTemplateEntryLite>(recordCount);
        for (var i = 0; i < recordCount; i++)
        {
            var record = br.ReadBytes(recordSize);
            if (record.Length != recordSize)
            {
                break;
            }

            static uint ReadField(byte[] record, int field)
            {
                var offset = field * 4;
                return BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(offset, 4));
            }

            var id = (int)ReadField(record, 0);
            if (id <= 0)
            {
                continue;
            }

            templates[id] = new FactionTemplateEntryLite(
                id,
                (int)ReadField(record, 1),
                ReadField(record, 2),
                ReadField(record, 3),
                ReadField(record, 4),
                ReadField(record, 5),
                ReadField(record, 6),
                ReadField(record, 7),
                ReadField(record, 8),
                ReadField(record, 9),
                ReadField(record, 10),
                ReadField(record, 11),
                ReadField(record, 12),
                ReadField(record, 13));
        }

        // consume string block to keep reader aligned and avoid unused header confusion
        _ = br.ReadBytes(Math.Max(0, stringBlockSize));
        return new FactionTemplateStore(templates);
    }
}

internal sealed class BotSettings
{
    public required ConnectionSettings Connection { get; init; }
    public required CombatSettings Combat { get; init; }
    public required DeathSettings Death { get; init; }
    public required NavigationSettings Navigation { get; init; }
    public required BehaviorSettings Behavior { get; init; }
    public required LoggingSettings Logging { get; init; }
    public ExplorationSettings Exploration { get; init; } = new();

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

        var fallback = CreateDefault();
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(fallback, new JsonSerializerOptions { WriteIndented = true }));
        return fallback;
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
                MaxTargetLevelDelta = 0,
                NoHpChangeTimeoutSeconds = 10,
                BlacklistSecondsAfterStuck = 10,
                StuckCheckGraceSeconds = 2,
                MinEngagementSeconds = 15,
                MissingTargetGraceSeconds = 8,
                LostTargetResetSeconds = 3,
                IdleRecoverySeconds = 4,
                UnknownTargetHpNoDamageTimeoutSeconds = 8,
                FinisherNoProgressTimeoutSeconds = 25,
                MaxApproachFailures = 4,
                ApproachFailureWindowSeconds = 12,
                TargetResetHpJumpPercent = 20,
                LeashResetDistanceMultiplier = 1.35f
            },
            Death = new DeathSettings
            {
                ReleaseDelayMs = 1000,
                AutoResWaitAfterRepopMs = 12000,
                ReclaimAttemptIntervalMs = 2000,
                ReclaimMaxIntervalMs = 15000,
                ReclaimBackoffMultiplier = 1.5f,
                ReclaimRejectDecisionMs = 1500,
                DeadCorpseRepopRetryIntervalMs = 3000,
                CorpseSearchRadius = 39f,
                GhostCorpseMoveCommandIntervalMs = 1500,
                GhostCorpseApproachArrivalRadius = 2.5f,
                GhostCorpseMoveStuckTimeoutMs = 6000
            },
            Navigation = new NavigationSettings
            {
                RepathIntervalMs = 750,
                StuckTimeoutMs = 3000,
                FollowProjectionMapId = 0,
                MeleeRange = 2.5f,
                MeleeStopBuffer = 1.5f,
                ChaseStepTimeoutMs = 2000,
                ChaseCommandIntervalMs = 900,
                ChaseRetryTimeoutMs = 1200,
                ChaseRetryArrivalBuffer = 0.75f,
                ChaseAdaptiveArrivalPerFailure = 0.25f,
                ChaseAdaptiveArrivalMaxBonus = 1.0f,
                StuckCheckMaxDistance = 4.0f,
                ChaseStartBuffer = 1.25f,
                CloseRangeNudgeSeconds = 2,
                MinMeleeDistance = 1.1f,
                PreferredMeleeDistance = 1.8f,
                PreferredMeleeTolerance = 0.35f,
                OverlapRepositionDistance = 0.45f,
                RepositionCooldownMs = 1000,
                RepositionStepTimeoutMs = 700,
                FaceNudgeCooldownMs = 2000,
                FaceNudgeNoProgressMs = 1600,
                FaceNudgeStepTimeoutMs = 500,
                FaceRefreshBackAngleDeg = 130f,
                TargetPredictionMaxAgeMs = 1200,
                TargetPredictionMinLeadSeconds = 0.15f,
                TargetPredictionMaxLeadSeconds = 0.90f,
                TargetPredictionMinSpeed = 0.40f,
                TargetPredictionMaxSpeed = 9.0f,
                TargetPredictionMaxDistance = 6.0f
            },
            Behavior = new BehaviorSettings
            {
                TickMs = 200,
                RunDurationSeconds = 20,
                EnableStartupMoveTest = false,
                StartMoveXOffset = 5.0f,
                ArrivalRadius = 2.5f,
                FollowNearestPlayer = false,
                FollowMinDistance = 1.0f,
                FollowResumeDistance = 2.0f,
                NoActionLogSeconds = 5,
                PhaseWatchdogTimeoutMs = 10000,
                PhaseWatchdogWarnMs = 1500,
                PhaseWatchdogBlockedLogIntervalMs = 2000
            },
            Logging = new LoggingSettings
            {
                Directory = "logs",
                EnablePacketTrace = true
            },
            Exploration = new ExplorationSettings
            {
                Enabled = true,
                IdleBeforeExploreMs = 4000,
                ExploreCommandIntervalMs = 1200,
                StepMinDistance = 10.0f,
                StepMaxDistance = 35.0f,
                CandidateCount = 20,
                ArrivalRadius = 2.5f,
                StepTimeoutMs = 3500,
                GridCellSize = 14.0f,
                VisitDecaySeconds = 120,
                BlacklistSeconds = 20,
                NoProgressTimeoutMs = 4000,
                MinProgressDistance = 0.30f
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
    public int NoHpChangeTimeoutSeconds { get; init; } = 10;
    public int BlacklistSecondsAfterStuck { get; init; } = 10;
    public int StuckCheckGraceSeconds { get; init; } = 2;
    public int MinEngagementSeconds { get; init; } = 15;
    public int MissingTargetGraceSeconds { get; init; } = 8;
    public int LostTargetResetSeconds { get; init; } = 3;
    public int IdleRecoverySeconds { get; init; } = 4;
    public int UnknownTargetHpNoDamageTimeoutSeconds { get; init; } = 8;
    public int FinisherNoProgressTimeoutSeconds { get; init; } = 25;
    public int MaxApproachFailures { get; init; } = 4;
    public int ApproachFailureWindowSeconds { get; init; } = 12;
    public int TargetResetHpJumpPercent { get; init; } = 20;
    public float LeashResetDistanceMultiplier { get; init; } = 1.35f;
}

internal sealed class DeathSettings
{
    public int ReleaseDelayMs { get; init; } = 1000;
    public int AutoResWaitAfterRepopMs { get; init; } = 12000;
    public int ReclaimAttemptIntervalMs { get; init; } = 2000;
    public int ReclaimMaxIntervalMs { get; init; } = 15000;
    public float ReclaimBackoffMultiplier { get; init; } = 1.5f;
    public int ReclaimRejectDecisionMs { get; init; } = 1500;
    public int DeadCorpseRepopRetryIntervalMs { get; init; } = 3000;
    public float CorpseSearchRadius { get; init; } = 39f;
    public int GhostCorpseMoveCommandIntervalMs { get; init; } = 1500;
    public float GhostCorpseApproachArrivalRadius { get; init; } = 2.5f;
    public int GhostCorpseMoveStuckTimeoutMs { get; init; } = 6000;
}

internal sealed class BehaviorSettings
{
    public required int TickMs { get; init; }
    public required int RunDurationSeconds { get; init; }
    public bool EnableStartupMoveTest { get; init; }
    public required float StartMoveXOffset { get; init; }
    public required float ArrivalRadius { get; init; }
    public required bool FollowNearestPlayer { get; init; }
    public required float FollowMinDistance { get; init; }
    public required float FollowResumeDistance { get; init; }
    public int NoActionLogSeconds { get; init; } = 5;
    public int PhaseWatchdogTimeoutMs { get; init; } = 10000;
    public int PhaseWatchdogWarnMs { get; init; } = 1500;
    public int PhaseWatchdogBlockedLogIntervalMs { get; init; } = 2000;
}

internal sealed class NavigationSettings
{
    public required int RepathIntervalMs { get; init; }
    public required int StuckTimeoutMs { get; init; }
    public required int FollowProjectionMapId { get; init; }
    public float MeleeRange { get; init; } = 2.5f;
    public float MeleeStopBuffer { get; init; } = 1.5f;
    public int ChaseStepTimeoutMs { get; init; } = 2000;
    public int ChaseCommandIntervalMs { get; init; } = 900;
    public int ChaseRetryTimeoutMs { get; init; } = 1200;
    public float ChaseRetryArrivalBuffer { get; init; } = 0.75f;
    public float ChaseAdaptiveArrivalPerFailure { get; init; } = 0.25f;
    public float ChaseAdaptiveArrivalMaxBonus { get; init; } = 1.0f;
    public float StuckCheckMaxDistance { get; init; } = 4.0f;
    public float ChaseStartBuffer { get; init; } = 1.25f;
    public int CloseRangeNudgeSeconds { get; init; } = 2;
    public float MinMeleeDistance { get; init; } = 1.1f;
    public float PreferredMeleeDistance { get; init; } = 1.8f;
    public float PreferredMeleeTolerance { get; init; } = 0.35f;
    public float OverlapRepositionDistance { get; init; } = 0.45f;
    public int RepositionCooldownMs { get; init; } = 1000;
    public int RepositionStepTimeoutMs { get; init; } = 700;
    public int FaceNudgeCooldownMs { get; init; } = 2000;
    public int FaceNudgeNoProgressMs { get; init; } = 1600;
    public int FaceNudgeStepTimeoutMs { get; init; } = 500;
    public float FaceRefreshBackAngleDeg { get; init; } = 130f;
    public int TargetPredictionMaxAgeMs { get; init; } = 1200;
    public float TargetPredictionMinLeadSeconds { get; init; } = 0.15f;
    public float TargetPredictionMaxLeadSeconds { get; init; } = 0.90f;
    public float TargetPredictionMinSpeed { get; init; } = 0.40f;
    public float TargetPredictionMaxSpeed { get; init; } = 9.0f;
    public float TargetPredictionMaxDistance { get; init; } = 6.0f;
}

internal sealed class LoggingSettings
{
    public required string Directory { get; init; }
    public required bool EnablePacketTrace { get; init; }
}

internal sealed class ExplorationSettings
{
    public bool Enabled { get; init; } = true;
    public int IdleBeforeExploreMs { get; init; } = 4000;
    public int ExploreCommandIntervalMs { get; init; } = 1200;
    public float StepMinDistance { get; init; } = 10.0f;
    public float StepMaxDistance { get; init; } = 35.0f;
    public int CandidateCount { get; init; } = 20;
    public float ArrivalRadius { get; init; } = 2.5f;
    public int StepTimeoutMs { get; init; } = 3500;
    public float GridCellSize { get; init; } = 14.0f;
    public int VisitDecaySeconds { get; init; } = 120;
    public int BlacklistSeconds { get; init; } = 20;
    public int NoProgressTimeoutMs { get; init; } = 4000;
    public float MinProgressDistance { get; init; } = 0.30f;
}
