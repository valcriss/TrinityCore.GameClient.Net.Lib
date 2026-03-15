using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Buffers.Binary;
using System.Text;
using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Models;
using TrinityCore.GameClient.Net.GameState.Abstractions;
using TrinityCore.GameClient.Net.GameState.Model;
using TrinityCore.GameClient.Net.Navigation.Abstractions;
using TrinityCore.GameClient.Net.Protocol.Auth;
using TrinityCore.GameClient.Net.Protocol.Crypto;
using TrinityCore.GameClient.Net.Protocol.World;
using TrinityCore.GameClient.Net.Transport.Abstractions;

namespace TrinityCore.GameClient.Net.Client.Real;

public sealed class RealGameClient(
    IConnectionFactory connectionFactory,
    IGameStateStore gameStateStore,
    IPathfinder pathfinder) : IGameClient, IAsyncDisposable
{
    private const int DefaultTimeoutMs = 6000;
    private const float ChaseConvergenceDisableDistance = 6.0f;
    private const float MovingHardCorrectionDistance = 4.0f;
    private const int MovingHardCorrectionStreakThreshold = 3;
    private const int ChaseRepathMinIntervalMs = 1500;
    private const float ChaseStepMinDistance = 0.12f;
    private const float ChaseStepSpeedMultiplier = 1.00f;
    private const float ServerForceLogThreshold = 0.03f;
    private const int DuplicateMovementCommandWindowMs = 250;
    private const int OutgoingValidationSampleLimitPerOpcode = 32;
    private const int OutgoingValidationHexPreviewBytes = 48;
    private const int HeartbeatIntervalMs = 100;
    private const int VisibilityCheckIntervalMs = 750;
    private const int VisibilityLookAheadMinPathPoints = 3;
    private const int VisibilityLookAheadMaxPoints = 8;
    private const float VisibilityShortcutMaxDistance = 14.0f;
    private const float VisibilityShortcutMaxDeviation = 1.25f;
    private const float HeartbeatGroundRecoveryDeltaZ = 0.18f;
    private const float HeartbeatGroundProbeUpgradeMinDeltaZ = 0.08f;
    private const float HeartbeatGroundProbeHighOffsetZ = 1.10f;
    private const float HeartbeatGroundProbeLowOffsetZ = 0.90f;
    private const float HeartbeatGroundContinuityMaxDeltaZ = 1.25f;
    private const float HeartbeatGroundDirectionalIntentMinDeltaZ = 0.25f;
    private const float HeartbeatGroundProbeOvershootPenaltyDeltaZ = 0.20f;
    private const float HeartbeatGroundCurrentFallbackPenaltyDeltaZ = 0.20f;
    private const float HeartbeatGroundChaseEnvelopeMarginZ = 0.35f;
    private const float HeartbeatGroundChaseNonStepPenalty = 0.35f;
    private const float HeartbeatGroundChaseStepDeviationPenaltyDeltaZ = 0.45f;
    private const float HeartbeatGroundChaseContinuityPenaltyDeltaZ = 0.65f;
    private const int SelfUpdateObjectPositionFreshMs = 1200;
    private const float SelfUpdateObjectStaleDistance = 3.0f;
    private const int SnapshotProjectionIntervalMs = 100;
    private readonly WorldCrypto _worldCrypto = new();
    private readonly SemaphoreSlim _worldSendLock = new(1, 1);
    private readonly SemaphoreSlim _movementExecutionGate = new(1, 1);
    private readonly object _movementCommandSync = new();
    private readonly SemaphoreSlim _questRequestLock = new(1, 1);
    private readonly object _questStateSync = new();
    private readonly object _socialStateSync = new();

    private IConnection? _authConnection;
    private IConnection? _worldConnection;
    private string? _usernameUpper;
    private string? _password;
    private uint _realmId;
    private byte[] _sessionKeyBytes = [];
    private byte[] _expectedServerProof = [];
    private CancellationTokenSource? _keepAliveCts;
    private Task? _keepAliveTask;
    private CancellationTokenSource? _worldReceiveCts;
    private Task? _worldReceiveTask;
    private CancellationTokenSource? _snapshotProjectionCts;
    private Task? _snapshotProjectionTask;
    private TaskCompletionSource<LogoutResponseData>? _logoutResponseTcs;
    private TaskCompletionSource<bool>? _logoutCompleteTcs;
    private TaskCompletionSource<bool>? _logoutCancelAckTcs;
    private CharacterInfo? _currentCharacter;
    private PlayerSnapshot? _currentPlayer;
    private TargetSnapshot? _currentTarget;
    private int _currentMapId = -1;
    private float _currentOrientation;
    private float _currentRunSpeed = 7.0f;
    private long _lastMovementCommandAtTick;
    private bool _isMoveForwardActive;
    private bool _isAutoAttackActive;
    private ulong _autoAttackTargetGuid;
    private readonly object _entitiesSync = new();
    private readonly Dictionary<ulong, EntityState> _entities = [];
    private readonly Dictionary<ulong, SpeedSample> _entitySpeedSamples = [];
    private volatile bool _isChaseMode;
    private MovementTelemetry? _lastSentMovement;
    private MovementTelemetry? _lastReceivedSelfMovement;
    private SelfAuthorityDriftTelemetry? _lastSelfAuthorityDrift;
    private SpeedSample? _selfSpeedSample;
    private NavigationPoint? _serverObservedSelfPosition;
    private NavigationPoint? _lastUpdateObjectSelfPosition;
    private long _lastUpdateObjectSelfPositionChangeAtTick;
    private int _movingServerCorrectionStreak;
    private long _lastMoveStartSentAtTick;
    private long _lastMoveStopSentAtTick;
    private readonly Dictionary<WorldOpcode, int> _outgoingValidationSamples = [];
    private uint? _lastSelfRawHealth;
    private uint? _lastSelfRawMaxHealth;
    private uint? _lastSelfRawPlayerFlags;
    private uint? _lastSelfRawUnitBytes1;
    private int _lastCorpseEntityCount = -1;
    private long _serverGhostHintUntilTick;
    private long _serverDeathHintUntilTick;
    private long _serverAliveHintUntilTick;
    private long _corpseReclaimBlockedUntilTick;
    private NavigationPoint? _deathReleaseLocation;
    private int _deathReleaseMapId = -1;
    private long _deathReleaseLocationUntilTick;
    private bool _deathMovementResetApplied;
    private CancellationTokenSource? _activeMovementCommandCts;
    private long _activeMovementCommandId;
    private string? _activeMovementCommandName;
    private long _movementCommandSequence;
    private TaskCompletionSource<QuestGiverStatusInfo>? _questStatusTcs;
    private TaskCompletionSource<QuestGiverMenu>? _questMenuTcs;
    private TaskCompletionSource<QuestDialog>? _questDialogTcs;
    private TaskCompletionSource<QuestDefinition>? _questDefinitionTcs;
    private TaskCompletionSource<IReadOnlyList<QuestPoiInfo>>? _questPoiTcs;
    private TaskCompletionSource<byte>? _questInvalidTcs;
    private TaskCompletionSource<QuestTurnInResult>? _questTurnInResultTcs;
    private readonly Dictionary<uint, QuestDefinition> _questDefinitions = [];
    private readonly Dictionary<uint, QuestPoiInfo> _questPois = [];
    private readonly Dictionary<uint, ActiveQuestState> _activeQuests = [];
    private GroupMembershipInfo? _currentGroup;
    private GroupInviteInfo? _pendingGroupInvite;
    private readonly Queue<ReceivedChatMessage> _incomingChatMessages = new();

    public bool AutoAcceptGroupInvites { get; set; } = true;

    public GroupMembershipInfo? CurrentGroup
    {
        get
        {
            lock (_socialStateSync)
            {
                return _currentGroup;
            }
        }
    }

    public MeleeAttackState CurrentMeleeAttack => new(_isAutoAttackActive, _autoAttackTargetGuid);

    public GroupInviteInfo? PendingGroupInvite
    {
        get
        {
            lock (_socialStateSync)
            {
                return _pendingGroupInvite;
            }
        }
    }

    public IReadOnlyList<ReceivedChatMessage> DrainIncomingChatMessages()
    {
        lock (_socialStateSync)
        {
            if (_incomingChatMessages.Count == 0)
            {
                return [];
            }

            var messages = _incomingChatMessages.ToArray();
            _incomingChatMessages.Clear();
            return messages;
        }
    }

    public async Task<bool> LoginAsync(AuthServerInfo server, AuthServerCredentials credentials, CancellationToken cancellationToken = default)
    {
        await DisconnectAllAsync(cancellationToken);
        UpdateSnapshot(null, null);

        _usernameUpper = credentials.Login.ToUpperInvariant();
        _password = credentials.Password;
        _authConnection = connectionFactory.Create();
        await _authConnection.ConnectAsync(server.Host, server.Port, cancellationToken);
        Trace.WriteLine($"NET AUTH CONNECT {server.Host}:{server.Port}");

        var localAddress = (_authConnection.LocalAddress ?? IPAddress.Loopback).MapToIPv4();
        var challengePacket = AuthPacketCodec.BuildLogonChallengePacket(_usernameUpper, localAddress);
        Trace.WriteLine($"NET AUTH SEND {(AuthCommand)challengePacket[0]} len={challengePacket.Length}");
        await _authConnection.SendAsync(challengePacket, cancellationToken);

        var authChallengePacket = await ReadAuthPacketAsync(_authConnection, cancellationToken);
        if (authChallengePacket.Command != AuthCommand.LogonChallenge)
        {
            return false;
        }

        var challenge = AuthPacketCodec.ParseLogonChallengePayload(authChallengePacket.Payload);
        if (challenge.Result != AuthResult.Success)
        {
            return false;
        }

        var proofData = AuthProofCalculator.Compute(_usernameUpper, _password, challenge);
        _sessionKeyBytes = proofData.SessionKeyBytes;
        _expectedServerProof = proofData.ExpectedM2;

        var proofPacket = AuthPacketCodec.BuildLogonProofPacket(proofData.ClientPublicEphemeral, proofData.ClientM1, new byte[20]);
        Trace.WriteLine($"NET AUTH SEND {(AuthCommand)proofPacket[0]} len={proofPacket.Length}");
        await _authConnection.SendAsync(proofPacket, cancellationToken);

        var authProofPacket = await ReadAuthPacketAsync(_authConnection, cancellationToken);
        if (authProofPacket.Command != AuthCommand.LogonProof)
        {
            return false;
        }

        var proofResponse = AuthPacketCodec.ParseLogonProofPayload(authProofPacket.Payload);
        return proofResponse.Result == AuthResult.Success &&
               AuthProofCalculator.ValidateServerProof(_expectedServerProof, proofResponse.M2);
    }

    public async Task<IReadOnlyList<RealmInfo>> GetRealmsAsync(CancellationToken cancellationToken = default)
    {
        if (_authConnection is null || !_authConnection.IsConnected)
        {
            return [];
        }

        await _authConnection.SendAsync(AuthPacketCodec.BuildRealmListPacket(), cancellationToken);
        Trace.WriteLine($"NET AUTH SEND {AuthCommand.RealmList} len=5");
        var response = await ReadAuthPacketAsync(_authConnection, cancellationToken);
        if (response.Command != AuthCommand.RealmList)
        {
            return [];
        }

        var realms = AuthPacketCodec.ParseRealmListPayload(response.Payload);
        return realms.Select(r => new RealmInfo(r.Id, r.Name, r.Address, r.Port)).ToArray();
    }

    public async Task<bool> ConnectRealmAsync(RealmInfo realm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_usernameUpper) || _sessionKeyBytes.Length == 0)
        {
            return false;
        }

        _realmId = (uint)realm.Id;
        _worldConnection = connectionFactory.Create();
        await _worldConnection.ConnectAsync(realm.Address, realm.Port, cancellationToken);
        Trace.WriteLine($"NET WORLD CONNECT {realm.Address}:{realm.Port}");

        while (true)
        {
            var packet = await ReadWorldPacketAsync(_worldConnection, cancellationToken);
            switch (packet.Header.Opcode)
            {
                case WorldOpcode.ServerAuthChallenge:
                {
                    var serverSeed = WorldPacketCodec.ParseServerAuthChallenge(packet.Payload);
                    var clientSeed = BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(4), 0);
                    var authPayload = WorldPacketCodec.BuildAuthSessionPayload(
                        _usernameUpper,
                        _realmId,
                        serverSeed,
                        new BigInteger(_sessionKeyBytes, isUnsigned: true, isBigEndian: false),
                        clientSeed);

                    await SendWorldPacketAsync(_worldConnection, WorldOpcode.ClientAuthSession, authPayload, cancellationToken);
                    _worldCrypto.Initialize(_sessionKeyBytes);
                    break;
                }

                case WorldOpcode.ServerAuthResponse:
                    return WorldPacketCodec.ParseServerAuthResponse(packet.Payload);

                case WorldOpcode.SmsgTimeSyncReq:
                    await RespondTimeSyncAsync(packet.Payload, cancellationToken);
                    break;
            }
        }
    }

    public async Task<IReadOnlyList<CharacterInfo>> GetCharactersAsync(CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            return [];
        }

        await SendWorldPacketAsync(_worldConnection, WorldOpcode.CmsgCharEnum, WorldPacketCodec.BuildCharacterEnumPayload(), cancellationToken);
        while (true)
        {
            var packet = await ReadWorldPacketAsync(_worldConnection, cancellationToken);
            switch (packet.Header.Opcode)
            {
                case WorldOpcode.SmsgCharEnum:
                    return WorldPacketCodec
                        .ParseCharacterList(packet.Payload)
                        .Select(x => new CharacterInfo(x.Guid, x.Name, x.Level, x.RaceId, x.ClassId, x.GenderId))
                        .ToArray();
                case WorldOpcode.SmsgTimeSyncReq:
                    await RespondTimeSyncAsync(packet.Payload, cancellationToken);
                    break;
            }
        }
    }

    public async Task<bool> EnterWorldAsync(CharacterInfo character, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            return false;
        }

        _currentCharacter = character;
        await SendWorldPacketAsync(_worldConnection, WorldOpcode.CmsgPlayerLogin, WorldPacketCodec.BuildCharacterLoginPayload(character.Guid), cancellationToken);
        while (true)
        {
            var packet = await ReadWorldPacketAsync(_worldConnection, cancellationToken);
            switch (packet.Header.Opcode)
            {
                case WorldOpcode.SmsgLoginVerifyWorld:
                {
                    var location = WorldPacketCodec.ParseLoginVerifyWorld(packet.Payload);
                    _currentMapId = location.MapId;
                    _currentOrientation = location.Orientation;
                    _currentPlayer = new PlayerSnapshot(
                        character.Guid,
                        character.Name,
                        character.Level,
                        null,
                        location.X,
                        location.Y,
                        location.Z,
                        _currentOrientation,
                        100,
                        false,
                        false);
                    UpdateSnapshot(_currentPlayer, _currentTarget);
                    await SendWorldPacketAsync(_worldConnection, WorldOpcode.CmsgSetActiveMover, MovementPacketCodec.BuildActiveMoverPayload(character.Guid), cancellationToken);
                    StartKeepAliveLoop();
                    StartWorldReceiveLoop();
                    StartSnapshotProjectionLoop();
                    await SendWorldPacketAsync(
                        _worldConnection,
                        WorldOpcode.CmsgQuestgiverStatusMultipleQuery,
                        [],
                        cancellationToken);
                    Trace.WriteLine("NET WORLD QUEST STATUS_MULTIPLE_QUERY_SEND reason=enter-world");
                    return true;
                }
                case WorldOpcode.SmsgTimeSyncReq:
                    await RespondTimeSyncAsync(packet.Payload, cancellationToken);
                    break;
            }
        }
    }

    public async Task<bool> MoveToAsync(
        NavigationPoint destination,
        float arrivalRadius = 2.5f,
        int repathIntervalMs = 750,
        int stuckTimeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected || _currentCharacter is null || _currentPlayer is null)
        {
            return false;
        }

        var movementScope = await BeginMovementCommandAsync("MoveTo", cancelPrevious: true, cancellationToken);
        if (movementScope is null)
        {
            return false;
        }

        using var exclusiveMovement = movementScope;
        var movementToken = exclusiveMovement.CancellationToken;

        var arrivalRadiusSq = arrivalRadius * arrivalRadius;
        var finalDestination = destination;
        var lastRepathAt = Environment.TickCount64;
        var lastProgressAt = Environment.TickCount64;
        var lastDistanceSq = DistanceSquared(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z, finalDestination.X, finalDestination.Y, finalDestination.Z);

        if (_currentMapId < 0)
        {
            return false;
        }

        var startPoint = pathfinder.ProjectToSurface(_currentMapId, new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z));
        var endPoint = pathfinder.ProjectToSurface(_currentMapId, finalDestination);
        var path = pathfinder.FindPath(_currentMapId, startPoint, endPoint);
        Trace.WriteLine(
            $"NET WORLD MOVE INIT op={exclusiveMovement.OperationId} map={_currentMapId} " +
            $"startRaw=({_currentPlayer.X:F3},{_currentPlayer.Y:F3},{_currentPlayer.Z:F3}) startProj=({startPoint.X:F3},{startPoint.Y:F3},{startPoint.Z:F3}) " +
            $"endRaw=({finalDestination.X:F3},{finalDestination.Y:F3},{finalDestination.Z:F3}) endProj=({endPoint.X:F3},{endPoint.Y:F3},{endPoint.Z:F3}) pathPoints={path.Count}");

        if (path.Count == 0)
        {
            return true;
        }

        var waypointIndex = 0;
        var projectedWaypointIndex = -1;
        NavigationPoint? projectedWaypoint = null;
        var lastVisibilityCheckAt = 0L;
        var heartbeatCount = 0;
        long? arrivedSinceTick = null;
        var lastLoopTick = Environment.TickCount64;
        var nextHeartbeatTick = lastLoopTick + HeartbeatIntervalMs;
        if (!_isMoveForwardActive)
        {
            await SendMovementPacketAsync(
                WorldOpcode.MsgMoveStartForward,
                new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z),
                MovementPacketCodec.MovementFlags.Forward,
                movementToken);
        }

        while (!movementToken.IsCancellationRequested)
        {
            var snapshot = _currentPlayer;
            if (snapshot is null)
            {
                await Task.Delay(100, movementToken);
                continue;
            }

            var now = Environment.TickCount64;
            var tickSeconds = Math.Clamp((now - lastLoopTick) / 1000f, 0.09f, 0.35f);
            var currentDistanceSq = DistanceSquared(snapshot.X, snapshot.Y, snapshot.Z, finalDestination.X, finalDestination.Y, finalDestination.Z);
            if (currentDistanceSq <= arrivalRadiusSq)
            {
                arrivedSinceTick ??= now;
                if (now - arrivedSinceTick.Value >= 250)
                {
                    await SendStopSequenceAsync(finalDestination, movementToken);
                    return true;
                }
            }
            else
            {
                arrivedSinceTick = null;
            }

            if (currentDistanceSq < lastDistanceSq - 0.25f)
            {
                lastDistanceSq = currentDistanceSq;
                lastProgressAt = now;
            }
            else if (now - lastProgressAt >= stuckTimeoutMs)
            {
                Trace.WriteLine($"NET WORLD MOVE STUCK op={exclusiveMovement.OperationId} after={stuckTimeoutMs}ms");
                await SendStopSequenceAsync(new NavigationPoint(snapshot.X, snapshot.Y, snapshot.Z), movementToken);
                return false;
            }

            var adaptiveRepathIntervalMs = currentDistanceSq > (ChaseConvergenceDisableDistance * ChaseConvergenceDisableDistance)
                ? Math.Max(repathIntervalMs, ChaseRepathMinIntervalMs)
                : repathIntervalMs;
            if (path.Count > 2 && now - lastRepathAt >= adaptiveRepathIntervalMs)
            {
                var repathStart = pathfinder.ProjectToSurface(_currentMapId, new NavigationPoint(snapshot.X, snapshot.Y, snapshot.Z));
                var repathEnd = pathfinder.ProjectToSurface(_currentMapId, finalDestination);
                var repathPath = pathfinder.FindPath(_currentMapId, repathStart, repathEnd);
                if (repathPath.Count > 0)
                {
                    path = repathPath;
                    waypointIndex = FindClosestWaypointIndex(path, snapshot.X, snapshot.Y, snapshot.Z);
                    projectedWaypointIndex = -1;
                    lastVisibilityCheckAt = 0L;
                }
                lastRepathAt = now;
                Trace.WriteLine(
                    $"NET WORLD MOVE REPATH op={exclusiveMovement.OperationId} map={_currentMapId} " +
                    $"from=({snapshot.X:F3},{snapshot.Y:F3},{snapshot.Z:F3}) fromProj=({repathStart.X:F3},{repathStart.Y:F3},{repathStart.Z:F3}) " +
                    $"to=({finalDestination.X:F3},{finalDestination.Y:F3},{finalDestination.Z:F3}) toProj=({repathEnd.X:F3},{repathEnd.Y:F3},{repathEnd.Z:F3}) " +
                    $"points={path.Count} wpIndex={waypointIndex + 1}/{Math.Max(path.Count, 1)} interval={adaptiveRepathIntervalMs}ms");
            }

            if (path.Count == 0)
            {
                await SendStopSequenceAsync(new NavigationPoint(snapshot.X, snapshot.Y, snapshot.Z), movementToken);
                return true;
            }

            if (waypointIndex >= path.Count)
            {
                await SendStopSequenceAsync(finalDestination, movementToken);
                Trace.WriteLine($"NET WORLD MOVE ARRIVED op={exclusiveMovement.OperationId} reason=path-completed");
                return true;
            }

            var deathRecoveryMove = snapshot.IsGhost || snapshot.IsDeadHint;

            if (!deathRecoveryMove &&
                path.Count >= VisibilityLookAheadMinPathPoints &&
                now - lastVisibilityCheckAt >= VisibilityCheckIntervalMs)
            {
                var currentPoint = new NavigationPoint(snapshot.X, snapshot.Y, snapshot.Z);
                var lookAheadIndex = waypointIndex;
                if (CanAdvanceWaypointByVisibility(path, waypointIndex, path.Count - 1, currentPoint, out var endDistance2D, out var endDeviation2D, out var endDecision))
                {
                    lookAheadIndex = path.Count - 1;
                    Trace.WriteLine(
                        $"NET WORLD MOVE LOOKAHEAD op={exclusiveMovement.OperationId} mode=end " +
                        $"from={waypointIndex + 1}/{path.Count} to={lookAheadIndex + 1}/{path.Count} " +
                        $"decision={endDecision} dist2d={endDistance2D:F3} deviation2d={endDeviation2D:F3}");
                }
                else
                {
                    Trace.WriteLine(
                        $"NET WORLD MOVE LOOKAHEAD op={exclusiveMovement.OperationId} mode=end-blocked " +
                        $"from={waypointIndex + 1}/{path.Count} to={path.Count}/{path.Count} " +
                        $"decision={endDecision} dist2d={endDistance2D:F3} deviation2d={endDeviation2D:F3}");
                    var maxCandidate = Math.Min(path.Count - 1, waypointIndex + VisibilityLookAheadMaxPoints);
                    for (var candidate = maxCandidate; candidate > waypointIndex; candidate--)
                    {
                        if (!CanAdvanceWaypointByVisibility(path, waypointIndex, candidate, currentPoint, out var candidateDistance2D, out var candidateDeviation2D, out var candidateDecision))
                        {
                            continue;
                        }

                        lookAheadIndex = candidate;
                        Trace.WriteLine(
                            $"NET WORLD MOVE LOOKAHEAD op={exclusiveMovement.OperationId} mode=candidate " +
                            $"from={waypointIndex + 1}/{path.Count} to={lookAheadIndex + 1}/{path.Count} " +
                            $"decision={candidateDecision} dist2d={candidateDistance2D:F3} deviation2d={candidateDeviation2D:F3}");
                        break;
                    }
                }

                if (lookAheadIndex > waypointIndex)
                {
                    Trace.WriteLine(
                        $"NET WORLD MOVE LOOKAHEAD op={exclusiveMovement.OperationId} fromWp={waypointIndex + 1} toWp={lookAheadIndex + 1} totalWp={path.Count}");
                    waypointIndex = lookAheadIndex;
                    projectedWaypointIndex = -1;
                }

                lastVisibilityCheckAt = now;
            }

            var waypoint = path[Math.Min(waypointIndex, path.Count - 1)];
            var waypointDistanceSq = DistanceSquared(snapshot.X, snapshot.Y, snapshot.Z, waypoint.X, waypoint.Y, waypoint.Z);
            if (waypointDistanceSq <= 1.0f)
            {
                waypointIndex++;
                projectedWaypointIndex = -1;
                if (waypointIndex >= path.Count)
                {
                    await SendStopSequenceAsync(finalDestination, movementToken);
                    Trace.WriteLine($"NET WORLD MOVE ARRIVED op={exclusiveMovement.OperationId} reason=waypoints-completed");
                    return true;
                }

                waypoint = path[waypointIndex];
            }

            if (!_isMoveForwardActive && waypointDistanceSq > 0.04f)
            {
                await SendMovementPacketAsync(
                    WorldOpcode.MsgMoveStartForward,
                    new NavigationPoint(snapshot.X, snapshot.Y, snapshot.Z),
                    MovementPacketCodec.MovementFlags.Forward,
                    movementToken);
            }

            if (projectedWaypointIndex != waypointIndex)
            {
                projectedWaypoint = pathfinder.ProjectToSurface(_currentMapId, waypoint);
                projectedWaypointIndex = waypointIndex;
            }
            var safeProjectedWaypoint = projectedWaypoint ?? waypoint;
            // Approximation of Trinity UpdateAllowedPositionZ for ground movement:
            // keep XY movement interpolation, but keep Z on projected ground.
            var safeWaypoint = new NavigationPoint(safeProjectedWaypoint.X, safeProjectedWaypoint.Y, safeProjectedWaypoint.Z);
            var chaseMode = !deathRecoveryMove &&
                            currentDistanceSq > (ChaseConvergenceDisableDistance * ChaseConvergenceDisableDistance);
            _isChaseMode = chaseMode;
            var speedFactor = deathRecoveryMove
                ? 0.82f
                : (chaseMode ? ChaseStepSpeedMultiplier : 1.0f);
            var maxStepDistance = MathF.Max(
                deathRecoveryMove ? 0.10f : (chaseMode ? ChaseStepMinDistance : 0.25f),
                _currentRunSpeed * tickSeconds * speedFactor);
            if (deathRecoveryMove)
            {
                var ghostSafeCap = (_currentRunSpeed * tickSeconds * 0.95f) + 0.02f;
                maxStepDistance = MathF.Min(maxStepDistance, ghostSafeCap);
            }
            var steppedWaypoint = MoveTowards(
                new NavigationPoint(snapshot.X, snapshot.Y, snapshot.Z),
                safeWaypoint,
                maxStepDistance);
            // Re-project interpolated XY every heartbeat to keep Z aligned with local terrain.
            var (steppedGround, groundProbeSource, groundProbeZ, groundDeltaToStep, groundDeltaToPlayer, groundDeltaToTarget) =
                ResolveHeartbeatGroundPoint(
                    new NavigationPoint(snapshot.X, snapshot.Y, snapshot.Z),
                    steppedWaypoint,
                    safeWaypoint,
                    chaseMode,
                    deathRecoveryMove);
            var heartbeatPoint = new NavigationPoint(
                steppedWaypoint.X,
                steppedWaypoint.Y,
                steppedGround.Z);
            if (!string.Equals(groundProbeSource, "step-z", StringComparison.Ordinal) ||
                MathF.Abs(groundDeltaToStep) >= HeartbeatGroundRecoveryDeltaZ)
            {
                Trace.WriteLine(
                    $"NET WORLD MOVE GROUND_PROBE op={exclusiveMovement.OperationId} source={groundProbeSource} probeZ={groundProbeZ:F3} " +
                    $"deltaStep={groundDeltaToStep:F3} deltaPlayer={groundDeltaToPlayer:F3} deltaTarget={groundDeltaToTarget:F3} " +
                    $"player=({snapshot.X:F3},{snapshot.Y:F3},{snapshot.Z:F3}) step=({steppedWaypoint.X:F3},{steppedWaypoint.Y:F3},{steppedWaypoint.Z:F3}) " +
                    $"target=({safeWaypoint.X:F3},{safeWaypoint.Y:F3},{safeWaypoint.Z:F3}) ground=({steppedGround.X:F3},{steppedGround.Y:F3},{steppedGround.Z:F3})");
            }
            var heartbeatBeforeDriftComp = heartbeatPoint;
            if (!deathRecoveryMove)
            {
                heartbeatPoint = ApplyMovingDriftCompensation(heartbeatPoint);
            }
            var driftCompApplied = MathF.Sqrt(DistanceSquared(
                heartbeatBeforeDriftComp.X,
                heartbeatBeforeDriftComp.Y,
                heartbeatBeforeDriftComp.Z,
                heartbeatPoint.X,
                heartbeatPoint.Y,
                heartbeatPoint.Z));
            var hbMove2d = MathF.Sqrt(DistanceSquared(snapshot.X, snapshot.Y, 0f, heartbeatPoint.X, heartbeatPoint.Y, 0f));
            var heartbeatFlags = _isMoveForwardActive
                ? MovementPacketCodec.MovementFlags.Forward
                : MovementPacketCodec.MovementFlags.None;
            heartbeatCount++;
            var nearestPlayerLog = BuildNearestPlayerLog(snapshot.X, snapshot.Y, snapshot.Z);
            var selfDriftLog = BuildSelfDriftLog(snapshot.X, snapshot.Y, snapshot.Z);
            Trace.WriteLine(
                $"NET WORLD MOVE HB#{heartbeatCount} op={exclusiveMovement.OperationId} wp={waypointIndex + 1}/{path.Count} " +
                $"player=({snapshot.X:F3},{snapshot.Y:F3},{snapshot.Z:F3}) " +
                $"wpRaw=({waypoint.X:F3},{waypoint.Y:F3},{waypoint.Z:F3}) " +
                $"wpProj=({safeProjectedWaypoint.X:F3},{safeProjectedWaypoint.Y:F3},{safeProjectedWaypoint.Z:F3}) " +
                $"wpSafe=({safeWaypoint.X:F3},{safeWaypoint.Y:F3},{safeWaypoint.Z:F3}) " +
                $"wpStep=({steppedWaypoint.X:F3},{steppedWaypoint.Y:F3},{steppedWaypoint.Z:F3}) " +
                $"hbGround=({steppedGround.X:F3},{steppedGround.Y:F3},{steppedGround.Z:F3}) hbGroundSource={groundProbeSource} hbGroundProbeZ={groundProbeZ:F3} " +
                $"hbGroundDeltaStep={groundDeltaToStep:F3} hbGroundDeltaPlayer={groundDeltaToPlayer:F3} hbGroundDeltaTarget={groundDeltaToTarget:F3} " +
                $"hbPos=({heartbeatPoint.X:F3},{heartbeatPoint.Y:F3},{heartbeatPoint.Z:F3}) hbMove2d={hbMove2d:F3} hbDriftComp={driftCompApplied:F3} hbFlags={heartbeatFlags} stepMax={maxStepDistance:F3} tick={tickSeconds:F3}s chaseMode={chaseMode} deathMove={deathRecoveryMove} " +
                $"{nearestPlayerLog} {selfDriftLog}");

            await SendMovementPacketAsync(WorldOpcode.MsgMoveHeartbeat, heartbeatPoint, heartbeatFlags, movementToken);
            lastLoopTick = now;

            var nowAfterSend = Environment.TickCount64;
            if (nextHeartbeatTick <= nowAfterSend)
            {
                var behind = ((nowAfterSend - nextHeartbeatTick) / HeartbeatIntervalMs) + 1;
                nextHeartbeatTick += behind * HeartbeatIntervalMs;
            }

            var delayMs = (int)(nextHeartbeatTick - nowAfterSend);
            if (delayMs > 0)
            {
                await Task.Delay(delayMs, movementToken);
            }
            nextHeartbeatTick += HeartbeatIntervalMs;
        }

        if (_currentPlayer is not null && IsMovementCommandCurrent(exclusiveMovement.OperationId))
        {
            try
            {
                await SendStopSequenceAsync(
                    new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z),
                    CancellationToken.None);
            }
            catch
            {
                // best effort stop
            }
        }

        Trace.WriteLine(
            $"NET WORLD MOVE CANCELLED op={exclusiveMovement.OperationId} current={IsMovementCommandCurrent(exclusiveMovement.OperationId)}");

        return false;
    }

    public async Task<bool> StopMovementAsync(CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected || _currentPlayer is null)
        {
            return false;
        }

        var movementScope = await BeginMovementCommandAsync("StopMovement", cancelPrevious: true, cancellationToken);
        if (movementScope is null)
        {
            return false;
        }

        using var exclusiveMovement = movementScope;
        await SendStopSequenceAsync(new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z), exclusiveMovement.CancellationToken);
        return true;
    }

    public async Task<bool> FaceTowardsAsync(NavigationPoint destination, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected || _currentPlayer is null)
        {
            return false;
        }

        var dx = destination.X - _currentPlayer.X;
        var dy = destination.Y - _currentPlayer.Y;
        if (MathF.Abs(dx) <= 0.001f && MathF.Abs(dy) <= 0.001f)
        {
            return false;
        }

        var forcedOrientation = MathF.Atan2(dy, dx);
        var movementScope = await BeginMovementCommandAsync("FaceTowards", cancelPrevious: true, cancellationToken);
        if (movementScope is null)
        {
            return false;
        }

        using var exclusiveMovement = movementScope;
        await SendMovementPacketAsync(
            WorldOpcode.MsgMoveHeartbeat,
            new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z),
            MovementPacketCodec.MovementFlags.None,
            exclusiveMovement.CancellationToken,
            forcedOrientation);
        return true;
    }

    private async Task SendStopSequenceAsync(NavigationPoint point, CancellationToken cancellationToken)
    {
        if (!_isMoveForwardActive)
        {
            // Do not skip STOP when local state says idle: local/server movement flags can drift
            // after authority resets, and this hard stop clears "run on place" loops server-side.
            Trace.WriteLine($"NET WORLD MOVE STOP_SEQUENCE {BuildMovementCommandLog()} force-send reason=local-inactive");
        }

        // Trinity movement state is more stable when a neutral heartbeat follows STOP.
        Trace.WriteLine(
            $"NET WORLD MOVE STOP_SEQUENCE {BuildMovementCommandLog()} point=({point.X:F3},{point.Y:F3},{point.Z:F3})");
        await SendMovementPacketAsync(
            WorldOpcode.MsgMoveStop,
            point,
            MovementPacketCodec.MovementFlags.None,
            cancellationToken);

        await SendMovementPacketAsync(
            WorldOpcode.MsgMoveHeartbeat,
            point,
            MovementPacketCodec.MovementFlags.None,
            cancellationToken);
    }

    public async Task<bool> SelectTargetAsync(ulong targetGuid, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine("NET WORLD SELECT TARGET failed: world not connected");
            return false;
        }

        if (targetGuid == 0)
        {
            // After enter world, nearby entities can arrive a bit later via update packets.
            for (var i = 0; i < 20; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lock (_entitiesSync)
                {
                    if (_entities.Count > 0)
                    {
                        break;
                    }
                }

                await Task.Delay(100, cancellationToken);
            }
        }

        ulong selectedGuid;
        int knownEntities;
        int aliveCandidates;
        int leveledCandidates;
        var playerLevel = _currentPlayer?.Level ?? int.MaxValue;
        var playerX = _currentPlayer?.X ?? 0f;
        var playerY = _currentPlayer?.Y ?? 0f;
        var playerZ = _currentPlayer?.Z ?? 0f;
        lock (_entitiesSync)
        {
            knownEntities = _entities.Count;
            aliveCandidates = _entities.Count(x => x.Value.MaxHealth > 0);
            leveledCandidates = _entities.Count(x => x.Value.Level.HasValue);
            selectedGuid = targetGuid == 0
                ? _entities
                    .Where(x => x.Key != _currentCharacter?.Guid)
                    .Where(x => x.Value.IsCreature)
                    .Where(x => !x.Value.Level.HasValue || x.Value.Level.Value <= playerLevel)
                    .OrderByDescending(x => x.Value.HasPosition)
                    .ThenBy(x => x.Value.DistanceSquaredTo(playerX, playerY, playerZ))
                    .ThenBy(x => x.Key)
                    .Select(x => x.Key)
                    .FirstOrDefault()
                : targetGuid;

            if (selectedGuid == 0)
            {
                Trace.WriteLine(
                    $"NET WORLD SELECT TARGET no candidate known={knownEntities} alive={aliveCandidates} leveled={leveledCandidates}");
                return false;
            }
        }

        await SendWorldPacketAsync(
            _worldConnection,
            WorldOpcode.CmsgSetSelection,
            WorldPacketCodec.BuildSetSelectionPayload(selectedGuid),
            cancellationToken);

        lock (_entitiesSync)
        {
            _entities.TryGetValue(selectedGuid, out var targetState);
            _currentTarget = new TargetSnapshot(
                selectedGuid,
                null,
                targetState?.Level,
                null,
                null,
                null,
                targetState?.HealthPercent);
        }

        Trace.WriteLine($"NET WORLD SELECT TARGET sent guid={selectedGuid}");
        UpdateSnapshot(_currentPlayer, _currentTarget);
        return true;
    }

    public async Task<bool> StartMeleeAttackAsync(ulong targetGuid, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine("NET WORLD COMBAT ATTACK_START failed: world not connected");
            return false;
        }

        if (targetGuid == 0)
        {
            targetGuid = _currentTarget?.Guid ?? 0;
            if (targetGuid == 0)
            {
                Trace.WriteLine("NET WORLD COMBAT ATTACK_START failed: no target");
                return false;
            }
        }

        if (_isAutoAttackActive && _autoAttackTargetGuid == targetGuid)
        {
            return true;
        }

        if (_isAutoAttackActive && _autoAttackTargetGuid != 0 && _autoAttackTargetGuid != targetGuid)
        {
            await SendWorldPacketAsync(
                _worldConnection,
                WorldOpcode.CmsgAttackStop,
                WorldPacketCodec.BuildAttackStopPayload(),
                cancellationToken);
        }

        await SendWorldPacketAsync(
            _worldConnection,
            WorldOpcode.CmsgAttackSwing,
            WorldPacketCodec.BuildAttackSwingPayload(targetGuid),
            cancellationToken);

        _isAutoAttackActive = true;
        _autoAttackTargetGuid = targetGuid;
        Trace.WriteLine($"NET WORLD COMBAT ATTACK_START sent target={targetGuid}");
        return true;
    }

    public async Task<bool> StopMeleeAttackAsync(CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            return false;
        }

        if (!_isAutoAttackActive && _autoAttackTargetGuid == 0)
        {
            return true;
        }

        await SendWorldPacketAsync(
            _worldConnection,
            WorldOpcode.CmsgAttackStop,
            WorldPacketCodec.BuildAttackStopPayload(),
            cancellationToken);

        _isAutoAttackActive = false;
        _autoAttackTargetGuid = 0;
        Trace.WriteLine("NET WORLD COMBAT ATTACK_STOP sent");
        return true;
    }

    public async Task<bool> AcceptPendingGroupInviteAsync(CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            return false;
        }

        GroupInviteInfo? invite;
        lock (_socialStateSync)
        {
            invite = _pendingGroupInvite;
        }

        if (invite is null)
        {
            Trace.WriteLine("NET WORLD GROUP ACCEPT skip reason=no_pending_invite");
            return false;
        }

        await SendWorldPacketAsync(
            _worldConnection,
            WorldOpcode.CmsgGroupAccept,
            SocialPacketCodec.BuildGroupAcceptPayload(invite.ProposedRoles),
            cancellationToken);
        Trace.WriteLine(
            $"NET WORLD GROUP ACCEPT sent inviter=\"{invite.InviterName}\" proposedRoles={invite.ProposedRoles}");
        return true;
    }

    public async Task<bool> DeclinePendingGroupInviteAsync(CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            return false;
        }

        GroupInviteInfo? invite;
        lock (_socialStateSync)
        {
            invite = _pendingGroupInvite;
        }

        if (invite is null)
        {
            Trace.WriteLine("NET WORLD GROUP DECLINE skip reason=no_pending_invite");
            return false;
        }

        await SendWorldPacketAsync(
            _worldConnection,
            WorldOpcode.CmsgGroupDecline,
            SocialPacketCodec.BuildGroupDeclinePayload(),
            cancellationToken);
        Trace.WriteLine($"NET WORLD GROUP DECLINE sent inviter=\"{invite.InviterName}\"");
        lock (_socialStateSync)
        {
            _pendingGroupInvite = null;
        }

        return true;
    }

    public async Task<bool> SendChatMessageAsync(
        ChatChannel channel,
        string message,
        string? whisperTarget = null,
        string? channelName = null,
        ChatLanguage language = ChatLanguage.Auto,
        CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected || _currentCharacter is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        if (channel == ChatChannel.Whisper && string.IsNullOrWhiteSpace(whisperTarget))
        {
            Trace.WriteLine("NET WORLD CHAT SEND skip channel=Whisper reason=missing_target");
            return false;
        }

        if (channel == ChatChannel.Channel && string.IsNullOrWhiteSpace(channelName))
        {
            Trace.WriteLine("NET WORLD CHAT SEND skip channel=Channel reason=missing_channel_name");
            return false;
        }

        var resolvedLanguage = ResolveChatLanguage(language, _currentCharacter.RaceId);
        var trimmedMessage = message.Trim();
        await SendWorldPacketAsync(
            _worldConnection,
            WorldOpcode.CmsgMessagechat,
            SocialPacketCodec.BuildChatMessagePayload(channel, resolvedLanguage, trimmedMessage, whisperTarget, channelName),
            cancellationToken);
        Trace.WriteLine(
            $"NET WORLD CHAT SEND channel={channel} language={resolvedLanguage} whisperTarget=\"{whisperTarget ?? string.Empty}\" channelName=\"{channelName ?? string.Empty}\" message=\"{trimmedMessage}\"");
        return true;
    }

    public async Task<bool> SendEmoteAsync(uint emoteId, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected || emoteId == 0)
        {
            return false;
        }

        await SendWorldPacketAsync(
            _worldConnection,
            WorldOpcode.CmsgEmote,
            SocialPacketCodec.BuildEmotePayload(emoteId),
            cancellationToken);
        Trace.WriteLine($"NET WORLD EMOTE SEND emoteId={emoteId}");
        return true;
    }

    public async Task<bool> SendTextEmoteAsync(
        uint textEmoteId,
        ulong targetGuid = 0,
        uint emoteNum = 0,
        CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected || textEmoteId == 0)
        {
            return false;
        }

        await SendWorldPacketAsync(
            _worldConnection,
            WorldOpcode.CmsgTextEmote,
            SocialPacketCodec.BuildTextEmotePayload(textEmoteId, emoteNum, targetGuid),
            cancellationToken);
        Trace.WriteLine($"NET WORLD TEXT_EMOTE SEND textEmoteId={textEmoteId} emoteNum={emoteNum} targetGuid={targetGuid}");
        return true;
    }

    public async Task<QuestDefinition?> QueryQuestDefinitionAsync(uint questId, CancellationToken cancellationToken = default)
    {
        if (questId == 0)
        {
            return null;
        }

        lock (_questStateSync)
        {
            if (_questDefinitions.TryGetValue(questId, out var cached))
            {
                return cached;
            }
        }

        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine($"NET WORLD QUEST QUERY_INFO skip questId={questId} reason=world_not_connected");
            return null;
        }

        await _questRequestLock.WaitAsync(cancellationToken);
        try
        {
            ResetQuestAwaiters();
            _questDefinitionTcs = NewTcs<QuestDefinition>();

            Trace.WriteLine($"NET WORLD QUEST QUERY_INFO_SEND questId={questId}");
            await SendWorldPacketAsync(
                _worldConnection,
                WorldOpcode.CmsgQuestQuery,
                QuestPacketCodec.BuildQuestQueryPayload(questId),
                cancellationToken);

            using var timeoutCts = CreateTimeoutToken(cancellationToken);
            var definition = await WaitAsync(_questDefinitionTcs.Task, timeoutCts.Token);
            CacheQuestDefinition(definition);
            Trace.WriteLine(
                $"NET WORLD QUEST QUERY_INFO_RECV questId={questId} title=\"{definition.Title}\" objectives={definition.Objectives.Count} itemObjectives={definition.ItemObjectives.Count}");
            UpdateSnapshot(_currentPlayer, _currentTarget);
            return definition;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Trace.WriteLine($"NET WORLD QUEST QUERY_INFO_TIMEOUT questId={questId}");
            return null;
        }
        finally
        {
            ResetQuestAwaiters();
            _questRequestLock.Release();
        }
    }

    public async Task<IReadOnlyList<QuestPoiInfo>> QueryQuestPoiAsync(IReadOnlyList<uint> questIds, CancellationToken cancellationToken = default)
    {
        var safeQuestIds = questIds
            .Where(x => x != 0)
            .Distinct()
            .ToArray();
        if (safeQuestIds.Length == 0)
        {
            return [];
        }

        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine($"NET WORLD QUEST POI_QUERY skip count={safeQuestIds.Length} reason=world_not_connected");
            return [];
        }

        await _questRequestLock.WaitAsync(cancellationToken);
        try
        {
            ResetQuestAwaiters();
            _questPoiTcs = NewTcs<IReadOnlyList<QuestPoiInfo>>();

            Trace.WriteLine(
                $"NET WORLD QUEST POI_QUERY_SEND count={safeQuestIds.Length} questIds={string.Join(",", safeQuestIds)}");
            await SendWorldPacketAsync(
                _worldConnection,
                WorldOpcode.CmsgQuestPoiQuery,
                QuestPacketCodec.BuildQuestPoiQueryPayload(safeQuestIds),
                cancellationToken);

            using var timeoutCts = CreateTimeoutToken(cancellationToken);
            var pois = await WaitAsync(_questPoiTcs.Task, timeoutCts.Token);
            CacheQuestPois(pois);
            Trace.WriteLine(
                $"NET WORLD QUEST POI_QUERY_RECV count={pois.Count} questIds={string.Join(",", pois.Select(x => x.QuestId))}");
            UpdateSnapshot(_currentPlayer, _currentTarget);
            return pois;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Trace.WriteLine($"NET WORLD QUEST POI_QUERY_TIMEOUT count={safeQuestIds.Length}");
            return [];
        }
        finally
        {
            ResetQuestAwaiters();
            _questRequestLock.Release();
        }
    }

    public async Task<QuestGiverStatusInfo?> QueryQuestGiverStatusAsync(ulong questGiverGuid, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine($"NET WORLD QUEST STATUS_QUERY skip questGiver={questGiverGuid} reason=world_not_connected");
            return null;
        }

        await _questRequestLock.WaitAsync(cancellationToken);
        try
        {
            ResetQuestAwaiters();
            _questStatusTcs = NewTcs<QuestGiverStatusInfo>();

            Trace.WriteLine($"NET WORLD QUEST STATUS_QUERY_SEND questGiver={questGiverGuid} {BuildQuestGiverDebugLog(questGiverGuid)}");
            await SendWorldPacketAsync(
                _worldConnection,
                WorldOpcode.CmsgQuestgiverStatusQuery,
                QuestPacketCodec.BuildQuestGiverStatusQueryPayload(questGiverGuid),
                cancellationToken);

            using var timeoutCts = CreateTimeoutToken(cancellationToken);
            var status = await WaitAsync(_questStatusTcs.Task, timeoutCts.Token);
            Trace.WriteLine(
                $"NET WORLD QUEST STATUS_QUERY_RECV questGiver={questGiverGuid} status={status.Status} {BuildQuestGiverDebugLog(questGiverGuid)}");
            return status;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Trace.WriteLine($"NET WORLD QUEST STATUS_QUERY_TIMEOUT questGiver={questGiverGuid}");
            return null;
        }
        finally
        {
            ResetQuestAwaiters();
            _questRequestLock.Release();
        }
    }

    public async Task<QuestGiverMenu?> OpenQuestGiverAsync(ulong questGiverGuid, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine($"NET WORLD QUEST HELLO skip questGiver={questGiverGuid} reason=world_not_connected");
            return null;
        }

        await _questRequestLock.WaitAsync(cancellationToken);
        try
        {
            ResetQuestAwaiters();
            _questMenuTcs = NewTcs<QuestGiverMenu>();
            _questDialogTcs = NewTcs<QuestDialog>();

            Trace.WriteLine($"NET WORLD QUEST HELLO_SEND questGiver={questGiverGuid} {BuildQuestGiverDebugLog(questGiverGuid)}");
            await SendWorldPacketAsync(
                _worldConnection,
                WorldOpcode.CmsgQuestgiverHello,
                QuestPacketCodec.BuildQuestGiverHelloPayload(questGiverGuid),
                cancellationToken);

            using var timeoutCts = CreateTimeoutToken(cancellationToken);
            var completed = await Task.WhenAny(
                _questMenuTcs.Task,
                _questDialogTcs.Task,
                Task.Delay(Timeout.Infinite, timeoutCts.Token));
            if (completed == _questMenuTcs.Task)
            {
                var menu = await _questMenuTcs.Task;
                Trace.WriteLine(
                    $"NET WORLD QUEST HELLO_MENU questGiver={questGiverGuid} items={menu.Items.Count} greetingLen={menu.Greeting.Length}");
                return menu;
            }

            if (completed == _questDialogTcs.Task)
            {
                var dialog = await _questDialogTcs.Task;
                Trace.WriteLine(
                    $"NET WORLD QUEST HELLO_DIRECT_DIALOG questGiver={questGiverGuid} questId={dialog.QuestId} kind={dialog.Kind} title=\"{dialog.Title}\"");
                return new QuestGiverMenu(
                    questGiverGuid,
                    dialog.Text,
                    0,
                    0,
                    [new QuestGiverMenuItem(dialog.QuestId, 0, 0, dialog.Flags, false, dialog.Title)]);
            }

            Trace.WriteLine($"NET WORLD QUEST HELLO_TIMEOUT questGiver={questGiverGuid}");
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Trace.WriteLine($"NET WORLD QUEST HELLO_TIMEOUT questGiver={questGiverGuid}");
            return null;
        }
        finally
        {
            ResetQuestAwaiters();
            _questRequestLock.Release();
        }
    }

    public async Task<QuestDialog?> QueryQuestAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine($"NET WORLD QUEST QUERY skip questGiver={questGiverGuid} questId={questId} reason=world_not_connected");
            return null;
        }

        await _questRequestLock.WaitAsync(cancellationToken);
        try
        {
            ResetQuestAwaiters();
            _questDialogTcs = NewTcs<QuestDialog>();
            _questInvalidTcs = NewTcs<byte>();

            Trace.WriteLine(
                $"NET WORLD QUEST QUERY_SEND questGiver={questGiverGuid} questId={questId} {BuildQuestGiverDebugLog(questGiverGuid)}");
            await SendWorldPacketAsync(
                _worldConnection,
                WorldOpcode.CmsgQuestgiverQueryQuest,
                QuestPacketCodec.BuildQuestGiverQueryQuestPayload(questGiverGuid, questId),
                cancellationToken);

            return await WaitForQuestDialogAsync("QUERY", questGiverGuid, questId, cancellationToken);
        }
        finally
        {
            ResetQuestAwaiters();
            _questRequestLock.Release();
        }
    }

    public async Task<bool> AcceptQuestAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine($"NET WORLD QUEST ACCEPT skip questGiver={questGiverGuid} questId={questId} reason=world_not_connected");
            return false;
        }

        var accepted = false;
        await _questRequestLock.WaitAsync(cancellationToken);
        try
        {
            ResetQuestAwaiters();
            _questInvalidTcs = NewTcs<byte>();

            Trace.WriteLine(
                $"NET WORLD QUEST ACCEPT_SEND questGiver={questGiverGuid} questId={questId} {BuildQuestGiverDebugLog(questGiverGuid)}");
            await SendWorldPacketAsync(
                _worldConnection,
                WorldOpcode.CmsgQuestgiverAcceptQuest,
                QuestPacketCodec.BuildQuestGiverAcceptQuestPayload(questGiverGuid, questId),
                cancellationToken);

            using var timeoutCts = CreateTimeoutToken(cancellationToken, 750);
            var completed = await Task.WhenAny(
                _questInvalidTcs.Task,
                Task.Delay(Timeout.Infinite, timeoutCts.Token));
            if (completed == _questInvalidTcs.Task)
            {
                var reason = await _questInvalidTcs.Task;
                Trace.WriteLine($"NET WORLD QUEST ACCEPT_REJECTED questGiver={questGiverGuid} questId={questId} reason={reason}");
                return false;
            }

            Trace.WriteLine($"NET WORLD QUEST ACCEPT_OK questGiver={questGiverGuid} questId={questId}");
            accepted = true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Trace.WriteLine($"NET WORLD QUEST ACCEPT_OK questGiver={questGiverGuid} questId={questId} mode=no_immediate_reject");
            accepted = true;
        }
        finally
        {
            ResetQuestAwaiters();
            _questRequestLock.Release();
        }

        if (!accepted)
        {
            return false;
        }

        RegisterAcceptedQuest(questId);
        await WarmQuestKnowledgeAsync(questId, cancellationToken);
        UpdateSnapshot(_currentPlayer, _currentTarget);
        return true;
    }

    public async Task<QuestDialog?> CompleteQuestAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine($"NET WORLD QUEST COMPLETE skip questGiver={questGiverGuid} questId={questId} reason=world_not_connected");
            return null;
        }

        await _questRequestLock.WaitAsync(cancellationToken);
        try
        {
            ResetQuestAwaiters();
            _questDialogTcs = NewTcs<QuestDialog>();
            _questInvalidTcs = NewTcs<byte>();
            _questTurnInResultTcs = NewTcs<QuestTurnInResult>();

            Trace.WriteLine(
                $"NET WORLD QUEST COMPLETE_SEND questGiver={questGiverGuid} questId={questId} {BuildQuestGiverDebugLog(questGiverGuid)}");
            await SendWorldPacketAsync(
                _worldConnection,
                WorldOpcode.CmsgQuestgiverCompleteQuest,
                QuestPacketCodec.BuildQuestGiverCompleteQuestPayload(questGiverGuid, questId),
                cancellationToken);

            using var timeoutCts = CreateTimeoutToken(cancellationToken);
            var completed = await Task.WhenAny(
                _questDialogTcs.Task,
                _questInvalidTcs.Task,
                _questTurnInResultTcs.Task,
                Task.Delay(Timeout.Infinite, timeoutCts.Token));
            if (completed == _questDialogTcs.Task)
            {
                var dialog = await _questDialogTcs.Task;
                Trace.WriteLine(
                    $"NET WORLD QUEST COMPLETE_DIALOG questGiver={questGiverGuid} questId={questId} kind={dialog.Kind} canComplete={dialog.CanComplete}");
                return dialog;
            }

            if (completed == _questInvalidTcs.Task)
            {
                var reason = await _questInvalidTcs.Task;
                Trace.WriteLine($"NET WORLD QUEST COMPLETE_REJECTED questGiver={questGiverGuid} questId={questId} reason={reason}");
                return null;
            }

            if (completed == _questTurnInResultTcs.Task)
            {
                var turnIn = await _questTurnInResultTcs.Task;
                Trace.WriteLine(
                    $"NET WORLD QUEST COMPLETE_RESULT questGiver={questGiverGuid} questId={turnIn.QuestId} xp={turnIn.RewardXp} money={turnIn.RewardMoney}");
                RemoveActiveQuest(turnIn.QuestId);
                UpdateSnapshot(_currentPlayer, _currentTarget);
                return new QuestDialog(
                    QuestDialogKind.OfferReward,
                    questGiverGuid,
                    turnIn.QuestId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    false,
                    0,
                    0,
                    true,
                    false,
                    0,
                    turnIn.RewardMoney,
                    turnIn.RewardXp,
                    [],
                    [],
                    []);
            }

            Trace.WriteLine($"NET WORLD QUEST COMPLETE_TIMEOUT questGiver={questGiverGuid} questId={questId}");
            return null;
        }
        finally
        {
            ResetQuestAwaiters();
            _questRequestLock.Release();
        }
    }

    public async Task<QuestDialog?> RequestQuestRewardAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine($"NET WORLD QUEST REWARD_REQUEST skip questGiver={questGiverGuid} questId={questId} reason=world_not_connected");
            return null;
        }

        await _questRequestLock.WaitAsync(cancellationToken);
        try
        {
            ResetQuestAwaiters();
            _questDialogTcs = NewTcs<QuestDialog>();
            _questInvalidTcs = NewTcs<byte>();

            Trace.WriteLine(
                $"NET WORLD QUEST REWARD_REQUEST_SEND questGiver={questGiverGuid} questId={questId} {BuildQuestGiverDebugLog(questGiverGuid)}");
            await SendWorldPacketAsync(
                _worldConnection,
                WorldOpcode.CmsgQuestgiverRequestReward,
                QuestPacketCodec.BuildQuestGiverRequestRewardPayload(questGiverGuid, questId),
                cancellationToken);

            return await WaitForQuestDialogAsync("REWARD_REQUEST", questGiverGuid, questId, cancellationToken);
        }
        finally
        {
            ResetQuestAwaiters();
            _questRequestLock.Release();
        }
    }

    public async Task<bool> ChooseQuestRewardAsync(
        ulong questGiverGuid,
        uint questId,
        uint rewardIndex = 0,
        CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine($"NET WORLD QUEST REWARD_CHOOSE skip questGiver={questGiverGuid} questId={questId} reason=world_not_connected");
            return false;
        }

        await _questRequestLock.WaitAsync(cancellationToken);
        try
        {
            ResetQuestAwaiters();
            _questInvalidTcs = NewTcs<byte>();
            _questTurnInResultTcs = NewTcs<QuestTurnInResult>();

            Trace.WriteLine(
                $"NET WORLD QUEST REWARD_CHOOSE_SEND questGiver={questGiverGuid} questId={questId} rewardIndex={rewardIndex} {BuildQuestGiverDebugLog(questGiverGuid)}");
            await SendWorldPacketAsync(
                _worldConnection,
                WorldOpcode.CmsgQuestgiverChooseReward,
                QuestPacketCodec.BuildQuestGiverChooseRewardPayload(questGiverGuid, questId, rewardIndex),
                cancellationToken);

            using var timeoutCts = CreateTimeoutToken(cancellationToken);
            var completed = await Task.WhenAny(
                _questTurnInResultTcs.Task,
                _questInvalidTcs.Task,
                Task.Delay(Timeout.Infinite, timeoutCts.Token));
            if (completed == _questTurnInResultTcs.Task)
            {
                var turnIn = await _questTurnInResultTcs.Task;
                Trace.WriteLine(
                    $"NET WORLD QUEST REWARD_CHOOSE_OK questGiver={questGiverGuid} questId={turnIn.QuestId} rewardIndex={rewardIndex} xp={turnIn.RewardXp} money={turnIn.RewardMoney}");
                RemoveActiveQuest(turnIn.QuestId);
                UpdateSnapshot(_currentPlayer, _currentTarget);
                return true;
            }

            if (completed == _questInvalidTcs.Task)
            {
                var reason = await _questInvalidTcs.Task;
                Trace.WriteLine(
                    $"NET WORLD QUEST REWARD_CHOOSE_REJECTED questGiver={questGiverGuid} questId={questId} rewardIndex={rewardIndex} reason={reason}");
                return false;
            }

            Trace.WriteLine(
                $"NET WORLD QUEST REWARD_CHOOSE_TIMEOUT questGiver={questGiverGuid} questId={questId} rewardIndex={rewardIndex}");
            return false;
        }
        finally
        {
            ResetQuestAwaiters();
            _questRequestLock.Release();
        }
    }

    public async Task<bool> RequestRepopAsync(CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine("NET WORLD DEATH REPOP failed: world not connected");
            return false;
        }

        await SendWorldPacketAsync(
            _worldConnection,
            WorldOpcode.CmsgRepopRequest,
            WorldPacketCodec.BuildRepopRequestPayload(checkInstance: false),
            cancellationToken);
        Trace.WriteLine("NET WORLD DEATH REPOP sent");
        return true;
    }

    public async Task<bool> ReclaimCorpseAsync(ulong corpseGuid = 0, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            Trace.WriteLine("NET WORLD DEATH RECLAIM failed: world not connected");
            return false;
        }

        var nowTick = Environment.TickCount64;
        if (nowTick < _corpseReclaimBlockedUntilTick)
        {
            var remainingMs = _corpseReclaimBlockedUntilTick - nowTick;
            Trace.WriteLine(
                $"NET WORLD DEATH RECLAIM blocked reason=server_delay remainingMs={remainingMs} " +
                $"blockedUntilTick={_corpseReclaimBlockedUntilTick} nowTick={nowTick} corpseGuid={corpseGuid}");
            return false;
        }

        await SendWorldPacketAsync(
            _worldConnection,
            WorldOpcode.CmsgReclaimCorpse,
            WorldPacketCodec.BuildReclaimCorpsePayload(corpseGuid),
            cancellationToken);
        Trace.WriteLine(
            $"NET WORLD DEATH RECLAIM sent corpseGuid={corpseGuid} " +
            $"blockedUntilTick={_corpseReclaimBlockedUntilTick} nowTick={nowTick}");
        return true;
    }

    public async Task<bool> LogoutAsync(int timeoutSeconds = 25, CancellationToken cancellationToken = default)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            return false;
        }

        StopKeepAliveLoop();
        StartWorldReceiveLoop();
        await SendWorldPacketAsync(_worldConnection, WorldOpcode.CmsgLogoutRequest, [], cancellationToken);

        var timeoutMs = Math.Max(1, timeoutSeconds) * 1000;
        using var timeoutCts = CreateTimeoutToken(cancellationToken, timeoutMs);
        _logoutResponseTcs = NewTcs<LogoutResponseData>();
        _logoutCompleteTcs = NewTcs<bool>();
        _logoutCancelAckTcs = NewTcs<bool>();
        try
        {
            var response = await WaitAsync(_logoutResponseTcs.Task, timeoutCts.Token);
            Trace.WriteLine($"NET WORLD LOGOUT RESPONSE result={response.ResultCode} instant={response.Instant}");
            if (response.ResultCode != 0)
            {
                await SendWorldPacketAsync(_worldConnection, WorldOpcode.CmsgLogoutCancel, WorldPacketCodec.BuildLogoutCancelPayload(), timeoutCts.Token);
                _ = await WaitAsync(_logoutCancelAckTcs.Task, timeoutCts.Token);
                Trace.WriteLine("NET WORLD LOGOUT CANCEL ACK");
                StartKeepAliveLoop();
                return false;
            }

            if (response.Instant)
            {
                UpdateSnapshot(null, null);
                _currentCharacter = null;
                _currentPlayer = null;
                _currentTarget = null;
                _currentMapId = -1;
                _currentOrientation = 0;
                _currentRunSpeed = 7.0f;
                _isAutoAttackActive = false;
                _autoAttackTargetGuid = 0;
                lock (_entitiesSync)
                {
                    _entities.Clear();
                }
                ClearQuestState();
                ClearSocialState();
                await _worldConnection.DisconnectAsync(cancellationToken);
                StopWorldReceiveLoop();
                StopSnapshotProjectionLoop();
                return true;
            }

            _ = await WaitAsync(_logoutCompleteTcs.Task, timeoutCts.Token);
            UpdateSnapshot(null, null);
            _currentCharacter = null;
            _currentPlayer = null;
            _currentTarget = null;
            _currentMapId = -1;
            _currentOrientation = 0;
            _currentRunSpeed = 7.0f;
            _isAutoAttackActive = false;
            _autoAttackTargetGuid = 0;
            lock (_entitiesSync)
            {
                _entities.Clear();
            }
            ClearQuestState();
            ClearSocialState();
            await _worldConnection.DisconnectAsync(cancellationToken);
            StopWorldReceiveLoop();
            StopSnapshotProjectionLoop();
            return true;
        }
        finally
        {
            _logoutResponseTcs = null;
            _logoutCompleteTcs = null;
            _logoutCancelAckTcs = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        StopKeepAliveLoop();
        StopWorldReceiveLoop();
        StopSnapshotProjectionLoop();
        await DisconnectAllAsync(CancellationToken.None);
    }

    private async Task RespondTimeSyncAsync(byte[] payload, CancellationToken cancellationToken)
    {
        if (_worldConnection is null)
        {
            return;
        }

        var counter = WorldPacketCodec.ParseTimeSyncRequest(payload);
        var clientTimestamp = unchecked((uint)Environment.TickCount);
        var response = WorldPacketCodec.BuildTimeSyncResponsePayload(counter, clientTimestamp);
        Trace.WriteLine($"NET WORLD TIME_SYNC_RESP counter={counter} clientTimestamp={clientTimestamp}");
        await SendWorldPacketAsync(_worldConnection, WorldOpcode.CmsgTimeSyncResp, response, cancellationToken);
    }

    private async Task SendWorldPacketAsync(IConnection connection, WorldOpcode opcode, byte[] payload, CancellationToken cancellationToken)
    {
        await _worldSendLock.WaitAsync(cancellationToken);
        try
        {
            var packet = WorldPacketCodec.BuildPacket(opcode, payload, _worldCrypto);
            Trace.WriteLine($"NET WORLD SEND {opcode} payload={payload.Length} frame={packet.Length}");
            TraceOutgoingPacketValidation(opcode, payload, packet);
            await connection.SendAsync(packet, cancellationToken);
        }
        finally
        {
            _worldSendLock.Release();
        }
    }

    private async Task<(AuthCommand Command, byte[] Payload)> ReadAuthPacketAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var first = await ReadExactAsync(connection, 1, cancellationToken);
        var command = (AuthCommand)first[0];

        using var timeoutCts = CreateTimeoutToken(cancellationToken);
        var buffer = new byte[4096];
        var total = 0;
        do
        {
            var read = await connection.ReceiveAsync(buffer.AsMemory(total), timeoutCts.Token);
            if (read == 0)
            {
                break;
            }

            total += read;
        } while (connection.Available > 0 && total < buffer.Length);

        Trace.WriteLine($"NET AUTH RECV {command} payload={total}");
        return (command, buffer[..total]);
    }

    private async Task<(WorldPacketHeader Header, byte[] Payload)> ReadWorldPacketAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var first = await ReadExactAsync(connection, 1, cancellationToken);
        _worldCrypto.Decrypt(first, 0, 1);

        var headerLength = (first[0] & 0x80) != 0 ? 5 : 4;
        var header = new byte[headerLength];
        header[0] = (byte)(headerLength == 5 ? first[0] & 0x7F : first[0]);

        var rest = await ReadExactAsync(connection, headerLength - 1, cancellationToken);
        Buffer.BlockCopy(rest, 0, header, 1, rest.Length);
        _worldCrypto.Decrypt(header, 1, headerLength - 1);

        var packetHeader = WorldPacketCodec.ParseHeader(header, headerLength);
        var payload = packetHeader.Size > 0
            ? await ReadExactAsync(connection, packetHeader.Size, cancellationToken)
            : [];

        Trace.WriteLine($"NET WORLD RECV {packetHeader.Opcode} payload={payload.Length} header={packetHeader.HeaderLength}");
        return (packetHeader, payload);
    }

    private static async Task<byte[]> ReadExactAsync(IConnection connection, int size, CancellationToken cancellationToken)
    {
        var buffer = new byte[size];
        var read = 0;
        using var timeoutCts = CreateTimeoutToken(cancellationToken);
        while (read < size)
        {
            var bytes = await connection.ReceiveAsync(buffer.AsMemory(read, size - read), timeoutCts.Token);
            if (bytes == 0)
            {
                throw new IOException("Socket closed while reading.");
            }

            read += bytes;
        }

        return buffer;
    }

    private async Task DisconnectAllAsync(CancellationToken cancellationToken)
    {
        StopKeepAliveLoop();
        StopWorldReceiveLoop();
        StopSnapshotProjectionLoop();
        _currentCharacter = null;
        _currentPlayer = null;
        _currentTarget = null;
        _currentMapId = -1;
        _currentOrientation = 0;
        _currentRunSpeed = 7.0f;
        _isAutoAttackActive = false;
        _autoAttackTargetGuid = 0;
        _isMoveForwardActive = false;
        _movingServerCorrectionStreak = 0;
        _serverObservedSelfPosition = null;
        _lastUpdateObjectSelfPosition = null;
        _lastUpdateObjectSelfPositionChangeAtTick = 0;
        _lastMoveStartSentAtTick = 0;
        _lastMoveStopSentAtTick = 0;
        _outgoingValidationSamples.Clear();
        _serverGhostHintUntilTick = 0;
        _serverDeathHintUntilTick = 0;
        _serverAliveHintUntilTick = 0;
        _corpseReclaimBlockedUntilTick = 0;
        _deathMovementResetApplied = false;
        lock (_entitiesSync)
        {
            _entities.Clear();
        }
        ClearQuestState();
        ClearSocialState();
        if (_worldConnection is not null)
        {
            await _worldConnection.DisconnectAsync(cancellationToken);
            await _worldConnection.DisposeAsync();
            _worldConnection = null;
        }

        if (_authConnection is not null)
        {
            await _authConnection.DisconnectAsync(cancellationToken);
            await _authConnection.DisposeAsync();
            _authConnection = null;
        }
    }

    private static CancellationTokenSource CreateTimeoutToken(CancellationToken source, int timeoutMs = DefaultTimeoutMs)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(source);
        cts.CancelAfter(timeoutMs);
        return cts;
    }

    private async Task<MovementCommandScope?> BeginMovementCommandAsync(
        string commandName,
        bool cancelPrevious,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource? previousCommandCts = null;
        CancellationTokenSource commandCts;
        long operationId;

        lock (_movementCommandSync)
        {
            if (cancelPrevious)
            {
                previousCommandCts = _activeMovementCommandCts;
            }

            operationId = Interlocked.Increment(ref _movementCommandSequence);
            commandCts = new CancellationTokenSource();
            _activeMovementCommandCts = commandCts;
            _activeMovementCommandId = operationId;
            _activeMovementCommandName = commandName;
        }

        if (previousCommandCts is not null && !ReferenceEquals(previousCommandCts, commandCts))
        {
            Trace.WriteLine(
                $"NET WORLD MOVE OP_SUPERSEDE nextOp={operationId} nextKind={commandName} prev={BuildMovementCommandLog()}");
            previousCommandCts.Cancel();
        }

        await _movementExecutionGate.WaitAsync(cancellationToken);
        try
        {
            lock (_movementCommandSync)
            {
                if (!ReferenceEquals(_activeMovementCommandCts, commandCts))
                {
                    Trace.WriteLine(
                        $"NET WORLD MOVE OP_ABORT_BEFORE_START op={operationId} kind={commandName} reason=superseded-before-gate");
                    commandCts.Dispose();
                    _movementExecutionGate.Release();
                    return null;
                }
            }

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, commandCts.Token);
            Trace.WriteLine($"NET WORLD MOVE OP_BEGIN op={operationId} kind={commandName}");
            return new MovementCommandScope(this, operationId, commandName, commandCts, linkedCts);
        }
        catch
        {
            commandCts.Dispose();
            _movementExecutionGate.Release();
            throw;
        }
    }

    private void EndMovementCommand(MovementCommandScope scope)
    {
        lock (_movementCommandSync)
        {
            if (ReferenceEquals(_activeMovementCommandCts, scope.CommandCts))
            {
                _activeMovementCommandCts = null;
                _activeMovementCommandId = 0;
                _activeMovementCommandName = null;
            }
        }

        scope.LinkedCts.Dispose();
        scope.CommandCts.Dispose();
        _movementExecutionGate.Release();
        Trace.WriteLine($"NET WORLD MOVE OP_END op={scope.OperationId} kind={scope.CommandName}");
    }

    private bool IsMovementCommandCurrent(long operationId)
    {
        lock (_movementCommandSync)
        {
            return _activeMovementCommandId == operationId;
        }
    }

    private string BuildMovementCommandLog()
    {
        lock (_movementCommandSync)
        {
            return _activeMovementCommandId > 0
                ? $"op={_activeMovementCommandId} kind={_activeMovementCommandName}"
                : "op=none";
        }
    }

    private void ResetQuestAwaiters()
    {
        _questStatusTcs = null;
        _questMenuTcs = null;
        _questDialogTcs = null;
        _questDefinitionTcs = null;
        _questPoiTcs = null;
        _questInvalidTcs = null;
        _questTurnInResultTcs = null;
    }

    private async Task<QuestDialog?> WaitForQuestDialogAsync(
        string operationName,
        ulong questGiverGuid,
        uint questId,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CreateTimeoutToken(cancellationToken);
        var completed = await Task.WhenAny(
            _questDialogTcs!.Task,
            _questInvalidTcs!.Task,
            Task.Delay(Timeout.Infinite, timeoutCts.Token));
        if (completed == _questDialogTcs.Task)
        {
            var dialog = await _questDialogTcs.Task;
            Trace.WriteLine(
                $"NET WORLD QUEST {operationName}_DIALOG questGiver={questGiverGuid} questId={dialog.QuestId} kind={dialog.Kind} canComplete={dialog.CanComplete} rewardChoices={dialog.ChoiceItems.Count} rewardItems={dialog.RewardItems.Count} requiredItems={dialog.RequiredItems.Count}");
            return dialog;
        }

        if (completed == _questInvalidTcs.Task)
        {
            var reason = await _questInvalidTcs.Task;
            Trace.WriteLine(
                $"NET WORLD QUEST {operationName}_REJECTED questGiver={questGiverGuid} questId={questId} reason={reason}");
            return null;
        }

        Trace.WriteLine($"NET WORLD QUEST {operationName}_TIMEOUT questGiver={questGiverGuid} questId={questId}");
        return null;
    }

    private string BuildQuestGiverDebugLog(ulong questGiverGuid)
    {
        lock (_entitiesSync)
        {
            if (!_entities.TryGetValue(questGiverGuid, out var state))
            {
                return "questGiver=unknown";
            }

            return
                $"questGiver(entry={state.EntryId?.ToString() ?? "n/a"},npcFlags=0x{state.NpcFlags:X8},isQuestGiver={state.IsQuestGiver},status={state.QuestGiverStatus?.ToString() ?? "n/a"},pos={FormatEntityPosition(state)})";
        }
    }

    private static string FormatEntityPosition(EntityState state)
    {
        return state.HasPosition
            ? $"({state.X!.Value:F3},{state.Y!.Value:F3},{state.Z!.Value:F3})"
            : "n/a";
    }

    private static string BuildPayloadHexPreview(byte[] payload, int maxBytes = 48)
    {
        return payload.Length > 0
            ? Convert.ToHexString(payload.AsSpan(0, Math.Min(payload.Length, maxBytes)))
            : "empty";
    }

    private static string SanitizeChatLogText(string? value, int maxLength = 64)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace('\r', ' ').Replace('\n', ' ').Replace('"', '\'').Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..Math.Max(0, maxLength - 3)] + "...";
    }

    private static ChatLanguage ResolveChatLanguage(ChatLanguage requestedLanguage, byte? raceId)
    {
        if (requestedLanguage != ChatLanguage.Auto)
        {
            return requestedLanguage;
        }

        return raceId switch
        {
            2 or 5 or 6 or 8 or 10 => ChatLanguage.Orcish,
            _ => ChatLanguage.Common
        };
    }

    private void SetPendingGroupInvite(GroupInviteInfo? invite)
    {
        lock (_socialStateSync)
        {
            _pendingGroupInvite = invite;
        }
    }

    private void SetCurrentGroup(GroupMembershipInfo? group)
    {
        lock (_socialStateSync)
        {
            _currentGroup = group;
        }
    }

    private void ClearSocialState()
    {
        lock (_socialStateSync)
        {
            _currentGroup = null;
            _pendingGroupInvite = null;
            _incomingChatMessages.Clear();
        }
    }

    private void EnqueueIncomingChatMessage(ReceivedChatMessage message)
    {
        lock (_socialStateSync)
        {
            if (_incomingChatMessages.Count >= 64)
            {
                _ = _incomingChatMessages.Dequeue();
            }

            _incomingChatMessages.Enqueue(message);
        }
    }

    private string? ResolveKnownPlayerName(ulong guid)
    {
        if (guid == 0)
        {
            return null;
        }

        lock (_socialStateSync)
        {
            if (_currentCharacter is not null && _currentCharacter.Guid == guid)
            {
                return _currentCharacter.Name;
            }

            if (_currentGroup is not null)
            {
                var member = _currentGroup.Members.FirstOrDefault(x => x.Guid == guid);
                if (member is not null && !string.IsNullOrWhiteSpace(member.Name))
                {
                    return member.Name;
                }
            }
        }

        return null;
    }

    private async Task WarmQuestKnowledgeAsync(uint questId, CancellationToken cancellationToken)
    {
        var needDefinition = false;
        var needPoi = false;
        lock (_questStateSync)
        {
            needDefinition = !_questDefinitions.ContainsKey(questId);
            needPoi = !_questPois.ContainsKey(questId);
        }

        if (needDefinition)
        {
            try
            {
                _ = await QueryQuestDefinitionAsync(questId, cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }

        if (needPoi)
        {
            try
            {
                _ = await QueryQuestPoiAsync([questId], cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }
    }

    private void RegisterAcceptedQuest(uint questId)
    {
        lock (_questStateSync)
        {
            if (!_activeQuests.TryGetValue(questId, out var questState))
            {
                questState = new ActiveQuestState(questId);
                _activeQuests[questId] = questState;
            }

            questState.IsCompleted = false;
            questState.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
            if (_questDefinitions.TryGetValue(questId, out var definition))
            {
                ApplyQuestDefinition(questState, definition);
            }
        }
    }

    private void RemoveActiveQuest(uint questId)
    {
        lock (_questStateSync)
        {
            _activeQuests.Remove(questId);
        }
    }

    private void CacheQuestDefinition(QuestDefinition definition)
    {
        lock (_questStateSync)
        {
            _questDefinitions[definition.QuestId] = definition;
            if (_activeQuests.TryGetValue(definition.QuestId, out var questState))
            {
                ApplyQuestDefinition(questState, definition);
            }
        }
    }

    private void CacheQuestPois(IReadOnlyList<QuestPoiInfo> pois)
    {
        lock (_questStateSync)
        {
            foreach (var poi in pois)
            {
                _questPois[poi.QuestId] = poi;
                if (!_activeQuests.TryGetValue(poi.QuestId, out var questState))
                {
                    continue;
                }

                questState.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }
    }

    private void ClearQuestState()
    {
        lock (_questStateSync)
        {
            _questDefinitions.Clear();
            _questPois.Clear();
            _activeQuests.Clear();
        }
    }

    private void MarkQuestCompleted(uint questId, bool isCompleted)
    {
        lock (_questStateSync)
        {
            if (!_activeQuests.TryGetValue(questId, out var questState))
            {
                questState = new ActiveQuestState(questId);
                _activeQuests[questId] = questState;
            }

            questState.IsCompleted = isCompleted;
            questState.LastUpdatedAtUtc = DateTimeOffset.UtcNow;

            if (!isCompleted || !_questDefinitions.TryGetValue(questId, out var definition))
            {
                return;
            }

            foreach (var objective in definition.Objectives)
            {
                if (objective.ObjectiveIndex is < 0 or >= 4)
                {
                    continue;
                }

                questState.CreatureOrGoCounts[objective.ObjectiveIndex] = Math.Max(
                    questState.CreatureOrGoCounts[objective.ObjectiveIndex],
                    objective.RequiredCount);
                questState.CreatureOrGoKnown[objective.ObjectiveIndex] = true;
            }

            foreach (var itemObjective in definition.ItemObjectives)
            {
                if (itemObjective.ObjectiveIndex is < 0 or >= 6)
                {
                    continue;
                }

                questState.ItemCounts[itemObjective.ObjectiveIndex] = itemObjective.RequiredCount;
            }
        }
    }

    private void UpdateQuestObjectiveProgress(uint questId, uint rawObjectiveEntry, uint currentCount, uint requiredCount)
    {
        lock (_questStateSync)
        {
            if (!_activeQuests.TryGetValue(questId, out var questState))
            {
                questState = new ActiveQuestState(questId);
                _activeQuests[questId] = questState;
            }

            questState.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
            questState.PendingObjectiveProgress[rawObjectiveEntry] = new QuestObjectiveRuntimeProgress(currentCount, requiredCount);

            if (_questDefinitions.TryGetValue(questId, out var definition))
            {
                ApplyDefinitionProgress(questState, definition, rawObjectiveEntry, currentCount, requiredCount);
            }
        }
    }

    private void ApplyQuestDefinition(ActiveQuestState questState, QuestDefinition definition)
    {
        foreach (var objective in definition.Objectives)
        {
            if (objective.ObjectiveIndex is < 0 or >= 4)
            {
                continue;
            }

            questState.CreatureOrGoCounts[objective.ObjectiveIndex] = Math.Min(
                questState.CreatureOrGoCounts[objective.ObjectiveIndex],
                objective.RequiredCount);
            if (!questState.CreatureOrGoKnown[objective.ObjectiveIndex] &&
                objective.Kind is QuestObjectiveKind.Creature or QuestObjectiveKind.GameObject or QuestObjectiveKind.None)
            {
                questState.CreatureOrGoKnown[objective.ObjectiveIndex] = true;
            }

            var wireEntry = GetQuestObjectiveWireEntry(objective);
            if (wireEntry.HasValue &&
                questState.PendingObjectiveProgress.TryGetValue(wireEntry.Value, out var pending))
            {
                ApplyDefinitionProgress(questState, definition, wireEntry.Value, pending.CurrentCount, pending.RequiredCount);
            }
        }
    }

    private static void ApplyDefinitionProgress(
        ActiveQuestState questState,
        QuestDefinition definition,
        uint rawObjectiveEntry,
        uint currentCount,
        uint requiredCount)
    {
        foreach (var objective in definition.Objectives)
        {
            var wireEntry = GetQuestObjectiveWireEntry(objective);
            if (!wireEntry.HasValue || wireEntry.Value != rawObjectiveEntry)
            {
                continue;
            }

            if (objective.ObjectiveIndex is < 0 or >= 4)
            {
                continue;
            }

            questState.CreatureOrGoCounts[objective.ObjectiveIndex] = currentCount;
            questState.CreatureOrGoKnown[objective.ObjectiveIndex] = true;
            if (requiredCount > 0 && currentCount >= requiredCount)
            {
                questState.CreatureOrGoCounts[objective.ObjectiveIndex] = requiredCount;
            }
        }
    }

    private IReadOnlyList<ActiveQuestSnapshot> BuildActiveQuestSnapshots()
    {
        lock (_questStateSync)
        {
            return _activeQuests.Values
                .OrderBy(x => x.QuestId)
                .Select(x => BuildActiveQuestSnapshot(x))
                .ToArray();
        }
    }

    private ActiveQuestSnapshot BuildActiveQuestSnapshot(ActiveQuestState questState)
    {
        _questDefinitions.TryGetValue(questState.QuestId, out var definition);
        _questPois.TryGetValue(questState.QuestId, out var poi);

        var objectives = definition?.Objectives
            .Select(objective =>
            {
                var currentCount = 0u;
                if (objective.ObjectiveIndex is >= 0 and < 4 && questState.CreatureOrGoKnown[objective.ObjectiveIndex])
                {
                    currentCount = questState.CreatureOrGoCounts[objective.ObjectiveIndex];
                }

                if (questState.IsCompleted && currentCount < objective.RequiredCount)
                {
                    currentCount = objective.RequiredCount;
                }

                return new QuestObjectiveProgressSnapshot(
                    objective.ObjectiveIndex,
                    objective.Kind,
                    objective.TargetEntryId,
                    objective.Text,
                    objective.RequiredCount,
                    currentCount,
                    objective.RequiredCount > 0 && currentCount >= objective.RequiredCount);
            })
            .ToArray() ?? [];

        var itemObjectives = definition?.ItemObjectives
            .Select(itemObjective =>
            {
                uint? currentCount = null;
                if (itemObjective.ObjectiveIndex is >= 0 and < 6)
                {
                    currentCount = questState.ItemCounts[itemObjective.ObjectiveIndex];
                }

                if (questState.IsCompleted && currentCount is null)
                {
                    currentCount = itemObjective.RequiredCount;
                }

                return new QuestItemObjectiveProgressSnapshot(
                    itemObjective.ObjectiveIndex,
                    itemObjective.ItemId,
                    itemObjective.RequiredCount,
                    currentCount,
                    currentCount.HasValue && currentCount.Value >= itemObjective.RequiredCount);
            })
            .ToArray() ?? [];

        var poiBlobs = poi?.Blobs
            .Select(blob => new QuestPoiBlobSnapshot(
                blob.BlobIndex,
                blob.ObjectiveIndex,
                blob.MapId,
                blob.WorldMapAreaId,
                blob.Floor,
                blob.Priority,
                blob.Flags,
                blob.Points.Select(point => new QuestPoiPointSnapshot(point.X, point.Y)).ToArray()))
            .ToArray() ?? [];

        return new ActiveQuestSnapshot(
            questState.QuestId,
            definition?.Title,
            definition?.ObjectivesSummary ?? string.Empty,
            questState.IsCompleted,
            objectives,
            itemObjectives,
            poiBlobs);
    }

    private static uint? GetQuestObjectiveWireEntry(QuestObjectiveDefinition objective)
    {
        if (!objective.TargetEntryId.HasValue)
        {
            return null;
        }

        return objective.Kind switch
        {
            QuestObjectiveKind.GameObject => objective.TargetEntryId.Value | 0x80000000,
            QuestObjectiveKind.Creature => objective.TargetEntryId.Value,
            _ => null
        };
    }

    private void StartKeepAliveLoop()
    {
        StopKeepAliveLoop();
        _keepAliveCts = new CancellationTokenSource();
        _keepAliveTask = Task.Run(async () =>
        {
            while (!_keepAliveCts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), _keepAliveCts.Token);
                    if (_keepAliveCts.IsCancellationRequested || _worldConnection is null || !_worldConnection.IsConnected)
                    {
                        continue;
                    }

                    await SendWorldPacketAsync(_worldConnection, WorldOpcode.CmsgKeepAlive, [], _keepAliveCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        });
    }

    private void StopKeepAliveLoop()
    {
        try
        {
            _keepAliveCts?.Cancel();
            _keepAliveTask?.Wait(TimeSpan.FromMilliseconds(100));
        }
        catch
        {
            // ignored during shutdown
        }
        finally
        {
            _keepAliveCts?.Dispose();
            _keepAliveCts = null;
            _keepAliveTask = null;
        }
    }

    private void StartWorldReceiveLoop()
    {
        if (_worldConnection is null || !_worldConnection.IsConnected)
        {
            return;
        }

        StopWorldReceiveLoop();
        _worldReceiveCts = new CancellationTokenSource();
        var receiveCts = _worldReceiveCts;
        var receiveToken = receiveCts.Token;
        _worldReceiveTask = Task.Run(async () =>
        {
            while (!receiveToken.IsCancellationRequested)
            {
                var connection = _worldConnection;
                if (connection is null || !connection.IsConnected)
                {
                    break;
                }

                WorldPacketHeader header;
                byte[] payload;
                try
                {
                    var packet = await ReadWorldPacketAsync(connection, receiveToken);
                    header = packet.Header;
                    payload = packet.Payload;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(
                        $"NET WORLD RECEIVE LOOP READ_ERROR type={ex.GetType().Name} message={ex.Message}");
                    Trace.WriteLine(ex.ToString());
                    if (receiveToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        await Task.Delay(50, receiveToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    continue;
                }

                try
                {
                    await HandleWorldPacketAsync(header.Opcode, payload, receiveToken);
                }
                catch (OperationCanceledException)
                {
                    if (receiveToken.IsCancellationRequested)
                    {
                        break;
                    }

                    continue;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(
                        $"NET WORLD RECEIVE LOOP HANDLE_ERROR opcode={header.Opcode} payload={payload.Length} " +
                        $"type={ex.GetType().Name} message={ex.Message}");
                    Trace.WriteLine(ex.ToString());
                }
            }

            if (!receiveToken.IsCancellationRequested &&
                ReferenceEquals(_worldReceiveCts, receiveCts) &&
                _worldConnection is { IsConnected: true })
            {
                Trace.WriteLine("NET WORLD RECEIVE LOOP WATCHDOG restart_scheduled=true delayMs=250");
                _ = Task.Run(async () =>
                {
                    await Task.Delay(250).ConfigureAwait(false);
                    if (!receiveToken.IsCancellationRequested &&
                        ReferenceEquals(_worldReceiveCts, receiveCts) &&
                        _worldConnection is { IsConnected: true })
                    {
                        Trace.WriteLine("NET WORLD RECEIVE LOOP WATCHDOG restart_now=true");
                        StartWorldReceiveLoop();
                    }
                });
            }
        });
    }

    private void StartSnapshotProjectionLoop()
    {
        StopSnapshotProjectionLoop();
        _snapshotProjectionCts = new CancellationTokenSource();
        _snapshotProjectionTask = Task.Run(async () =>
        {
            while (!_snapshotProjectionCts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(SnapshotProjectionIntervalMs, _snapshotProjectionCts.Token);
                    if (_snapshotProjectionCts.IsCancellationRequested)
                    {
                        continue;
                    }

                    UpdateSnapshot(_currentPlayer, _currentTarget);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"NET WORLD SNAPSHOT PROJECTION LOOP ERROR {ex.Message}");
                    break;
                }
            }
        });
    }

    private void StopSnapshotProjectionLoop()
    {
        try
        {
            _snapshotProjectionCts?.Cancel();
            _snapshotProjectionTask?.Wait(TimeSpan.FromMilliseconds(200));
        }
        catch
        {
            // ignored during shutdown
        }
        finally
        {
            _snapshotProjectionCts?.Dispose();
            _snapshotProjectionCts = null;
            _snapshotProjectionTask = null;
        }
    }

    private void StopWorldReceiveLoop()
    {
        try
        {
            _worldReceiveCts?.Cancel();
            _worldReceiveTask?.Wait(TimeSpan.FromMilliseconds(200));
        }
        catch
        {
            // ignored during shutdown
        }
        finally
        {
            _worldReceiveCts?.Dispose();
            _worldReceiveCts = null;
            _worldReceiveTask = null;
        }
    }

    private async Task HandleWorldPacketAsync(WorldOpcode opcode, byte[] payload, CancellationToken cancellationToken)
    {
        switch (opcode)
        {
            case WorldOpcode.SmsgTimeSyncReq:
                await RespondTimeSyncAsync(payload, cancellationToken);
                break;

            case WorldOpcode.SmsgLogoutResponse:
                _logoutResponseTcs?.TrySetResult(WorldPacketCodec.ParseLogoutResponse(payload));
                break;

            case WorldOpcode.SmsgLogoutComplete:
                _logoutCompleteTcs?.TrySetResult(true);
                break;

            case WorldOpcode.SmsgLogoutCancelAck:
                _logoutCancelAckTcs?.TrySetResult(true);
                break;

            case WorldOpcode.SmsgTransferPending:
                if (payload.Length >= 4)
                {
                    var mapId = WorldPacketCodec.ParseTransferPendingMapId(payload);
                    Trace.WriteLine($"NET WORLD TRANSFER PENDING map={mapId} payload={payload.Length}");
                }
                else
                {
                    Trace.WriteLine($"NET WORLD TRANSFER PENDING malformed payload={payload.Length}");
                }
                break;

            case WorldOpcode.SmsgNewWorld:
                if (_worldConnection is null || !_worldConnection.IsConnected)
                {
                    break;
                }

                if (payload.Length >= 20)
                {
                    var location = WorldPacketCodec.ParseNewWorld(payload);
                    _currentMapId = location.MapId;
                    _currentOrientation = location.Orientation;
                    if (_currentPlayer is not null)
                    {
                        _currentPlayer = _currentPlayer with
                        {
                            X = location.X,
                            Y = location.Y,
                            Z = location.Z,
                            Orientation = location.Orientation
                        };
                        _serverObservedSelfPosition = new NavigationPoint(location.X, location.Y, location.Z);
                    }

                    Trace.WriteLine(
                        $"NET WORLD TRANSFER NEW_WORLD map={location.MapId} " +
                        $"pos=({location.X:F3},{location.Y:F3},{location.Z:F3}) o={location.Orientation:F3}");
                    UpdateSnapshot(_currentPlayer, _currentTarget);
                }
                else
                {
                    Trace.WriteLine($"NET WORLD TRANSFER NEW_WORLD malformed payload={payload.Length}");
                }

                await SendWorldPacketAsync(
                    _worldConnection,
                    WorldOpcode.MsgMoveWorldportAck,
                    WorldPacketCodec.BuildMoveWorldportAckPayload(),
                    cancellationToken);
                Trace.WriteLine("NET WORLD TRANSFER WORLDPORT_ACK sent");
                break;

            case WorldOpcode.SmsgCorpseReclaimDelay:
                if (payload.Length >= 4)
                {
                    var remainingMs = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    var nowTick = Environment.TickCount64;
                    var previousBlockedUntilTick = _corpseReclaimBlockedUntilTick;
                    var previousRemainingMs = Math.Max(0, previousBlockedUntilTick - nowTick);
                    _serverDeathHintUntilTick = Math.Max(_serverDeathHintUntilTick, nowTick + 15000);
                    _corpseReclaimBlockedUntilTick = remainingMs > 0
                        ? nowTick + remainingMs
                        : nowTick;
                    var hex = Convert.ToHexString(payload.AsSpan(0, Math.Min(payload.Length, 16)));
                    Trace.WriteLine(
                        $"NET WORLD DEATH SMSG_CORPSE_RECLAIM_DELAY remainingMs={remainingMs} " +
                        $"previousRemainingMs={previousRemainingMs} nowTick={nowTick} " +
                        $"blockedUntilTick={_corpseReclaimBlockedUntilTick} " +
                        $"deltaBlockedMs={_corpseReclaimBlockedUntilTick - previousBlockedUntilTick} " +
                        $"payloadHex={hex}");
                }
                else
                {
                    var hex = payload.Length > 0
                        ? Convert.ToHexString(payload.AsSpan(0, Math.Min(payload.Length, 16)))
                        : "empty";
                    Trace.WriteLine(
                        $"NET WORLD DEATH SMSG_CORPSE_RECLAIM_DELAY malformed payload={payload.Length} payloadHex={hex}");
                }
                break;

            case WorldOpcode.SmsgDeathReleaseLoc:
                if (payload.Length >= 16)
                {
                    var mapId = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(0, 4));
                    var x = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(4, 4));
                    var y = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(8, 4));
                    var z = BinaryPrimitives.ReadSingleLittleEndian(payload.AsSpan(12, 4));
                    var nowTick = Environment.TickCount64;
                    _serverGhostHintUntilTick = Math.Max(_serverGhostHintUntilTick, nowTick + 20000);
                    _serverDeathHintUntilTick = Math.Max(_serverDeathHintUntilTick, nowTick + 20000);

                    // Death release location is authoritative for ghost spawn/teleport.
                    // Keep this anchor for a short window to avoid stale local drift.
                    if (mapId >= 0)
                    {
                        var releasePoint = new NavigationPoint(x, y, z);
                        _deathReleaseLocation = releasePoint;
                        _deathReleaseMapId = mapId;
                        // Keep a short authority window for cemetery teleport stabilization,
                        // then let regular movement convergence handle ghost travel.
                        _deathReleaseLocationUntilTick = nowTick + 2500;
                        _currentMapId = mapId;
                        ResetMovementStateForServerRelocation("DEATH_RELEASE_LOC");

                        if (_currentPlayer is not null)
                        {
                            var localBefore = new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z);
                            _currentPlayer = _currentPlayer with
                            {
                                X = x,
                                Y = y,
                                Z = z
                            };
                            _serverObservedSelfPosition = releasePoint;
                            _lastUpdateObjectSelfPosition = releasePoint;
                            _lastUpdateObjectSelfPositionChangeAtTick = nowTick;
                            _movingServerCorrectionStreak = 0;

                            lock (_entitiesSync)
                            {
                                if (_currentCharacter is not null)
                                {
                                    if (!_entities.TryGetValue(_currentCharacter.Guid, out var selfState))
                                    {
                                        selfState = new EntityState
                                        {
                                            Guid = _currentCharacter.Guid,
                                            TypeId = 4
                                        };
                                        _entities[_currentCharacter.Guid] = selfState;
                                    }

                                    selfState.X = x;
                                    selfState.Y = y;
                                    selfState.Z = z;
                                    selfState.HasActiveSpline = false;
                                }
                            }

                            LogServerForcedPosition(
                                opcode,
                                "DEATH_RELEASE_LOC",
                                localBefore,
                                releasePoint,
                                nowTick,
                                null);
                            UpdateSnapshot(_currentPlayer, _currentTarget);
                        }
                    }
                    else
                    {
                        _deathReleaseLocation = null;
                        _deathReleaseMapId = -1;
                        _deathReleaseLocationUntilTick = 0;
                    }

                    Trace.WriteLine(
                        $"NET WORLD DEATH RELEASE_LOC map={mapId} pos=(" +
                        $"{x.ToString("F3", CultureInfo.InvariantCulture)}," +
                        $"{y.ToString("F3", CultureInfo.InvariantCulture)}," +
                        $"{z.ToString("F3", CultureInfo.InvariantCulture)})");
                }
                else
                {
                    Trace.WriteLine($"NET WORLD DEATH RELEASE_LOC malformed payload={payload.Length}");
                }
                break;

            case WorldOpcode.SmsgPreResurrect:
            {
                var index = 0;
                if (TryReadPackedGuid(payload, ref index, out var resurrectGuid))
                {
                    var nowTick = Environment.TickCount64;
                    _serverGhostHintUntilTick = Math.Max(_serverGhostHintUntilTick, nowTick + 10000);
                    _serverDeathHintUntilTick = Math.Max(_serverDeathHintUntilTick, nowTick + 10000);
                    Trace.WriteLine($"NET WORLD DEATH PRE_RESURRECT playerGuid={resurrectGuid}");
                }
                else
                {
                    Trace.WriteLine($"NET WORLD DEATH PRE_RESURRECT malformed payload={payload.Length}");
                }
                break;
            }

            case WorldOpcode.SmsgUpdateObject:
                ApplyUpdateObject(WorldPacketCodec.ParseUpdateObjectBatch(payload));
                break;

            case WorldOpcode.SmsgCompressedUpdateObject:
            {
                var decompressed = WorldPacketCodec.DecompressUpdateObjectPayload(payload);
                if (decompressed.Length > 0)
                {
                    ApplyUpdateObject(WorldPacketCodec.ParseUpdateObjectBatch(decompressed));
                }
                break;
            }

            case WorldOpcode.SmsgForceRunSpeedChange:
                await HandleForceSpeedChangeOpcodeAsync(
                    serverOpcode: opcode,
                    ackOpcode: WorldOpcode.CmsgForceRunSpeedChangeAck,
                    payload: payload,
                    hasLegacyRunMarker: true,
                    updateRunSpeed: true,
                    cancellationToken);
                break;

            case WorldOpcode.SmsgForceRunBackSpeedChange:
                await HandleForceSpeedChangeOpcodeAsync(
                    serverOpcode: opcode,
                    ackOpcode: WorldOpcode.CmsgForceRunBackSpeedChangeAck,
                    payload: payload,
                    hasLegacyRunMarker: false,
                    updateRunSpeed: false,
                    cancellationToken);
                break;

            case WorldOpcode.SmsgForceSwimSpeedChange:
                await HandleForceSpeedChangeOpcodeAsync(
                    serverOpcode: opcode,
                    ackOpcode: WorldOpcode.CmsgForceSwimSpeedChangeAck,
                    payload: payload,
                    hasLegacyRunMarker: false,
                    updateRunSpeed: false,
                    cancellationToken);
                break;

            case WorldOpcode.SmsgForceWalkSpeedChange:
                await HandleForceSpeedChangeOpcodeAsync(
                    serverOpcode: opcode,
                    ackOpcode: WorldOpcode.CmsgForceWalkSpeedChangeAck,
                    payload: payload,
                    hasLegacyRunMarker: false,
                    updateRunSpeed: false,
                    cancellationToken);
                break;

            case WorldOpcode.SmsgForceSwimBackSpeedChange:
                await HandleForceSpeedChangeOpcodeAsync(
                    serverOpcode: opcode,
                    ackOpcode: WorldOpcode.CmsgForceSwimBackSpeedChangeAck,
                    payload: payload,
                    hasLegacyRunMarker: false,
                    updateRunSpeed: false,
                    cancellationToken);
                break;

            case WorldOpcode.SmsgForceTurnRateChange:
                await HandleForceSpeedChangeOpcodeAsync(
                    serverOpcode: opcode,
                    ackOpcode: WorldOpcode.CmsgForceTurnRateChangeAck,
                    payload: payload,
                    hasLegacyRunMarker: false,
                    updateRunSpeed: false,
                    cancellationToken);
                break;

            case WorldOpcode.SmsgForceFlightSpeedChange:
                await HandleForceSpeedChangeOpcodeAsync(
                    serverOpcode: opcode,
                    ackOpcode: WorldOpcode.CmsgForceFlightSpeedChangeAck,
                    payload: payload,
                    hasLegacyRunMarker: false,
                    updateRunSpeed: false,
                    cancellationToken);
                break;

            case WorldOpcode.SmsgForceFlightBackSpeedChange:
                await HandleForceSpeedChangeOpcodeAsync(
                    serverOpcode: opcode,
                    ackOpcode: WorldOpcode.CmsgForceFlightBackSpeedChangeAck,
                    payload: payload,
                    hasLegacyRunMarker: false,
                    updateRunSpeed: false,
                    cancellationToken);
                break;

            case WorldOpcode.SmsgForcePitchRateChange:
                await HandleForceSpeedChangeOpcodeAsync(
                    serverOpcode: opcode,
                    ackOpcode: WorldOpcode.CmsgForcePitchRateChangeAck,
                    payload: payload,
                    hasLegacyRunMarker: false,
                    updateRunSpeed: false,
                    cancellationToken);
                break;

            case WorldOpcode.SmsgMonsterMove:
            case WorldOpcode.SmsgMonsterMoveTransport:
                ApplyMonsterMoveOpcode(opcode, payload);
                break;

            case WorldOpcode.MsgMoveTeleportAck:
                if (_worldConnection is null || !_worldConnection.IsConnected)
                {
                    break;
                }

                {
                    var index = 0;
                    if (!TryReadPackedGuid(payload, ref index, out var teleportGuid))
                    {
                        Trace.WriteLine($"NET WORLD TRANSFER TELEPORT_ACK malformed guid payload={payload.Length}");
                        break;
                    }

                    uint sequenceIndex = 0;
                    if (payload.Length >= index + 4)
                    {
                        sequenceIndex = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(index, 4));
                    }

                    ResetMovementStateForServerRelocation("MSG_MOVE_TELEPORT_ACK");
                    var movementTimeMs = unchecked((uint)Environment.TickCount);
                    await SendWorldPacketAsync(
                        _worldConnection,
                        WorldOpcode.MsgMoveTeleportAck,
                        WorldPacketCodec.BuildMoveTeleportAckPayload(teleportGuid, sequenceIndex, movementTimeMs),
                        cancellationToken);
                    Trace.WriteLine(
                        $"NET WORLD TRANSFER TELEPORT_ACK recvGuid={teleportGuid} seq={sequenceIndex} " +
                        $"ackMovementTime={movementTimeMs}");
                }
                break;

            case WorldOpcode.SmsgAttackStart:
                if (_currentCharacter is not null && payload.Length >= 16)
                {
                    var attackerGuid = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(0, 8));
                    var victimGuid = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(8, 8));
                    if (attackerGuid == _currentCharacter.Guid)
                    {
                        _isAutoAttackActive = true;
                        _autoAttackTargetGuid = victimGuid;
                    }
                }

                Trace.WriteLine($"NET WORLD COMBAT ATTACK_START_RECV payload={payload.Length}");
                break;

            case WorldOpcode.SmsgAttackStop:
                _isAutoAttackActive = false;
                _autoAttackTargetGuid = 0;
                Trace.WriteLine($"NET WORLD COMBAT ATTACK_STOP_RECV payload={payload.Length}");
                break;

            case WorldOpcode.SmsgGroupInvite:
            {
                var invite = SocialPacketCodec.ParseGroupInvitePayload(payload);
                SetPendingGroupInvite(invite);
                Trace.WriteLine(
                    $"NET WORLD GROUP INVITE_RECV inviter=\"{invite.InviterName}\" canAccept={invite.CanAccept} proposedRoles={invite.ProposedRoles} lfgSlots={invite.LfgSlots.Count} completedMask={invite.LfgCompletedMask}");

                if (AutoAcceptGroupInvites && invite.CanAccept && _worldConnection is not null && _worldConnection.IsConnected)
                {
                    try
                    {
                        _ = await AcceptPendingGroupInviteAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"NET WORLD GROUP INVITE_AUTO_ACCEPT_FAIL inviter=\"{invite.InviterName}\" error={ex.Message}");
                    }
                }

                break;
            }

            case WorldOpcode.SmsgGroupDecline:
                Trace.WriteLine($"NET WORLD GROUP DECLINE_RECV player=\"{Encoding.UTF8.GetString(payload).TrimEnd('\0')}\"");
                break;

            case WorldOpcode.SmsgGroupCancel:
                SetPendingGroupInvite(null);
                Trace.WriteLine("NET WORLD GROUP CANCEL_RECV");
                break;

            case WorldOpcode.SmsgGroupDestroyed:
                SetCurrentGroup(null);
                Trace.WriteLine("NET WORLD GROUP DESTROYED_RECV");
                break;

            case WorldOpcode.SmsgGroupSetLeader:
            {
                var leaderName = SocialPacketCodec.ParseGroupSetLeaderPayload(payload);
                Trace.WriteLine($"NET WORLD GROUP LEADER_RECV leader=\"{leaderName}\"");
                break;
            }

            case WorldOpcode.SmsgGroupList:
            {
                var group = SocialPacketCodec.ParseGroupListPayload(payload);
                SetCurrentGroup(group);
                if (group is not null)
                {
                    SetPendingGroupInvite(null);
                }
                if (group is null)
                {
                    Trace.WriteLine($"NET WORLD GROUP LIST_RECV state=none payloadHex={BuildPayloadHexPreview(payload)}");
                }
                else
                {
                    Trace.WriteLine(
                        $"NET WORLD GROUP LIST_RECV groupGuid={group.GroupGuid} leaderGuid={group.LeaderGuid} groupType=0x{group.GroupType:X2} membersOther={group.OtherMemberCount} selfSubGroup={group.MemberSubGroup} selfFlags=0x{group.MemberFlags:X2} selfRoles=0x{group.MemberRoles:X2}");
                }

                break;
            }

            case WorldOpcode.SmsgPartyCommandResult:
            {
                var result = SocialPacketCodec.ParsePartyCommandResultPayload(payload);
                Trace.WriteLine(
                    $"NET WORLD GROUP RESULT_RECV operation={result.Operation} member=\"{result.MemberName}\" result={result.Result} value={result.Value}");
                break;
            }

            case WorldOpcode.SmsgPartyMemberStats:
            case WorldOpcode.SmsgPartyMemberStatsFull:
                Trace.WriteLine($"NET WORLD GROUP MEMBER_STATS_RECV opcode={opcode} payload={payload.Length}");
                break;

            case WorldOpcode.SmsgChatNotInParty:
                Trace.WriteLine($"NET WORLD CHAT NOT_IN_PARTY payloadHex={BuildPayloadHexPreview(payload)}");
                break;

            case WorldOpcode.SmsgMessagechat:
            case WorldOpcode.SmsgGmMessagechat:
            {
                var gmMessage = opcode == WorldOpcode.SmsgGmMessagechat;
                var chat = SocialPacketCodec.ParseIncomingChatPayload(payload, gmMessage);
                if (chat is null)
                {
                    Trace.WriteLine(
                        $"NET WORLD CHAT RECV parse=failed gm={gmMessage} opcode={opcode} payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                    break;
                }

                var resolvedSenderName = chat.SenderName ?? ResolveKnownPlayerName(chat.SenderGuid);
                var resolvedReceiverName = chat.ReceiverName ?? ResolveKnownPlayerName(chat.ReceiverGuid);
                var enriched = chat with
                {
                    SenderName = resolvedSenderName,
                    ReceiverName = resolvedReceiverName
                };
                EnqueueIncomingChatMessage(enriched);
                Trace.WriteLine(
                    $"NET WORLD CHAT RECV type={enriched.MessageType} gm={gmMessage} language={enriched.Language} senderGuid={enriched.SenderGuid} sender=\"{SanitizeChatLogText(enriched.SenderName)}\" receiverGuid={enriched.ReceiverGuid} receiver=\"{SanitizeChatLogText(enriched.ReceiverName)}\" channel=\"{SanitizeChatLogText(enriched.ChannelName)}\" tag={enriched.ChatTag} message=\"{SanitizeChatLogText(enriched.Message, 160)}\" payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                break;
            }

            case WorldOpcode.SmsgQuestQueryResponse:
            {
                var definition = QuestPacketCodec.ParseQuestQueryResponse(payload);
                CacheQuestDefinition(definition);
                _questDefinitionTcs?.TrySetResult(definition);
                Trace.WriteLine(
                    $"NET WORLD QUEST QUERY_INFO_CACHE questId={definition.QuestId} title=\"{definition.Title}\" objectives={definition.Objectives.Count} itemObjectives={definition.ItemObjectives.Count} poiHint=({definition.PoiX:F1},{definition.PoiY:F1}) payloadHex={BuildPayloadHexPreview(payload)}");
                UpdateSnapshot(_currentPlayer, _currentTarget);
                break;
            }

            case WorldOpcode.SmsgQuestPoiQueryResponse:
            {
                var pois = QuestPacketCodec.ParseQuestPoiQueryResponse(payload);
                CacheQuestPois(pois);
                _questPoiTcs?.TrySetResult(pois);
                Trace.WriteLine(
                    $"NET WORLD QUEST POI_RECV count={pois.Count} questIds={string.Join(",", pois.Select(x => x.QuestId))} payloadHex={BuildPayloadHexPreview(payload)}");
                UpdateSnapshot(_currentPlayer, _currentTarget);
                break;
            }

            case WorldOpcode.SmsgQuestgiverStatus:
            {
                var status = QuestPacketCodec.ParseQuestGiverStatus(payload);
                lock (_entitiesSync)
                {
                    if (!_entities.TryGetValue(status.QuestGiverGuid, out var state))
                    {
                        state = new EntityState { Guid = status.QuestGiverGuid };
                        _entities[status.QuestGiverGuid] = state;
                    }

                    state.QuestGiverStatus = status.Status;
                }

                _questStatusTcs?.TrySetResult(status);
                Trace.WriteLine(
                    $"NET WORLD QUEST STATUS_RECV questGiver={status.QuestGiverGuid} status={status.Status} payloadHex={BuildPayloadHexPreview(payload)} {BuildQuestGiverDebugLog(status.QuestGiverGuid)}");
                UpdateSnapshot(_currentPlayer, _currentTarget);
                break;
            }

            case WorldOpcode.SmsgQuestgiverStatusMultiple:
            {
                var statuses = QuestPacketCodec.ParseQuestGiverStatusMultiple(payload);
                lock (_entitiesSync)
                {
                    foreach (var status in statuses)
                    {
                        if (!_entities.TryGetValue(status.QuestGiverGuid, out var state))
                        {
                            state = new EntityState { Guid = status.QuestGiverGuid };
                            _entities[status.QuestGiverGuid] = state;
                        }

                        state.QuestGiverStatus = status.Status;
                    }
                }

                Trace.WriteLine(
                    $"NET WORLD QUEST STATUS_MULTIPLE count={statuses.Count} sample={(statuses.Count > 0 ? $"{statuses[0].QuestGiverGuid}:{statuses[0].Status}" : "none")} payloadHex={BuildPayloadHexPreview(payload)}");
                UpdateSnapshot(_currentPlayer, _currentTarget);
                break;
            }

            case WorldOpcode.SmsgQuestgiverQuestList:
            {
                var menu = QuestPacketCodec.ParseQuestGiverQuestList(payload);
                _questMenuTcs?.TrySetResult(menu);
                Trace.WriteLine(
                    $"NET WORLD QUEST MENU_RECV questGiver={menu.QuestGiverGuid} items={menu.Items.Count} greetingLen={menu.Greeting.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                break;
            }

            case WorldOpcode.SmsgQuestGiverQuestDetails:
            {
                var dialog = QuestPacketCodec.ParseQuestGiverQuestDetails(payload);
                _questDialogTcs?.TrySetResult(dialog);
                Trace.WriteLine(
                    $"NET WORLD QUEST DETAILS_RECV questGiver={dialog.QuestGiverGuid} questId={dialog.QuestId} title=\"{dialog.Title}\" rewardChoices={dialog.ChoiceItems.Count} rewardItems={dialog.RewardItems.Count} payloadHex={BuildPayloadHexPreview(payload)}");
                break;
            }

            case WorldOpcode.SmsgQuestgiverRequestItems:
            {
                var dialog = QuestPacketCodec.ParseQuestGiverRequestItems(payload);
                _questDialogTcs?.TrySetResult(dialog);
                Trace.WriteLine(
                    $"NET WORLD QUEST REQUEST_ITEMS_RECV questGiver={dialog.QuestGiverGuid} questId={dialog.QuestId} canComplete={dialog.CanComplete} requiredItems={dialog.RequiredItems.Count} requiredMoney={dialog.RequiredMoney} payloadHex={BuildPayloadHexPreview(payload)}");
                break;
            }

            case WorldOpcode.SmsgQuestGiverOfferRewardMessage:
            {
                var dialog = QuestPacketCodec.ParseQuestGiverOfferReward(payload);
                _questDialogTcs?.TrySetResult(dialog);
                Trace.WriteLine(
                    $"NET WORLD QUEST OFFER_REWARD_RECV questGiver={dialog.QuestGiverGuid} questId={dialog.QuestId} choices={dialog.ChoiceItems.Count} rewardItems={dialog.RewardItems.Count} xp={dialog.RewardXp} money={dialog.RewardMoney} payloadHex={BuildPayloadHexPreview(payload)}");
                break;
            }

            case WorldOpcode.SmsgQuestgiverQuestInvalid:
            {
                var reason = QuestPacketCodec.ParseQuestGiverQuestInvalidReason(payload);
                _questInvalidTcs?.TrySetResult(reason);
                Trace.WriteLine($"NET WORLD QUEST INVALID_RECV reason={reason} payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                break;
            }

            case WorldOpcode.SmsgQuestgiverQuestComplete:
            {
                var turnIn = QuestPacketCodec.ParseQuestGiverQuestComplete(payload);
                RemoveActiveQuest(turnIn.QuestId);
                _questTurnInResultTcs?.TrySetResult(turnIn);
                Trace.WriteLine(
                    $"NET WORLD QUEST COMPLETE_RECV questId={turnIn.QuestId} xp={turnIn.RewardXp} money={turnIn.RewardMoney} honor={turnIn.RewardHonor} talents={turnIn.RewardTalents} arena={turnIn.RewardArenaPoints} payloadHex={BuildPayloadHexPreview(payload)}");
                UpdateSnapshot(_currentPlayer, _currentTarget);
                break;
            }

            case WorldOpcode.SmsgQuestgiverQuestFailed:
                if (payload.Length >= 8)
                {
                    var questId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    var reason = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(4, 4));
                    Trace.WriteLine($"NET WORLD QUEST FAILED_RECV questId={questId} reason={reason} payloadHex={BuildPayloadHexPreview(payload)}");
                }
                else
                {
                    Trace.WriteLine($"NET WORLD QUEST FAILED_RECV malformed payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                }
                break;

            case WorldOpcode.SmsgQuestupdateComplete:
                if (payload.Length >= 4)
                {
                    var questId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    MarkQuestCompleted(questId, true);
                    Trace.WriteLine($"NET WORLD QUEST UPDATE_COMPLETE questId={questId} payloadHex={BuildPayloadHexPreview(payload)}");
                    UpdateSnapshot(_currentPlayer, _currentTarget);
                }
                else
                {
                    Trace.WriteLine($"NET WORLD QUEST UPDATE_COMPLETE malformed payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                }
                break;

            case WorldOpcode.SmsgQuestupdateAddKill:
                if (payload.Length >= 24)
                {
                    var questId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    var objectiveEntry = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(4, 4));
                    var currentCount = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(8, 4));
                    var requiredCount = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(12, 4));
                    var guid = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(16, 8));
                    UpdateQuestObjectiveProgress(questId, objectiveEntry, currentCount, requiredCount);
                    Trace.WriteLine(
                        $"NET WORLD QUEST UPDATE_ADD_KILL questId={questId} objectiveEntry={objectiveEntry} current={currentCount} required={requiredCount} guid={guid} payloadHex={BuildPayloadHexPreview(payload)}");
                    UpdateSnapshot(_currentPlayer, _currentTarget);
                }
                else
                {
                    Trace.WriteLine($"NET WORLD QUEST UPDATE_ADD_KILL malformed payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                }
                break;

            case WorldOpcode.SmsgQuestupdateAddItem:
                Trace.WriteLine($"NET WORLD QUEST UPDATE_ADD_ITEM payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                break;

            case WorldOpcode.SmsgQuestupdateFailed:
                if (payload.Length >= 4)
                {
                    var questId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    MarkQuestCompleted(questId, false);
                    Trace.WriteLine($"NET WORLD QUEST UPDATE_FAILED questId={questId} payloadHex={BuildPayloadHexPreview(payload)}");
                    UpdateSnapshot(_currentPlayer, _currentTarget);
                }
                else
                {
                    Trace.WriteLine($"NET WORLD QUEST UPDATE_FAILED malformed payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                }
                break;

            case WorldOpcode.SmsgQuestupdateFailedtimer:
                if (payload.Length >= 4)
                {
                    var questId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    MarkQuestCompleted(questId, false);
                    Trace.WriteLine($"NET WORLD QUEST UPDATE_FAILED_TIMER questId={questId} payloadHex={BuildPayloadHexPreview(payload)}");
                    UpdateSnapshot(_currentPlayer, _currentTarget);
                }
                else
                {
                    Trace.WriteLine($"NET WORLD QUEST UPDATE_FAILED_TIMER malformed payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                }
                break;

            case WorldOpcode.SmsgGossipMessage:
            case WorldOpcode.SmsgGossipComplete:
            case WorldOpcode.SmsgGossipPoi:
                Trace.WriteLine(
                    $"NET WORLD GOSSIP RECV opcode={opcode} payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                break;

            default:
                if (IsClientMovementPayloadOpcode(opcode))
                {
                    ApplyMovementOpcode(opcode, payload);
                }
                else if (IsAnyMovementOpcode(opcode))
                {
                    Trace.WriteLine($"NET WORLD MOVE RECV opcode={opcode} parser=unsupported-layout payload={payload.Length}");
                }
                else if (IsQuestOrGossipOpcode(opcode))
                {
                    Trace.WriteLine(
                        $"NET WORLD QUEST UNHANDLED opcode={opcode} payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                }
                else if (IsSocialOpcode(opcode))
                {
                    Trace.WriteLine(
                        $"NET WORLD SOCIAL UNHANDLED opcode={opcode} payload={payload.Length} payloadHex={BuildPayloadHexPreview(payload)}");
                }
                break;
        }
    }

    private async Task HandleForceSpeedChangeOpcodeAsync(
        WorldOpcode serverOpcode,
        WorldOpcode ackOpcode,
        byte[] payload,
        bool hasLegacyRunMarker,
        bool updateRunSpeed,
        CancellationToken cancellationToken)
    {
        if (_worldConnection is null || !_worldConnection.IsConnected || _currentCharacter is null || _currentPlayer is null)
        {
            Trace.WriteLine(
                $"NET WORLD SPEED_FORCE_ACK skip opcode={serverOpcode} payload={payload.Length} reason=client_not_ready");
            return;
        }

        if (!MovementPacketCodec.TryParseForceSpeedChangePayload(payload, hasLegacyRunMarker, out var forceData))
        {
            Trace.WriteLine(
                $"NET WORLD SPEED_FORCE_ACK malformed opcode={serverOpcode} payload={payload.Length} " +
                $"legacyRunMarker={hasLegacyRunMarker}");
            return;
        }

        if (updateRunSpeed)
        {
            _currentRunSpeed = forceData.NewSpeed;
        }

        var movementTimeMs = unchecked((uint)Environment.TickCount);
        var ackPayload = MovementPacketCodec.BuildForceSpeedChangeAckPayload(
            _currentCharacter.Guid,
            forceData.MovementCounter,
            movementTimeMs,
            _currentPlayer.X,
            _currentPlayer.Y,
            _currentPlayer.Z,
            _currentOrientation,
            forceData.NewSpeed);

        await SendWorldPacketAsync(_worldConnection, ackOpcode, ackPayload, cancellationToken);
        Trace.WriteLine(
            $"NET WORLD SPEED_FORCE_ACK recvOpcode={serverOpcode} sendOpcode={ackOpcode} " +
            $"counter={forceData.MovementCounter} speed={forceData.NewSpeed:F3} " +
            $"runSpeed={_currentRunSpeed:F3} moveTime={movementTimeMs} " +
            $"pos=({_currentPlayer.X:F3},{_currentPlayer.Y:F3},{_currentPlayer.Z:F3},{_currentOrientation:F3})");
    }

    private void ApplyMovementOpcode(WorldOpcode opcode, byte[] payload)
    {
        if (!MovementPacketCodec.TryParseMovementPayload(payload, out var movement))
        {
            Trace.WriteLine($"NET WORLD MOVE RECV opcode={opcode} parse=failed payload={payload.Length}");
            return;
        }

        var now = Environment.TickCount64;
        string sentCorrelation = string.Empty;
        lock (_entitiesSync)
        {
            if (!_entities.TryGetValue(movement.Guid, out var state))
            {
                state = new EntityState();
                _entities[movement.Guid] = state;
            }

            state.X = movement.X;
            state.Y = movement.Y;
            state.Z = movement.Z;
            state.O = movement.Orientation;

            if (_currentCharacter is not null && movement.Guid == _currentCharacter.Guid && _currentPlayer is not null)
            {
                var localBefore = new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z);
                _lastReceivedSelfMovement = new MovementTelemetry(opcode, movement.MovementTimeMs, movement.X, movement.Y, movement.Z, now);
                _movingServerCorrectionStreak = 0;
                var echoDistance = MathF.Sqrt(DistanceSquared(
                    localBefore.X,
                    localBefore.Y,
                    localBefore.Z,
                    movement.X,
                    movement.Y,
                    movement.Z));
                if (_lastSentMovement is MovementTelemetry sent)
                {
                    var dtTickMs = now - sent.LocalTickMs;
                    var dPosFromSent = MathF.Sqrt(DistanceSquared(movement.X, movement.Y, movement.Z, sent.X, sent.Y, sent.Z));
                    var dMoveTime = sent.MovementTimeMs.HasValue
                        ? MovementTimeDelta(movement.MovementTimeMs, sent.MovementTimeMs.Value).ToString()
                        : "n/a";
                    sentCorrelation =
                        $" sentOpcode={sent.Opcode} sentMoveTime={FormatMovementTime(sent.MovementTimeMs)} recvMoveTime={movement.MovementTimeMs} " +
                        $"dtTickMs={dtTickMs} dMoveTime={dMoveTime} dPosFromSent={dPosFromSent:F3}";
                }

                Trace.WriteLine(
                    $"NET WORLD SELF_MOVE_ECHO opcode={opcode} applied=False " +
                    $"local=({localBefore.X:F3},{localBefore.Y:F3},{localBefore.Z:F3},{_currentOrientation:F3}) " +
                    $"echo=({movement.X:F3},{movement.Y:F3},{movement.Z:F3},{movement.Orientation:F3}) " +
                    $"delta={echoDistance:F3}{sentCorrelation}");
            }
        }

        Trace.WriteLine(
            $"NET WORLD MOVE RECV opcode={opcode} guid={movement.Guid} " +
            $"flags={(uint)movement.Flags} flags2={movement.ExtraFlags} time={movement.MovementTimeMs} fall={movement.FallTimeMs} " +
            $"pos=({movement.X:F3},{movement.Y:F3},{movement.Z:F3},{movement.Orientation:F3})" +
            sentCorrelation);
        UpdateSnapshot(_currentPlayer, _currentTarget);
    }

    private void ApplyMonsterMoveOpcode(WorldOpcode opcode, byte[] payload)
    {
        if (!MovementPacketCodec.TryParseMonsterMovePayload(opcode, payload, out var movement))
        {
            Trace.WriteLine($"NET WORLD MOVE RECV opcode={opcode} parse=failed-monster payload={payload.Length}");
            return;
        }

        var now = Environment.TickCount64;
        string sentCorrelation = string.Empty;
        lock (_entitiesSync)
        {
            if (!_entities.TryGetValue(movement.Guid, out var state))
            {
                state = new EntityState();
                _entities[movement.Guid] = state;
            }

            // SMSG_MONSTER_MOVE carries spline info for moving units.
            // Keep movement plan so snapshots can project the in-flight position.
            state.ApplyMonsterMove(movement, now);

            if (_currentCharacter is not null && movement.Guid == _currentCharacter.Guid && _currentPlayer is not null)
            {
                var localBefore = new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z);
                _lastReceivedSelfMovement = new MovementTelemetry(opcode, null, movement.StartX, movement.StartY, movement.StartZ, now);
                _movingServerCorrectionStreak = 0;
                var echoDistance = MathF.Sqrt(DistanceSquared(
                    localBefore.X,
                    localBefore.Y,
                    localBefore.Z,
                    movement.StartX,
                    movement.StartY,
                    movement.StartZ));
                if (_lastSentMovement is MovementTelemetry sent)
                {
                    var dtTickMs = now - sent.LocalTickMs;
                    var dPosFromSent = MathF.Sqrt(DistanceSquared(movement.StartX, movement.StartY, movement.StartZ, sent.X, sent.Y, sent.Z));
                    sentCorrelation =
                        $" sentOpcode={sent.Opcode} sentMoveTime={FormatMovementTime(sent.MovementTimeMs)} recvMoveTime=n/a " +
                        $"dtTickMs={dtTickMs} dMoveTime=n/a dPosFromSent={dPosFromSent:F3}";
                }

                Trace.WriteLine(
                    $"NET WORLD SELF_MOVE_ECHO opcode={opcode} applied=False " +
                    $"local=({localBefore.X:F3},{localBefore.Y:F3},{localBefore.Z:F3},{_currentOrientation:F3}) " +
                    $"echo=({movement.StartX:F3},{movement.StartY:F3},{movement.StartZ:F3},{_currentOrientation:F3}) " +
                    $"delta={echoDistance:F3}{sentCorrelation}");
            }
        }

        Trace.WriteLine(
            $"NET WORLD MOVE RECV opcode={opcode} guid={movement.Guid} " +
            $"moveType={movement.MoveType} splineId={movement.SplineId} splineFlags=0x{movement.SplineFlags:X8} duration={movement.DurationMs} " +
            $"start=({movement.StartX:F3},{movement.StartY:F3},{movement.StartZ:F3}) " +
            $"end=({movement.EndX:F3},{movement.EndY:F3},{movement.EndZ:F3}) " +
            $"transport={movement.IsTransport} transportGuid={movement.TransportGuid} seat={movement.TransportSeat}" +
            sentCorrelation);
        UpdateSnapshot(_currentPlayer, _currentTarget);
    }

    private static bool TryReadPackedGuid(byte[] payload, ref int index, out ulong guid)
    {
        guid = 0;
        if ((uint)index >= (uint)payload.Length)
        {
            return false;
        }

        var mask = payload[index++];
        for (var i = 0; i < 8; i++)
        {
            if (((mask >> i) & 0x1) == 0)
            {
                continue;
            }

            if ((uint)index >= (uint)payload.Length)
            {
                return false;
            }

            guid |= (ulong)payload[index++] << (i * 8);
        }

        return true;
    }

    private static bool IsClientMovementPayloadOpcode(WorldOpcode opcode)
    {
        var value = (ushort)opcode;
        return (value >= 181 && value <= 218) || value == 238;
    }

    private static bool IsAnyMovementOpcode(WorldOpcode opcode)
    {
        var value = (ushort)opcode;
        return (value >= 181 && value <= 224) || value == 238 || value == (ushort)WorldOpcode.SmsgMonsterMoveTransport;
    }

    private static bool IsQuestOrGossipOpcode(WorldOpcode opcode)
    {
        var value = (ushort)opcode;
        return (value >= (ushort)WorldOpcode.CmsgGossipHello && value <= (ushort)WorldOpcode.SmsgQuestupdateAddItem) ||
               value == (ushort)WorldOpcode.SmsgGossipPoi ||
               value == (ushort)WorldOpcode.CmsgQuestQuery ||
               value == (ushort)WorldOpcode.SmsgQuestQueryResponse ||
               value == (ushort)WorldOpcode.CmsgQuestPoiQuery ||
               value == (ushort)WorldOpcode.SmsgQuestPoiQueryResponse ||
               value == (ushort)WorldOpcode.CmsgQuestgiverStatusMultipleQuery ||
               value == (ushort)WorldOpcode.SmsgQuestgiverStatusMultiple;
    }

    private static bool IsSocialOpcode(WorldOpcode opcode)
    {
        return opcode is WorldOpcode.SmsgGroupInvite or
            WorldOpcode.SmsgGroupCancel or
            WorldOpcode.CmsgGroupAccept or
            WorldOpcode.CmsgGroupDecline or
            WorldOpcode.SmsgGroupDecline or
            WorldOpcode.SmsgGroupSetLeader or
            WorldOpcode.SmsgGroupDestroyed or
            WorldOpcode.SmsgGroupList or
            WorldOpcode.SmsgPartyMemberStats or
            WorldOpcode.SmsgPartyMemberStatsFull or
            WorldOpcode.SmsgPartyCommandResult or
            WorldOpcode.CmsgMessagechat or
            WorldOpcode.SmsgMessagechat or
            WorldOpcode.SmsgGmMessagechat or
            WorldOpcode.CmsgEmote or
            WorldOpcode.SmsgEmote or
            WorldOpcode.CmsgTextEmote or
            WorldOpcode.SmsgTextEmote or
            WorldOpcode.SmsgChatNotInParty;
    }

    private void ApplyUpdateObject(UpdateObjectBatch batch)
    {
        var valuesCount = batch.Values.Count;
        var removedCount = batch.OutOfRangeGuids.Count;
        var updatedFieldCount = 0;
        var guidWithLevelCount = 0;
        var guidWithHealthCount = 0;
        var guidWithTargetCount = 0;
        var guidWithPositionCount = 0;
        var guidCreatureCount = 0;
        var guidPlayerCount = 0;
        var guidCorpseCount = 0;
        var guidGameObjectCount = 0;
        var guidDynamicObjectCount = 0;
        var guidQuestGiverCount = 0;
        var selfFieldsUpdated = false;
        uint? selfHealthField = null;
        uint? selfMaxHealthField = null;
        uint? selfUnitBytes1Field = null;
        uint? selfPlayerFlagsField = null;

        lock (_entitiesSync)
        {
            foreach (var guid in batch.OutOfRangeGuids)
            {
                _entities.Remove(guid);
            }

            foreach (var value in batch.Values)
            {
                if (!_entities.TryGetValue(value.Guid, out var state))
                {
                    state = new EntityState();
                    _entities[value.Guid] = state;
                }
                state.Guid = value.Guid;

                if (value.TypeId.HasValue)
                {
                    state.TypeId = value.TypeId.Value;
                }

                if (value.Movement is not null)
                {
                    state.X = value.Movement.X;
                    state.Y = value.Movement.Y;
                    state.Z = value.Movement.Z;
                    state.O = value.Movement.O;
                }

                foreach (var field in value.Fields)
                {
                    updatedFieldCount++;
                    switch (field.Key)
                    {
                        case 3: // OBJECT_FIELD_ENTRY
                            state.EntryId = field.Value;
                            break;
                        case 6: // CORPSE_FIELD_OWNER (low) on corpses
                            state.OwnerLow = field.Value;
                            break;
                        case 7: // CORPSE_FIELD_OWNER (high) on corpses
                            state.OwnerHigh = field.Value;
                            break;
                        case 18: // UNIT_FIELD_TARGET (low)
                            state.TargetLow = field.Value;
                            break;
                        case 19: // UNIT_FIELD_TARGET (high)
                            state.TargetHigh = field.Value;
                            break;
                        case 24: // UNIT_FIELD_HEALTH
                            state.Health = field.Value;
                            break;
                        case 32: // UNIT_FIELD_MAXHEALTH
                            state.MaxHealth = field.Value;
                            break;
                        case 54: // UNIT_FIELD_LEVEL
                            state.Level = (int)field.Value;
                            break;
                        case 55: // UNIT_FIELD_FACTIONTEMPLATE
                            state.FactionTemplateId = field.Value;
                            break;
                        case 59: // UNIT_FIELD_FLAGS
                            state.UnitFlags = field.Value;
                            break;
                        case 60: // UNIT_FIELD_FLAGS_2
                            state.UnitFlags2 = field.Value;
                            break;
                        case 74: // UNIT_FIELD_BYTES_1
                            state.UnitBytes1 = field.Value;
                            break;
                        case 79: // UNIT_DYNAMIC_FLAGS
                            state.DynamicFlags = field.Value;
                            break;
                        case 82: // UNIT_NPC_FLAGS
                            state.NpcFlags = field.Value;
                            break;
                        case 150: // PLAYER_FLAGS
                            state.PlayerFlags = field.Value;
                            break;
                    }

                    if (_currentCharacter is not null && value.Guid == _currentCharacter.Guid)
                    {
                        selfFieldsUpdated = true;
                        switch (field.Key)
                        {
                            case 24:
                                selfHealthField = field.Value;
                                break;
                            case 32:
                                selfMaxHealthField = field.Value;
                                break;
                            case 74:
                                selfUnitBytes1Field = field.Value;
                                break;
                            case 150:
                                selfPlayerFlagsField = field.Value;
                                break;
                        }
                    }
                }

                if (state.Level.HasValue)
                {
                    guidWithLevelCount++;
                }

                if (state.MaxHealth > 0 || state.Health > 0)
                {
                    guidWithHealthCount++;
                }

                if (state.TargetGuid != 0)
                {
                    guidWithTargetCount++;
                }

                if (state.HasPosition)
                {
                    guidWithPositionCount++;
                }

                if (state.IsCreature)
                {
                    guidCreatureCount++;
                }

                if (state.IsPlayer)
                {
                    guidPlayerCount++;
                }

                if (state.IsCorpse)
                {
                    guidCorpseCount++;
                }

                if (state.IsGameObject)
                {
                    guidGameObjectCount++;
                }

                if (state.IsDynamicObject)
                {
                    guidDynamicObjectCount++;
                }

                if (state.IsQuestGiver)
                {
                    guidQuestGiverCount++;
                }
            }

            if (_currentCharacter is null || _currentPlayer is null)
            {
                Trace.WriteLine(
                    $"NET WORLD UPDATE_OBJECT values={valuesCount} removed={removedCount} fields={updatedFieldCount} entities={_entities.Count} levels={guidWithLevelCount} health={guidWithHealthCount} targets={guidWithTargetCount} pos={guidWithPositionCount} creatures={guidCreatureCount} players={guidPlayerCount} corpses={guidCorpseCount} gameObjects={guidGameObjectCount} dynObjects={guidDynamicObjectCount} questGivers={guidQuestGiverCount} (no current character)");
                return;
            }

            if (_entities.TryGetValue(_currentCharacter.Guid, out var playerState))
            {
                if (selfFieldsUpdated)
                {
                    Trace.WriteLine(
                        $"NET WORLD SELF_FIELDS_UPDATE " +
                        $"healthRaw={(selfHealthField?.ToString() ?? "n/a")} " +
                        $"maxHealthRaw={(selfMaxHealthField?.ToString() ?? "n/a")} " +
                        $"unitBytes1Raw=0x{(selfUnitBytes1Field?.ToString("X8") ?? "n/a")} " +
                        $"playerFlagsRaw=0x{(selfPlayerFlagsField?.ToString("X8") ?? "n/a")}");
                }

                if (_lastSelfRawHealth != playerState.Health ||
                    _lastSelfRawMaxHealth != playerState.MaxHealth ||
                    _lastSelfRawPlayerFlags != playerState.PlayerFlags ||
                    _lastSelfRawUnitBytes1 != playerState.UnitBytes1)
                {
                    Trace.WriteLine(
                        $"NET WORLD SELF_STATE_RAW " +
                        $"health={playerState.Health} maxHealth={playerState.MaxHealth} " +
                        $"playerFlags=0x{playerState.PlayerFlags:X8} ghost={playerState.IsGhostPlayerFlag} " +
                        $"unitBytes1=0x{playerState.UnitBytes1:X8} standState={playerState.StandState} " +
                        $"deadHint={playerState.IsDeadHint} typeId={(playerState.TypeId?.ToString() ?? "n/a")}");
                    _lastSelfRawHealth = playerState.Health;
                    _lastSelfRawMaxHealth = playerState.MaxHealth;
                    _lastSelfRawPlayerFlags = playerState.PlayerFlags;
                    _lastSelfRawUnitBytes1 = playerState.UnitBytes1;
                }

                var now = Environment.TickCount64;
                var serverX = playerState.X ?? _currentPlayer.X;
                var serverY = playerState.Y ?? _currentPlayer.Y;
                var serverZ = playerState.Z ?? _currentPlayer.Z;

                var deathReleaseLocation = _deathReleaseLocation;
                var deathReleaseAuthorityActive =
                    deathReleaseLocation is not null &&
                    now <= _deathReleaseLocationUntilTick &&
                    (_deathReleaseMapId < 0 || _currentMapId < 0 || _deathReleaseMapId == _currentMapId);
                if (deathReleaseAuthorityActive)
                {
                    var release = deathReleaseLocation!;
                    serverX = release.X;
                    serverY = release.Y;
                    serverZ = release.Z;
                }

                var healthAlive = playerState.MaxHealth > 0 && playerState.Health > 0;
                if (playerState.IsGhostPlayerFlag)
                {
                    _serverGhostHintUntilTick = Math.Max(_serverGhostHintUntilTick, now + 20000);
                    _serverDeathHintUntilTick = Math.Max(_serverDeathHintUntilTick, now + 20000);
                    _serverAliveHintUntilTick = 0;
                }
                else if (healthAlive && !playerState.IsDeadHint)
                {
                    _serverAliveHintUntilTick = Math.Max(_serverAliveHintUntilTick, now + 5000);
                    _serverGhostHintUntilTick = 0;
                    _serverDeathHintUntilTick = 0;
                    _corpseReclaimBlockedUntilTick = 0;
                    _deathReleaseLocation = null;
                    _deathReleaseMapId = -1;
                    _deathReleaseLocationUntilTick = 0;
                }

                var ghostHintActive = now <= _serverGhostHintUntilTick;
                var deathHintActive = now <= _serverDeathHintUntilTick;
                var aliveHintActive = now <= _serverAliveHintUntilTick;
                var derivedGhost = playerState.IsGhostPlayerFlag || (ghostHintActive && !aliveHintActive);
                var derivedDead =
                    derivedGhost ||
                    playerState.IsDeadHint ||
                    (deathHintActive && !aliveHintActive) ||
                    (!aliveHintActive && playerState.MaxHealth > 0 && playerState.Health == 0);
                if (derivedGhost || derivedDead)
                {
                    if (!_deathMovementResetApplied && _isMoveForwardActive)
                    {
                        ResetMovementStateForServerRelocation("DEATH_STATE_SELF_UPDATE");
                    }

                    _deathMovementResetApplied = true;
                }
                else
                {
                    _deathMovementResetApplied = false;
                }

                var serverPosition = new NavigationPoint(serverX, serverY, serverZ);
                var serverPositionChanged = _lastUpdateObjectSelfPosition is null ||
                    DistanceSquared(
                        _lastUpdateObjectSelfPosition.X,
                        _lastUpdateObjectSelfPosition.Y,
                        _lastUpdateObjectSelfPosition.Z,
                        serverPosition.X,
                        serverPosition.Y,
                        serverPosition.Z) > 0.0025f; // > 5cm
                if (serverPositionChanged)
                {
                    _lastUpdateObjectSelfPosition = serverPosition;
                    _lastUpdateObjectSelfPositionChangeAtTick = now;
                }

                var serverPositionAgeMs = _lastUpdateObjectSelfPositionChangeAtTick == 0
                    ? long.MaxValue
                    : now - _lastUpdateObjectSelfPositionChangeAtTick;
                var serverPositionFresh = serverPositionAgeMs <= SelfUpdateObjectPositionFreshMs;
                _serverObservedSelfPosition = serverPosition;
                var serverDistanceSq = DistanceSquared(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z, serverX, serverY, serverZ);
                var serverDistance = MathF.Sqrt(serverDistanceSq);
                var localBeforeConvergence = new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z);
                var recentlyMoving = _isMoveForwardActive && (now - _lastMovementCommandAtTick) <= 1500;
                var serverRollbackLikely = serverDistanceSq > 9.0f; // > 3.0m
                var sent = _lastSentMovement;
                var recv = _lastReceivedSelfMovement;
                _lastSelfAuthorityDrift = new SelfAuthorityDriftTelemetry(
                    now,
                    serverX,
                    serverY,
                    serverZ,
                    _currentPlayer.X,
                    _currentPlayer.Y,
                    _currentPlayer.Z,
                    serverDistance,
                    recentlyMoving,
                    serverRollbackLikely,
                    sent?.Opcode,
                    sent?.MovementTimeMs,
                    sent is MovementTelemetry s ? now - s.LocalTickMs : null,
                    recv?.Opcode,
                    recv?.MovementTimeMs,
                    recv is MovementTelemetry r ? now - r.LocalTickMs : null);

                var sentInfo = "sent=none";
                if (sent is MovementTelemetry sentTelemetry)
                {
                    var sentAgeMs = now - sentTelemetry.LocalTickMs;
                    var serverDistFromSent = MathF.Sqrt(DistanceSquared(serverX, serverY, serverZ, sentTelemetry.X, sentTelemetry.Y, sentTelemetry.Z));
                    var localDistFromSent = MathF.Sqrt(DistanceSquared(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z, sentTelemetry.X, sentTelemetry.Y, sentTelemetry.Z));
                    sentInfo =
                        $"sent(op={sentTelemetry.Opcode},moveTime={FormatMovementTime(sentTelemetry.MovementTimeMs)},ageMs={sentAgeMs}," +
                        $"serverDist={serverDistFromSent:F3},localDist={localDistFromSent:F3})";
                }

                var recvInfo = "recv=none";
                if (recv is MovementTelemetry recvTelemetry)
                {
                    var recvAgeMs = now - recvTelemetry.LocalTickMs;
                    var serverDistFromRecv = MathF.Sqrt(DistanceSquared(serverX, serverY, serverZ, recvTelemetry.X, recvTelemetry.Y, recvTelemetry.Z));
                    var localDistFromRecv = MathF.Sqrt(DistanceSquared(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z, recvTelemetry.X, recvTelemetry.Y, recvTelemetry.Z));
                    recvInfo =
                        $"recv(op={recvTelemetry.Opcode},moveTime={FormatMovementTime(recvTelemetry.MovementTimeMs)},ageMs={recvAgeMs}," +
                        $"serverDist={serverDistFromRecv:F3},localDist={localDistFromRecv:F3})";
                }

                Trace.WriteLine(
                    $"NET WORLD SELF_DRIFT " +
                    $"server=({serverX:F3},{serverY:F3},{serverZ:F3}) " +
                    $"local=({_currentPlayer.X:F3},{_currentPlayer.Y:F3},{_currentPlayer.Z:F3}) " +
                    $"dist={serverDistance:F3} recentlyMoving={recentlyMoving} largeJump={serverRollbackLikely} " +
                    $"uoFresh={serverPositionFresh} uoAgeMs={(serverPositionAgeMs == long.MaxValue ? "n/a" : serverPositionAgeMs)} " +
                    $"deathReleaseAuthority={deathReleaseAuthorityActive} " +
                    $"{sentInfo} {recvInfo}");

                float? targetDistance = null;
                if (_currentTarget is TargetSnapshot target &&
                    target.X.HasValue &&
                    target.Y.HasValue &&
                    target.Z.HasValue)
                {
                    targetDistance = MathF.Sqrt(
                        DistanceSquared(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z, target.X.Value, target.Y.Value, target.Z.Value));
                }
                else
                {
                    var nearestPlayer = _entities
                        .Where(x => x.Key != _currentCharacter.Guid)
                        .Where(x => x.Value.IsPlayer && x.Value.HasPosition)
                        .Select(x => x.Value)
                        .OrderBy(x => x.DistanceSquaredTo(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z))
                        .FirstOrDefault();
                    if (nearestPlayer is not null && nearestPlayer.X.HasValue && nearestPlayer.Y.HasValue && nearestPlayer.Z.HasValue)
                    {
                        targetDistance = MathF.Sqrt(
                            DistanceSquared(
                                _currentPlayer.X,
                                _currentPlayer.Y,
                                _currentPlayer.Z,
                                nearestPlayer.X.Value,
                                nearestPlayer.Y.Value,
                                nearestPlayer.Z.Value));
                    }
                }

                var sentAgeForDecisionMs = sent is MovementTelemetry sentTelemetryForDecision
                    ? now - sentTelemetryForDecision.LocalTickMs
                    : long.MaxValue;
                var recentSelfMovementEcho = _lastReceivedSelfMovement is MovementTelemetry recvSelfForDecision &&
                                             now - recvSelfForDecision.LocalTickMs <= 1200;
                var recentSentMovementAuthority = sentAgeForDecisionMs <= 900;
                var recentSelfMovementAuthority = recentSelfMovementEcho || recentSentMovementAuthority;
                var movementAuthoritySource = recentSelfMovementEcho
                    ? "server-move-echo"
                    : recentSentMovementAuthority
                        ? "recent-client-send"
                        : "none";
                bool hardCorrectionDuringMove;
                if (recentlyMoving && serverPositionFresh)
                {
                    if (recentSelfMovementAuthority &&
                        (serverDistance >= MovingHardCorrectionDistance || serverRollbackLikely))
                    {
                        _movingServerCorrectionStreak++;
                    }
                    else
                    {
                        _movingServerCorrectionStreak = 0;
                    }

                    hardCorrectionDuringMove =
                        serverRollbackLikely ||
                        _movingServerCorrectionStreak >= MovingHardCorrectionStreakThreshold;
                }
                else
                {
                    _movingServerCorrectionStreak = 0;
                    hardCorrectionDuringMove = false;
                }

                var staleUpdateObjectForSelf =
                    !serverPositionFresh &&
                    serverDistance >= SelfUpdateObjectStaleDistance;
                // Apply hard snap only during the short release-location authority window.
                // Outside that window, treat UPDATE_OBJECT like normal authority with stale-reject
                // to avoid backward ghost ping-pong.
                var forceDeathAuthoritySnap = deathReleaseAuthorityActive && (derivedGhost || derivedDead);
                var forceDeathServerSnap = forceDeathAuthoritySnap;
                // Trinity can correct our position via UPDATE_OBJECT without echoing back self movement.
                // Keep rejecting stale snapshots, but accept fresh self updates when they correlate with
                // either a recent echoed movement or a recent movement packet we just sent.
                var allowMovingConvergence = !recentlyMoving || (serverPositionFresh && recentSelfMovementAuthority && hardCorrectionDuringMove);
                var applyServerConvergence = forceDeathServerSnap || (!staleUpdateObjectForSelf && allowMovingConvergence);
                var converged = forceDeathServerSnap
                    ? serverPosition
                    : applyServerConvergence
                        ? ConvergeToServerPosition(
                            new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z),
                            serverPosition,
                            recentlyMoving,
                            serverRollbackLikely,
                            _currentRunSpeed)
                        : new NavigationPoint(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z);
                var convergenceDelta = MathF.Sqrt(DistanceSquared(_currentPlayer.X, _currentPlayer.Y, _currentPlayer.Z, converged.X, converged.Y, converged.Z));

                Trace.WriteLine(
                    $"NET WORLD SELF_CONVERGENCE " +
                    $"applied={convergenceDelta:F3} mode={(forceDeathServerSnap ? "death-server-snap" : applyServerConvergence ? "converge" : "local-moving")} " +
                    $"forceDeathSnap={forceDeathServerSnap} " +
                    $"deathReleaseAuthoritySnap={forceDeathAuthoritySnap} " +
                    $"recentSelfMoveAuthority={recentSelfMovementAuthority} " +
                    $"authoritySource={movementAuthoritySource} " +
                    $"hardMoveCorrection={hardCorrectionDuringMove} streak={_movingServerCorrectionStreak} " +
                    $"staleUpdateObjectReject={staleUpdateObjectForSelf} " +
                    $"sentAgeMs={(sentAgeForDecisionMs == long.MaxValue ? "n/a" : sentAgeForDecisionMs)} targetDist={(targetDistance?.ToString("F3") ?? "n/a")} " +
                    $"next=({converged.X:F3},{converged.Y:F3},{converged.Z:F3})");
                LogServerForcedPosition(
                    WorldOpcode.SmsgUpdateObject,
                    "UPDATE_OBJECT",
                    localBeforeConvergence,
                    serverPosition,
                    now,
                    null);

                Trace.WriteLine(
                    $"NET WORLD SELF_DEATH_DERIVE " +
                    $"explicitGhost={playerState.IsGhostPlayerFlag} explicitDead={playerState.IsDeadHint} " +
                    $"health={playerState.Health}/{playerState.MaxHealth} " +
                    $"ghostHintActive={ghostHintActive} deathHintActive={deathHintActive} aliveHintActive={aliveHintActive} " +
                    $"derivedGhost={derivedGhost} derivedDead={derivedDead}");

                _currentPlayer = _currentPlayer with
                {
                    Level = playerState.Level ?? _currentPlayer.Level,
                    FactionTemplateId = playerState.FactionTemplateId > 0
                        ? (int)playerState.FactionTemplateId
                        : _currentPlayer.FactionTemplateId,
                    X = converged.X,
                    Y = converged.Y,
                    Z = converged.Z,
                    Orientation = _currentOrientation,
                    HealthPercent = playerState.HealthPercent ?? _currentPlayer.HealthPercent,
                    IsGhost = derivedGhost,
                    IsDeadHint = derivedDead
                };
                if (serverDistance <= 0.5f)
                {
                    _currentOrientation = playerState.O ?? _currentOrientation;
                }

                var currentTargetGuid = playerState.TargetGuid;
                if (currentTargetGuid == 0)
                {
                    _currentTarget = null;
                }
                else
                {
                    _entities.TryGetValue(currentTargetGuid, out var targetState);
                    float? targetX = null;
                    float? targetY = null;
                    float? targetZ = null;
                    if (targetState is not null && targetState.TryGetProjectedPosition(now, out var projectedTarget))
                    {
                        targetX = projectedTarget.X;
                        targetY = projectedTarget.Y;
                        targetZ = projectedTarget.Z;
                    }

                    _currentTarget = new TargetSnapshot(
                        currentTargetGuid,
                        null,
                        targetState?.Level,
                        targetX,
                        targetY,
                        targetZ,
                        targetState?.HealthPercent);
                }
            }
        }

        Trace.WriteLine(
            $"NET WORLD UPDATE_OBJECT values={valuesCount} removed={removedCount} fields={updatedFieldCount} entities={_entities.Count} levels={guidWithLevelCount} health={guidWithHealthCount} targets={guidWithTargetCount} pos={guidWithPositionCount} creatures={guidCreatureCount} players={guidPlayerCount} corpses={guidCorpseCount} gameObjects={guidGameObjectCount} dynObjects={guidDynamicObjectCount} questGivers={guidQuestGiverCount} currentTarget={_currentTarget?.Guid ?? 0}");
        if (_lastCorpseEntityCount != guidCorpseCount)
        {
            Trace.WriteLine($"NET WORLD CORPSE_COUNT_CHANGED from={_lastCorpseEntityCount} to={guidCorpseCount}");
            _lastCorpseEntityCount = guidCorpseCount;
        }
        UpdateSnapshot(_currentPlayer, _currentTarget);
    }

    private void ResetMovementStateForServerRelocation(string reason)
    {
        CancellationTokenSource? activeMovementCts;
        lock (_movementCommandSync)
        {
            activeMovementCts = _activeMovementCommandCts;
        }

        if (activeMovementCts is not null && !activeMovementCts.IsCancellationRequested)
        {
            activeMovementCts.Cancel();
        }

        _isMoveForwardActive = false;
        _lastMoveStartSentAtTick = 0;
        _lastMoveStopSentAtTick = 0;
        _movingServerCorrectionStreak = 0;
        lock (_entitiesSync)
        {
            _lastSentMovement = null;
        }

        Trace.WriteLine($"NET WORLD MOVE STATE_RESET reason={reason} {BuildMovementCommandLog()}");
    }

    private async Task SendMovementPacketAsync(
        WorldOpcode opcode,
        NavigationPoint point,
        MovementPacketCodec.MovementFlags flags,
        CancellationToken cancellationToken,
        float? forcedOrientation = null)
    {
        if (_worldConnection is null || _currentCharacter is null)
        {
            return;
        }

        var player = _currentPlayer;
        if (player is null)
        {
            return;
        }

        var nowTick = Environment.TickCount64;
        if (opcode == WorldOpcode.MsgMoveStartForward && _isMoveForwardActive)
        {
            Trace.WriteLine("NET WORLD MOVE SEND_SKIP opcode=MsgMoveStartForward reason=already-active");
            return;
        }

        if (opcode == WorldOpcode.MsgMoveStop &&
            !_isMoveForwardActive &&
            nowTick - _lastMoveStopSentAtTick <= DuplicateMovementCommandWindowMs)
        {
            Trace.WriteLine("NET WORLD MOVE SEND_SKIP opcode=MsgMoveStop reason=duplicate-stop");
            return;
        }

        if (forcedOrientation.HasValue)
        {
            _currentOrientation = forcedOrientation.Value;
        }
        else
        {
            var dx = point.X - player.X;
            var dy = point.Y - player.Y;
            if (Math.Abs(dx) > 0.001f || Math.Abs(dy) > 0.001f)
            {
                _currentOrientation = MathF.Atan2(dy, dx);
            }
        }

        var movementTime = unchecked((uint)Environment.TickCount);
        var payload = MovementPacketCodec.BuildMovementPayload(
            _currentCharacter.Guid,
            flags,
            movementTime,
            point.X,
            point.Y,
            point.Z,
            _currentOrientation);

        Trace.WriteLine(
            $"NET WORLD MOVE SEND {BuildMovementCommandLog()} opcode={opcode} flags={flags} moveTime={movementTime} pos=({point.X:F3},{point.Y:F3},{point.Z:F3},{_currentOrientation:F3})");
        await SendWorldPacketAsync(_worldConnection, opcode, payload, cancellationToken);
        _lastMovementCommandAtTick = nowTick;
        lock (_entitiesSync)
        {
            _lastSentMovement = new MovementTelemetry(opcode, movementTime, point.X, point.Y, point.Z, _lastMovementCommandAtTick);
            if (_currentCharacter is not null)
            {
                if (!_entities.TryGetValue(_currentCharacter.Guid, out var selfState))
                {
                    selfState = new EntityState
                    {
                        Guid = _currentCharacter.Guid,
                        TypeId = 4
                    };
                    _entities[_currentCharacter.Guid] = selfState;
                }

                selfState.X = point.X;
                selfState.Y = point.Y;
                selfState.Z = point.Z;
                selfState.O = _currentOrientation;
                selfState.HasActiveSpline = false;
            }
        }
        if (opcode == WorldOpcode.MsgMoveStartForward ||
            (opcode == WorldOpcode.MsgMoveHeartbeat && flags.HasFlag(MovementPacketCodec.MovementFlags.Forward)))
        {
            _isMoveForwardActive = true;
            _lastMoveStartSentAtTick = nowTick;
        }
        else if (opcode == WorldOpcode.MsgMoveStop)
        {
            _isMoveForwardActive = false;
            _lastMoveStopSentAtTick = nowTick;
        }

        _currentPlayer = player with { X = point.X, Y = point.Y, Z = point.Z };
        UpdateSnapshot(_currentPlayer, _currentTarget);
    }

    private static float ClampZ(float currentZ, float desiredZ, float maxStep)
    {
        var delta = desiredZ - currentZ;
        if (delta > maxStep)
        {
            return currentZ + maxStep;
        }

        if (delta < -maxStep)
        {
            return currentZ - maxStep;
        }

        return desiredZ;
    }

    private static float DistanceSquared(float ax, float ay, float az, float bx, float by, float bz)
    {
        var dx = ax - bx;
        var dy = ay - by;
        var dz = az - bz;
        return (dx * dx) + (dy * dy) + (dz * dz);
    }

    private static int MovementTimeDelta(uint current, uint reference)
    {
        return unchecked((int)(current - reference));
    }

    private static NavigationPoint ConvergeToServerPosition(
        NavigationPoint local,
        NavigationPoint server,
        bool recentlyMoving,
        bool largeJump,
        float runSpeed)
    {
        var dist = MathF.Sqrt(DistanceSquared(local.X, local.Y, local.Z, server.X, server.Y, server.Z));
        if (dist <= 0.05f)
        {
            return server;
        }

        // Keep straight-line motion smooth: while moving, ignore tiny/mid drifts and
        // converge slowly; when idle, converge faster to authority.
        if (recentlyMoving && dist < 1.25f)
        {
            return local;
        }

        var alpha = recentlyMoving ? (dist >= 3.0f ? 0.10f : 0.06f) : 0.35f;
        if (largeJump)
        {
            alpha = recentlyMoving ? 0.14f : 0.55f;
        }
        var target = new NavigationPoint(
            local.X + ((server.X - local.X) * alpha),
            local.Y + ((server.Y - local.Y) * alpha),
            local.Z + ((server.Z - local.Z) * alpha));

        var maxStepXy = MathF.Max(0.12f, runSpeed * (recentlyMoving ? 0.03f : 0.10f));
        if (largeJump)
        {
            maxStepXy = MathF.Max(maxStepXy, recentlyMoving ? 0.45f : 1.40f);
        }

        var xyStepped = MoveTowards(
            new NavigationPoint(local.X, local.Y, 0f),
            new NavigationPoint(target.X, target.Y, 0f),
            maxStepXy);

        var maxStepZ = recentlyMoving ? 0.05f : 0.18f;
        if (largeJump)
        {
            maxStepZ = recentlyMoving ? 0.15f : 0.65f;
        }

        var zStepped = ClampZ(local.Z, target.Z, maxStepZ);
        return new NavigationPoint(xyStepped.X, xyStepped.Y, zStepped);
    }

    private static string FormatMovementTime(uint? movementTimeMs)
    {
        return movementTimeMs?.ToString() ?? "n/a";
    }

    private void TraceOutgoingPacketValidation(WorldOpcode opcode, byte[] payload, byte[] frame)
    {
        var currentSample = _outgoingValidationSamples.TryGetValue(opcode, out var sampled)
            ? sampled + 1
            : 1;
        _outgoingValidationSamples[opcode] = currentSample;
        if (currentSample > OutgoingValidationSampleLimitPerOpcode)
        {
            return;
        }

        var plainHeaderSize = payload.Length + 4;
        var framePreview = Convert.ToHexString(frame.AsSpan(0, Math.Min(frame.Length, OutgoingValidationHexPreviewBytes)));
        var payloadPreview = payload.Length > 0
            ? Convert.ToHexString(payload.AsSpan(0, Math.Min(payload.Length, OutgoingValidationHexPreviewBytes)))
            : "empty";
        Trace.WriteLine(
            $"NET WORLD SEND_VALIDATE opcode={opcode} sample={currentSample} payload={payload.Length} frame={frame.Length} " +
            $"headerPlain(sizeBE={plainHeaderSize},cmdLE=0x{((uint)opcode):X8}) frameHex={framePreview} payloadHex={payloadPreview}");

        switch (opcode)
        {
            case WorldOpcode.MsgMoveStartForward:
            case WorldOpcode.MsgMoveStop:
            case WorldOpcode.MsgMoveHeartbeat:
            {
                if (MovementPacketCodec.TryParseMovementPayload(payload, out var movement))
                {
                    Trace.WriteLine(
                        $"NET WORLD SEND_VALIDATE MOVE opcode={opcode} guid={movement.Guid} " +
                        $"flags={(uint)movement.Flags} flags2={movement.ExtraFlags} time={movement.MovementTimeMs} fall={movement.FallTimeMs} " +
                        $"pos=({movement.X:F3},{movement.Y:F3},{movement.Z:F3},{movement.Orientation:F3})");
                }
                else
                {
                    Trace.WriteLine($"NET WORLD SEND_VALIDATE MOVE opcode={opcode} parse=failed");
                }

                break;
            }
            case WorldOpcode.CmsgForceRunSpeedChangeAck:
            case WorldOpcode.CmsgForceRunBackSpeedChangeAck:
            case WorldOpcode.CmsgForceSwimSpeedChangeAck:
            case WorldOpcode.CmsgForceWalkSpeedChangeAck:
            case WorldOpcode.CmsgForceSwimBackSpeedChangeAck:
            case WorldOpcode.CmsgForceTurnRateChangeAck:
            case WorldOpcode.CmsgForceFlightSpeedChangeAck:
            case WorldOpcode.CmsgForceFlightBackSpeedChangeAck:
            case WorldOpcode.CmsgForcePitchRateChangeAck:
            {
                if (MovementPacketCodec.TryParseForceSpeedChangeAckPayload(payload, out var movementCounter, out var movement, out var runSpeed))
                {
                    Trace.WriteLine(
                        $"NET WORLD SEND_VALIDATE SPEED_ACK opcode={opcode} counter={movementCounter} speed={runSpeed:F3} guid={movement.Guid} " +
                        $"flags={(uint)movement.Flags} flags2={movement.ExtraFlags} time={movement.MovementTimeMs} " +
                        $"pos=({movement.X:F3},{movement.Y:F3},{movement.Z:F3},{movement.Orientation:F3})");
                }
                else
                {
                    Trace.WriteLine($"NET WORLD SEND_VALIDATE SPEED_ACK opcode={opcode} parse=failed");
                }

                break;
            }
            case WorldOpcode.CmsgTimeSyncResp:
            {
                if (payload.Length >= 8)
                {
                    var counter = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    var clientTimestamp = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(4, 4));
                    Trace.WriteLine(
                        $"NET WORLD SEND_VALIDATE TIME_SYNC_RESP counter={counter} clientTimestamp={clientTimestamp}");
                }

                break;
            }
            case WorldOpcode.CmsgSetActiveMover:
            case WorldOpcode.CmsgPlayerLogin:
            case WorldOpcode.CmsgSetSelection:
            case WorldOpcode.CmsgAttackSwing:
            {
                if (payload.Length >= 8)
                {
                    var guid = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(0, 8));
                    Trace.WriteLine($"NET WORLD SEND_VALIDATE GUID_PAYLOAD opcode={opcode} guid={guid}");
                }

                break;
            }
            case WorldOpcode.CmsgKeepAlive:
                Trace.WriteLine($"NET WORLD SEND_VALIDATE KEEP_ALIVE payload={payload.Length}");
                break;
            case WorldOpcode.CmsgAttackStop:
                Trace.WriteLine($"NET WORLD SEND_VALIDATE ATTACK_STOP payload={payload.Length}");
                break;
            case WorldOpcode.CmsgGroupAccept:
                if (payload.Length >= 4)
                {
                    var roles = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    Trace.WriteLine($"NET WORLD SEND_VALIDATE GROUP_ACCEPT roles=0x{roles:X8}");
                }

                break;
            case WorldOpcode.CmsgGroupDecline:
                Trace.WriteLine("NET WORLD SEND_VALIDATE GROUP_DECLINE");
                break;
            case WorldOpcode.CmsgMessagechat:
            {
                if (payload.Length >= 8)
                {
                    var type = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    var language = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(4, 4));
                    Trace.WriteLine(
                        $"NET WORLD SEND_VALIDATE CHAT type=0x{type:X2} language=0x{language:X8} payloadHex={payloadPreview}");
                }

                break;
            }
            case WorldOpcode.CmsgEmote:
                if (payload.Length >= 4)
                {
                    var emoteId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    Trace.WriteLine($"NET WORLD SEND_VALIDATE EMOTE emoteId={emoteId}");
                }

                break;
            case WorldOpcode.CmsgTextEmote:
                if (payload.Length >= 16)
                {
                    var textEmoteId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    var emoteNum = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(4, 4));
                    var targetGuid = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(8, 8));
                    Trace.WriteLine(
                        $"NET WORLD SEND_VALIDATE TEXT_EMOTE textEmoteId={textEmoteId} emoteNum={emoteNum} targetGuid={targetGuid}");
                }

                break;
            case WorldOpcode.CmsgQuestgiverStatusQuery:
            case WorldOpcode.CmsgQuestgiverHello:
            case WorldOpcode.CmsgQuestgiverRequestReward:
            {
                if (payload.Length >= 8)
                {
                    var guid = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(0, 8));
                    Trace.WriteLine($"NET WORLD SEND_VALIDATE QUEST_GUID opcode={opcode} guid={guid}");
                }

                break;
            }
            case WorldOpcode.CmsgQuestgiverQueryQuest:
            case WorldOpcode.CmsgQuestgiverAcceptQuest:
            case WorldOpcode.CmsgQuestgiverCompleteQuest:
            {
                if (payload.Length >= 12)
                {
                    var guid = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(0, 8));
                    var questId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(8, 4));
                    Trace.WriteLine($"NET WORLD SEND_VALIDATE QUEST_TARGET opcode={opcode} guid={guid} questId={questId}");
                }

                break;
            }
            case WorldOpcode.CmsgQuestgiverChooseReward:
            {
                if (payload.Length >= 16)
                {
                    var guid = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(0, 8));
                    var questId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(8, 4));
                    var rewardIndex = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(12, 4));
                    Trace.WriteLine($"NET WORLD SEND_VALIDATE QUEST_REWARD opcode={opcode} guid={guid} questId={questId} rewardIndex={rewardIndex}");
                }

                break;
            }
            case WorldOpcode.CmsgQuestQuery:
            {
                if (payload.Length >= 4)
                {
                    var questId = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    Trace.WriteLine($"NET WORLD SEND_VALIDATE QUEST_QUERY questId={questId}");
                }

                break;
            }
            case WorldOpcode.CmsgQuestPoiQuery:
            {
                if (payload.Length >= 4)
                {
                    var count = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, 4));
                    var questIds = new List<uint>((int)Math.Min(count, 32));
                    var availableCount = Math.Min((payload.Length - 4) / 4, (int)count);
                    for (var i = 0; i < availableCount; i++)
                    {
                        questIds.Add(BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(4 + (i * 4), 4)));
                    }

                    Trace.WriteLine(
                        $"NET WORLD SEND_VALIDATE QUEST_POI_QUERY count={count} parsed={availableCount} questIds={string.Join(",", questIds)}");
                }

                break;
            }
            case WorldOpcode.CmsgQuestgiverStatusMultipleQuery:
                Trace.WriteLine($"NET WORLD SEND_VALIDATE QUEST_STATUS_MULTIPLE payload={payload.Length}");
                break;
        }
    }

    private void LogServerForcedPosition(
        WorldOpcode sourceOpcode,
        string source,
        NavigationPoint localBefore,
        NavigationPoint serverPosition,
        long nowTickMs,
        uint? recvMovementTimeMs)
    {
        var forcedDistance = MathF.Sqrt(DistanceSquared(
            localBefore.X,
            localBefore.Y,
            localBefore.Z,
            serverPosition.X,
            serverPosition.Y,
            serverPosition.Z));
        if (forcedDistance < ServerForceLogThreshold)
        {
            return;
        }

        var severity = forcedDistance >= 1.0f
            ? "hard"
            : forcedDistance >= 0.20f
                ? "soft"
                : "micro";

        var sentInfo = "sent=none";
        if (_lastSentMovement is MovementTelemetry sent)
        {
            var sentAge = nowTickMs - sent.LocalTickMs;
            var fromSent = MathF.Sqrt(DistanceSquared(serverPosition.X, serverPosition.Y, serverPosition.Z, sent.X, sent.Y, sent.Z));
            sentInfo = $"sent(op={sent.Opcode},moveTime={FormatMovementTime(sent.MovementTimeMs)},ageMs={sentAge},serverDist={fromSent:F3})";
        }

        var recvInfo = "recv=none";
        if (_lastReceivedSelfMovement is MovementTelemetry recv)
        {
            var recvAge = nowTickMs - recv.LocalTickMs;
            var fromRecv = MathF.Sqrt(DistanceSquared(serverPosition.X, serverPosition.Y, serverPosition.Z, recv.X, recv.Y, recv.Z));
            recvInfo = $"recv(op={recv.Opcode},moveTime={FormatMovementTime(recv.MovementTimeMs)},ageMs={recvAge},serverDist={fromRecv:F3})";
        }

        Trace.WriteLine(
            $"NET WORLD SERVER_FORCE severity={severity} source={source} opcode={sourceOpcode} " +
            $"delta={forcedDistance:F3} local=({localBefore.X:F3},{localBefore.Y:F3},{localBefore.Z:F3}) " +
            $"server=({serverPosition.X:F3},{serverPosition.Y:F3},{serverPosition.Z:F3}) " +
            $"recentlyMoving={_isMoveForwardActive} recvMoveTime={FormatMovementTime(recvMovementTimeMs)} " +
            $"{BuildMovementCommandLog()} " +
            $"{sentInfo} {recvInfo}");
    }

    private static NavigationPoint MoveTowards(NavigationPoint current, NavigationPoint target, float maxDistanceDelta)
    {
        var dx = target.X - current.X;
        var dy = target.Y - current.Y;
        var dz = target.Z - current.Z;
        var distance = MathF.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        if (distance <= 0.0001f || distance <= maxDistanceDelta)
        {
            return target;
        }

        var scale = maxDistanceDelta / distance;
        return new NavigationPoint(
            current.X + (dx * scale),
            current.Y + (dy * scale),
            current.Z + (dz * scale));
    }

    private (NavigationPoint Ground, string Source, float ProbeZ, float DeltaToStep, float DeltaToPlayer, float DeltaToTarget) ResolveHeartbeatGroundPoint(
        NavigationPoint currentPoint,
        NavigationPoint steppedWaypoint,
        NavigationPoint safeWaypoint,
        bool chaseMode,
        bool deathRecoveryMove)
    {
        var primaryProbe = new NavigationPoint(steppedWaypoint.X, steppedWaypoint.Y, steppedWaypoint.Z);
        var selectedGround = pathfinder.ProjectToSurface(_currentMapId, primaryProbe);
        var selectedSource = "step-z";
        var selectedProbeZ = primaryProbe.Z;

        if (!deathRecoveryMove)
        {
            var currentProbe = new NavigationPoint(steppedWaypoint.X, steppedWaypoint.Y, currentPoint.Z);
            var highProbe = new NavigationPoint(
                steppedWaypoint.X,
                steppedWaypoint.Y,
                MathF.Max(currentPoint.Z, MathF.Max(steppedWaypoint.Z, safeWaypoint.Z)) + HeartbeatGroundProbeHighOffsetZ);
            var lowProbe = new NavigationPoint(
                steppedWaypoint.X,
                steppedWaypoint.Y,
                MathF.Min(currentPoint.Z, MathF.Min(steppedWaypoint.Z, safeWaypoint.Z)) - HeartbeatGroundProbeLowOffsetZ);

            var candidates = new List<(NavigationPoint Ground, string Source, float ProbeZ, float Score)>
            {
                (pathfinder.ProjectToSurface(_currentMapId, primaryProbe), "step-z", primaryProbe.Z, 0.0f),
                (pathfinder.ProjectToSurface(_currentMapId, currentProbe), "current-z", currentProbe.Z, 0.0f),
                (pathfinder.ProjectToSurface(_currentMapId, highProbe), "high-z", highProbe.Z, 0.0f),
                (pathfinder.ProjectToSurface(_currentMapId, lowProbe), "low-z", lowProbe.Z, 0.0f)
            };

            var descendingIntent =
                safeWaypoint.Z < currentPoint.Z - HeartbeatGroundDirectionalIntentMinDeltaZ &&
                steppedWaypoint.Z < currentPoint.Z - (HeartbeatGroundDirectionalIntentMinDeltaZ * 0.5f);
            var ascendingIntent =
                safeWaypoint.Z > currentPoint.Z + HeartbeatGroundDirectionalIntentMinDeltaZ &&
                steppedWaypoint.Z > currentPoint.Z + (HeartbeatGroundDirectionalIntentMinDeltaZ * 0.5f);

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var continuity = MathF.Abs(candidate.Ground.Z - currentPoint.Z);
                var stepDelta = MathF.Abs(candidate.Ground.Z - steppedWaypoint.Z);
                var score = stepDelta + (continuity * 0.35f);

                if (candidate.Source == "step-z")
                {
                    score -= 0.05f;
                }

                if (continuity > HeartbeatGroundContinuityMaxDeltaZ)
                {
                    score += 4.0f;
                }

                if (candidate.Source == "current-z" &&
                    stepDelta > HeartbeatGroundCurrentFallbackPenaltyDeltaZ)
                {
                    score += 0.45f;
                }

                if (chaseMode)
                {
                    if (!string.Equals(candidate.Source, "step-z", StringComparison.Ordinal))
                    {
                        score += HeartbeatGroundChaseNonStepPenalty;
                    }

                    if (stepDelta > HeartbeatGroundChaseStepDeviationPenaltyDeltaZ)
                    {
                        score += 1.20f + ((stepDelta - HeartbeatGroundChaseStepDeviationPenaltyDeltaZ) * 1.35f);
                    }

                    if (continuity > HeartbeatGroundChaseContinuityPenaltyDeltaZ)
                    {
                        score += 1.00f + ((continuity - HeartbeatGroundChaseContinuityPenaltyDeltaZ) * 1.10f);
                    }

                    if (descendingIntent &&
                        (string.Equals(candidate.Source, "high-z", StringComparison.Ordinal) ||
                         string.Equals(candidate.Source, "current-z", StringComparison.Ordinal)) &&
                        candidate.Ground.Z > steppedWaypoint.Z + 0.10f)
                    {
                        score += 1.25f;
                    }

                    if (ascendingIntent &&
                        string.Equals(candidate.Source, "low-z", StringComparison.Ordinal) &&
                        candidate.Ground.Z < steppedWaypoint.Z - 0.10f)
                    {
                        score += 1.25f;
                    }
                }

                if (descendingIntent)
                {
                    if (candidate.Ground.Z > steppedWaypoint.Z + HeartbeatGroundProbeOvershootPenaltyDeltaZ)
                    {
                        score += 0.70f + ((candidate.Ground.Z - steppedWaypoint.Z) * 0.50f);
                    }

                    if (candidate.Ground.Z > currentPoint.Z + 0.05f)
                    {
                        score += 0.50f;
                    }
                }
                else if (ascendingIntent)
                {
                    if (candidate.Ground.Z < steppedWaypoint.Z - HeartbeatGroundProbeOvershootPenaltyDeltaZ)
                    {
                        score += 0.70f + ((steppedWaypoint.Z - candidate.Ground.Z) * 0.50f);
                    }

                    if (candidate.Ground.Z < currentPoint.Z - 0.05f)
                    {
                        score += 0.50f;
                    }
                }
                else if (candidate.Ground.Z < currentPoint.Z - 0.35f)
                {
                    score += 0.35f;
                }
                else if (candidate.Ground.Z > currentPoint.Z &&
                         continuity <= HeartbeatGroundContinuityMaxDeltaZ)
                {
                    score -= 0.10f;
                }

                candidates[i] = (candidate.Ground, candidate.Source, candidate.ProbeZ, score);
            }

            var bestCandidate = candidates
                .OrderBy(x => x.Score)
                .First();
            selectedGround = bestCandidate.Ground;
            selectedSource = bestCandidate.Source;
            selectedProbeZ = bestCandidate.ProbeZ;

            if (!chaseMode &&
                selectedGround.Z < steppedWaypoint.Z - HeartbeatGroundRecoveryDeltaZ &&
                selectedGround.Z < currentPoint.Z - HeartbeatGroundRecoveryDeltaZ)
            {
                var liftedProbe = new NavigationPoint(
                    steppedWaypoint.X,
                    steppedWaypoint.Y,
                    MathF.Max(currentPoint.Z, steppedWaypoint.Z) + HeartbeatGroundProbeHighOffsetZ);
                var liftedGround = pathfinder.ProjectToSurface(_currentMapId, liftedProbe);
                if (liftedGround.Z > selectedGround.Z + HeartbeatGroundProbeUpgradeMinDeltaZ &&
                    MathF.Abs(liftedGround.Z - currentPoint.Z) <= HeartbeatGroundContinuityMaxDeltaZ)
                {
                    selectedGround = liftedGround;
                    selectedSource = "recovery-high-z";
                    selectedProbeZ = liftedProbe.Z;
                }
            }

            if (chaseMode)
            {
                var envelopeMin = MathF.Min(currentPoint.Z, MathF.Min(steppedWaypoint.Z, safeWaypoint.Z)) - HeartbeatGroundChaseEnvelopeMarginZ;
                var envelopeMax = MathF.Max(currentPoint.Z, MathF.Max(steppedWaypoint.Z, safeWaypoint.Z)) + HeartbeatGroundChaseEnvelopeMarginZ;
                var clampedZ = Math.Clamp(selectedGround.Z, envelopeMin, envelopeMax);
                if (MathF.Abs(clampedZ - selectedGround.Z) >= 0.001f)
                {
                    selectedGround = new NavigationPoint(selectedGround.X, selectedGround.Y, clampedZ);
                    selectedSource = $"{selectedSource}-clamped";
                }

                if (!string.Equals(selectedSource, "step-z", StringComparison.Ordinal) &&
                    MathF.Abs(selectedGround.Z - steppedWaypoint.Z) > HeartbeatGroundChaseStepDeviationPenaltyDeltaZ)
                {
                    var conservativeGround = pathfinder.ProjectToSurface(_currentMapId, primaryProbe);
                    var conservativeZ = Math.Clamp(conservativeGround.Z, envelopeMin, envelopeMax);
                    selectedGround = new NavigationPoint(conservativeGround.X, conservativeGround.Y, conservativeZ);
                    selectedSource = "step-z-chase-fallback";
                    selectedProbeZ = primaryProbe.Z;
                }
            }
        }

        return (
            selectedGround,
            selectedSource,
            selectedProbeZ,
            selectedGround.Z - steppedWaypoint.Z,
            selectedGround.Z - currentPoint.Z,
            selectedGround.Z - safeWaypoint.Z);
    }

    private bool CanAdvanceWaypointByVisibility(
        IReadOnlyList<NavigationPoint> path,
        int currentIndex,
        int candidateIndex,
        NavigationPoint currentPoint,
        out float segmentDistance2D,
        out float deviation2D,
        out string decision)
    {
        segmentDistance2D = 0.0f;
        deviation2D = 0.0f;
        decision = "candidate-index";
        if (candidateIndex <= currentIndex || candidateIndex >= path.Count)
        {
            return false;
        }

        var candidatePoint = path[candidateIndex];
        segmentDistance2D = Distance2D(currentPoint, candidatePoint);
        if (segmentDistance2D > VisibilityShortcutMaxDistance)
        {
            decision = "segment_length";
            return false;
        }

        deviation2D = ComputeMaxDeviationFromSegment2D(path, currentIndex, candidateIndex, currentPoint, candidatePoint);
        if (deviation2D > VisibilityShortcutMaxDeviation)
        {
            decision = "path_deviation";
            return false;
        }

        if (!pathfinder.HasLineOfSight(_currentMapId, currentPoint, candidatePoint))
        {
            decision = "los";
            return false;
        }

        decision = "ok";
        return true;
    }

    private static float ComputeMaxDeviationFromSegment2D(
        IReadOnlyList<NavigationPoint> path,
        int startIndex,
        int endIndex,
        NavigationPoint segmentStart,
        NavigationPoint segmentEnd)
    {
        if (endIndex <= startIndex + 1)
        {
            return 0.0f;
        }

        var maxDeviation = 0.0f;
        for (var i = startIndex + 1; i < endIndex; i++)
        {
            var deviation = DistancePointToSegment2D(path[i], segmentStart, segmentEnd);
            if (deviation > maxDeviation)
            {
                maxDeviation = deviation;
            }
        }

        return maxDeviation;
    }

    private static float DistancePointToSegment2D(NavigationPoint point, NavigationPoint segmentStart, NavigationPoint segmentEnd)
    {
        var dx = segmentEnd.X - segmentStart.X;
        var dy = segmentEnd.Y - segmentStart.Y;
        var lengthSq = (dx * dx) + (dy * dy);
        if (lengthSq <= 0.0001f)
        {
            return Distance2D(point, segmentStart);
        }

        var t = (((point.X - segmentStart.X) * dx) + ((point.Y - segmentStart.Y) * dy)) / lengthSq;
        t = Math.Clamp(t, 0.0f, 1.0f);
        var projection = new NavigationPoint(
            segmentStart.X + (dx * t),
            segmentStart.Y + (dy * t),
            point.Z);
        return Distance2D(point, projection);
    }

    private static float Distance2D(NavigationPoint left, NavigationPoint right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    private static int FindClosestWaypointIndex(IReadOnlyList<NavigationPoint> path, float x, float y, float z)
    {
        if (path.Count == 0)
        {
            return 0;
        }

        var bestIndex = 0;
        var bestDistanceSq = float.MaxValue;
        for (var i = 0; i < path.Count; i++)
        {
            var point = path[i];
            var distanceSq = DistanceSquared(x, y, z, point.X, point.Y, point.Z);
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private string BuildNearestPlayerLog(float playerX, float playerY, float playerZ)
    {
        lock (_entitiesSync)
        {
            var now = Environment.TickCount64;
            var selfSpeedMps = UpdateSpeed(ref _selfSpeedSample, playerX, playerY, playerZ, now);
            var nearest = _entities
                .Where(x => x.Key != _currentCharacter?.Guid)
                .Where(x => x.Value.IsPlayer && x.Value.HasPosition)
                .Select(x => new
                {
                    x.Key,
                    State = x.Value,
                    DistanceSq = x.Value.DistanceSquaredTo(playerX, playerY, playerZ)
                })
                .OrderBy(x => x.DistanceSq)
                .FirstOrDefault();

            if (nearest is null || nearest.State.X is null || nearest.State.Y is null || nearest.State.Z is null)
            {
                return "nearestPlayer=none";
            }

            var nearestSpeedMps = UpdateEntitySpeed(nearest.Key, nearest.State.X.Value, nearest.State.Y.Value, nearest.State.Z.Value, now);
            var distance = MathF.Sqrt(nearest.DistanceSq);
            var dz = nearest.State.Z.Value - playerZ;
            var speedCompare = BuildSpeedCompareLog(selfSpeedMps, nearestSpeedMps);
            return
                $"nearestPlayerGuid={nearest.Key} " +
                $"nearestPlayerPos=({nearest.State.X.Value:F3},{nearest.State.Y.Value:F3},{nearest.State.Z.Value:F3}) " +
                $"nearestPlayerDist={distance:F3} nearestPlayerDeltaZ={dz:F3} " +
                $"{speedCompare}";
        }
    }

    private float? UpdateEntitySpeed(ulong guid, float x, float y, float z, long nowTickMs)
    {
        if (_entitySpeedSamples.TryGetValue(guid, out var last))
        {
            var speed = ComputeSpeed(last, x, y, z, nowTickMs);
            _entitySpeedSamples[guid] = new SpeedSample(x, y, z, nowTickMs);
            return speed;
        }

        _entitySpeedSamples[guid] = new SpeedSample(x, y, z, nowTickMs);
        return null;
    }

    private static float? UpdateSpeed(ref SpeedSample? sample, float x, float y, float z, long nowTickMs)
    {
        if (sample is SpeedSample last)
        {
            var speed = ComputeSpeed(last, x, y, z, nowTickMs);
            sample = new SpeedSample(x, y, z, nowTickMs);
            return speed;
        }

        sample = new SpeedSample(x, y, z, nowTickMs);
        return null;
    }

    private static float? ComputeSpeed(SpeedSample last, float x, float y, float z, long nowTickMs)
    {
        var dtMs = nowTickMs - last.TickMs;
        if (dtMs < 40 || dtMs > 5000)
        {
            return null;
        }

        var dx = x - last.X;
        var dy = y - last.Y;
        var distance = MathF.Sqrt((dx * dx) + (dy * dy));
        return distance / (dtMs / 1000f);
    }

    private string BuildSpeedCompareLog(float? selfSpeedMps, float? nearestSpeedMps)
    {
        if (!selfSpeedMps.HasValue || !nearestSpeedMps.HasValue)
        {
            return $"speedCompare=warming runSpeed={_currentRunSpeed:F2}";
        }

        var bot = selfSpeedMps.Value;
        var rhysel = nearestSpeedMps.Value;
        var delta = rhysel - bot;
        var ratio = bot > 0.01f ? rhysel / bot : 0f;
        return
            $"speedCompare=bot:{bot:F2}mps nearest:{rhysel:F2}mps delta:{delta:F2} ratio:{ratio:F2} runSpeed={_currentRunSpeed:F2}";
    }

    private string BuildSelfDriftLog(float playerX, float playerY, float playerZ)
    {
        lock (_entitiesSync)
        {
            if (_lastSelfAuthorityDrift is not SelfAuthorityDriftTelemetry drift)
            {
                return "selfDrift=none";
            }

            var now = Environment.TickCount64;
            var ageMs = now - drift.LocalTickMs;
            var distNow = MathF.Sqrt(DistanceSquared(playerX, playerY, playerZ, drift.ServerX, drift.ServerY, drift.ServerZ));
            return
                $"selfDriftDist={distNow:F3} selfDriftAtUpdate={drift.DriftDistance:F3} selfDriftAgeMs={ageMs} " +
                $"selfDriftMoving={drift.RecentlyMoving} selfDriftLargeJump={drift.LargeJump} " +
                $"selfDriftSentOp={drift.SentOpcode?.ToString() ?? "none"} selfDriftSentAgeMs={drift.SentAgeMs?.ToString() ?? "n/a"} " +
                $"selfDriftRecvOp={drift.RecvOpcode?.ToString() ?? "none"} selfDriftRecvAgeMs={drift.RecvAgeMs?.ToString() ?? "n/a"}";
        }
    }

    private NavigationPoint ApplyMovingDriftCompensation(NavigationPoint heartbeatPoint)
    {
        lock (_entitiesSync)
        {
            var now = Environment.TickCount64;
            var hasRecentEchoAuthority = _lastReceivedSelfMovement is MovementTelemetry recvSelfMovement &&
                                         now - recvSelfMovement.LocalTickMs <= 800;
            var hasRecentSentAuthority = _lastSentMovement is MovementTelemetry sentMovement &&
                                         now - sentMovement.LocalTickMs <= 800;
            if (!hasRecentEchoAuthority && !hasRecentSentAuthority)
            {
                return heartbeatPoint;
            }

            if (_lastSelfAuthorityDrift is not SelfAuthorityDriftTelemetry drift)
            {
                return heartbeatPoint;
            }

            var ageMs = now - drift.LocalTickMs;
            if (ageMs > 1500 || drift.DriftDistance < 1.0f)
            {
                return heartbeatPoint;
            }

            var dx = drift.ServerX - drift.LocalX;
            var dy = drift.ServerY - drift.LocalY;
            var dz = drift.ServerZ - drift.LocalZ;

            // Tiny correction baked into each heartbeat so server/local trajectories
            // stay close without visible snap-backs.
            var chaseCompensationScale = _isChaseMode ? 0.35f : 0.60f;
            if (hasRecentSentAuthority && !hasRecentEchoAuthority)
            {
                chaseCompensationScale *= 0.75f;
            }
            var gain = drift.LargeJump ? 0.035f : 0.020f;
            if (drift.DriftDistance >= 2.0f)
            {
                gain = drift.LargeJump ? 0.050f : 0.030f;
            }
            gain *= chaseCompensationScale;
            var correction = new NavigationPoint(
                heartbeatPoint.X + (dx * gain),
                heartbeatPoint.Y + (dy * gain),
                heartbeatPoint.Z + (dz * gain));

            var maxCorrectionXY = drift.LargeJump ? 0.10f : 0.05f;
            if (drift.DriftDistance >= 2.0f)
            {
                maxCorrectionXY = drift.LargeJump ? 0.16f : 0.08f;
            }
            maxCorrectionXY *= chaseCompensationScale;
            var stepped = MoveTowards(
                new NavigationPoint(heartbeatPoint.X, heartbeatPoint.Y, 0f),
                new NavigationPoint(correction.X, correction.Y, 0f),
                maxCorrectionXY);
            var maxCorrectionZ = drift.LargeJump ? 0.03f : 0.02f;
            if (drift.DriftDistance >= 2.0f)
            {
                maxCorrectionZ = drift.LargeJump ? 0.05f : 0.03f;
            }
            maxCorrectionZ *= chaseCompensationScale;
            var z = ClampZ(heartbeatPoint.Z, correction.Z, maxCorrectionZ);

            return new NavigationPoint(stepped.X, stepped.Y, z);
        }
    }

    private static TaskCompletionSource<T> NewTcs<T>()
    {
        return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static async Task<T> WaitAsync<T>(Task<T> task, CancellationToken cancellationToken)
    {
        var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cancellationToken));
        if (completed != task)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        return await task;
    }

    private void UpdateSnapshot(PlayerSnapshot? player, TargetSnapshot? target)
    {
        IReadOnlyList<NearbyUnitSnapshot> nearbyUnits;
        IReadOnlyList<NearbyWorldObjectSnapshot> nearbyWorldObjects;
        var activeQuests = BuildActiveQuestSnapshots();
        lock (_entitiesSync)
        {
            var nowTick = Environment.TickCount64;
            nearbyUnits = _entities
                .Where(x => x.Key != _currentCharacter?.Guid)
                .Where(x => x.Value.IsCreature || x.Value.IsPlayer || x.Value.IsCorpse)
                .Select(x =>
                {
                    float? posX = null;
                    float? posY = null;
                    float? posZ = null;
                    if (x.Value.TryGetProjectedPosition(nowTick, out var projected))
                    {
                        posX = projected.X;
                        posY = projected.Y;
                        posZ = projected.Z;
                    }

                    return new NearbyUnitSnapshot(
                        x.Key,
                        x.Value.IsCreature,
                        x.Value.IsPlayer,
                        x.Value.IsCorpse,
                        x.Value.IsAttackableHint,
                        x.Value.IsDeadHint,
                        x.Value.FactionTemplateId > 0 ? (int)x.Value.FactionTemplateId : null,
                        x.Value.OwnerGuid,
                        x.Value.TargetGuid,
                        x.Value.Level,
                        posX,
                        posY,
                        posZ,
                        x.Value.HealthPercent,
                        x.Value.EntryId,
                        x.Value.NpcFlags == 0 ? null : x.Value.NpcFlags,
                        x.Value.IsQuestGiver,
                        x.Value.QuestGiverStatus);
                })
                .ToArray();

            nearbyWorldObjects = _entities
                .Where(x => x.Key != _currentCharacter?.Guid)
                .Where(x => x.Value.IsGameObject || x.Value.IsDynamicObject)
                .Select(x =>
                {
                    float? posX = null;
                    float? posY = null;
                    float? posZ = null;
                    if (x.Value.TryGetProjectedPosition(nowTick, out var projected))
                    {
                        posX = projected.X;
                        posY = projected.Y;
                        posZ = projected.Z;
                    }

                    return new NearbyWorldObjectSnapshot(
                        x.Key,
                        x.Value.TypeId ?? 0,
                        x.Value.EntryId,
                        x.Value.OwnerGuid,
                        posX,
                        posY,
                        posZ,
                        x.Value.O);
                })
                .ToArray();
        }

        gameStateStore.Update(new WorldSnapshot(player, target, nearbyUnits, nearbyWorldObjects, activeQuests, DateTimeOffset.UtcNow));
    }

    private sealed class ActiveQuestState
    {
        public ActiveQuestState(uint questId)
        {
            QuestId = questId;
        }

        public uint QuestId { get; }

        public bool IsCompleted { get; set; }

        public uint[] CreatureOrGoCounts { get; } = new uint[4];

        public bool[] CreatureOrGoKnown { get; } = new bool[4];

        public uint?[] ItemCounts { get; } = new uint?[6];

        public Dictionary<uint, QuestObjectiveRuntimeProgress> PendingObjectiveProgress { get; } = [];

        public DateTimeOffset LastUpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }

    private sealed class MovementCommandScope : IDisposable
    {
        private readonly RealGameClient _owner;
        private bool _disposed;

        public MovementCommandScope(
            RealGameClient owner,
            long operationId,
            string commandName,
            CancellationTokenSource commandCts,
            CancellationTokenSource linkedCts)
        {
            _owner = owner;
            OperationId = operationId;
            CommandName = commandName;
            CommandCts = commandCts;
            LinkedCts = linkedCts;
        }

        public long OperationId { get; }

        public string CommandName { get; }

        public CancellationTokenSource CommandCts { get; }

        public CancellationTokenSource LinkedCts { get; }

        public CancellationToken CancellationToken => LinkedCts.Token;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner.EndMovementCommand(this);
        }
    }

    private sealed class EntityState
    {
        private const byte MonsterMoveTypeStop = 1;
        private const uint UnitFlagNonAttackable = 0x00000002;
        private const uint UnitFlagNotAttackable1 = 0x00000080;
        private const uint UnitFlagImmuneToPc = 0x00000100;
        private const uint UnitFlagNonAttackable2 = 0x00010000;
        private const uint UnitFlagOnTaxi = 0x00100000;
        private const uint UnitFlagUninteractible = 0x02000000;
        private const uint UnitFlagImmune = 0x80000000;
        private const uint NotAttackableMask =
            UnitFlagNonAttackable |
            UnitFlagNotAttackable1 |
            UnitFlagImmuneToPc |
            UnitFlagNonAttackable2 |
            UnitFlagOnTaxi |
            UnitFlagUninteractible |
            UnitFlagImmune;

        public ulong Guid { get; set; }
        public uint OwnerLow { get; set; }
        public uint OwnerHigh { get; set; }
        public uint TargetLow { get; set; }
        public uint TargetHigh { get; set; }
        public uint Health { get; set; }
        public uint MaxHealth { get; set; }
        public uint UnitFlags { get; set; }
        public uint UnitFlags2 { get; set; }
        public uint UnitBytes1 { get; set; }
        public uint DynamicFlags { get; set; }
        public uint PlayerFlags { get; set; }
        public uint FactionTemplateId { get; set; }
        public uint? EntryId { get; set; }
        public uint NpcFlags { get; set; }
        public QuestGiverStatus? QuestGiverStatus { get; set; }
        public int? Level { get; set; }
        public byte? TypeId { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public float? Z { get; set; }
        public float? O { get; set; }
        public bool HasActiveSpline { get; set; }
        public float SplineStartX { get; set; }
        public float SplineStartY { get; set; }
        public float SplineStartZ { get; set; }
        public float SplineEndX { get; set; }
        public float SplineEndY { get; set; }
        public float SplineEndZ { get; set; }
        public uint SplineDurationMs { get; set; }
        public long SplineStartAtTick { get; set; }

        public ulong OwnerGuid => ((ulong)OwnerHigh << 32) | OwnerLow;
        public ulong TargetGuid => ((ulong)TargetHigh << 32) | TargetLow;
        public int? HealthPercent => MaxHealth == 0 ? null : (int)Math.Clamp((Health * 100) / MaxHealth, 0, 100);
        public byte StandState => (byte)(UnitBytes1 & 0xFF);
        public bool IsStandStateDead => StandState == 7; // UNIT_STAND_STATE_DEAD
        public bool IsFeignDeath => (UnitFlags2 & 0x00000001) != 0; // UNIT_FLAG2_FEIGN_DEATH
        public bool IsLootable => (DynamicFlags & 0x0001) != 0; // UNIT_DYNFLAG_LOOTABLE
        public bool IsDynamicDead => (DynamicFlags & 0x0020) != 0; // UNIT_DYNFLAG_DEAD
        public bool IsSkinnable => (UnitFlags & 0x04000000) != 0; // UNIT_FLAG_SKINNABLE
        public bool IsGhostPlayerFlag => (PlayerFlags & 0x00000010) != 0; // PLAYER_FLAGS_GHOST
        public bool IsAttackableHint => (UnitFlags & NotAttackableMask) == 0;
        public bool IsQuestGiver => (NpcFlags & 0x00000002) != 0;
        public bool IsDeadHint =>
            (Health == 0 && MaxHealth > 0) ||
            IsStandStateDead ||
            IsLootable ||
            IsSkinnable ||
            (IsDynamicDead && !IsFeignDeath);
        public bool IsCreature => TypeId == 3;
        public bool IsCorpse => TypeId == 7;
        public bool IsGameObject => TypeId == 5;
        public bool IsDynamicObject => TypeId == 6;
        // TrinityCore 3.3.5 players use HighGuid::Player = 0x0000 in the top 16 bits.
        // Keep a conservative fallback when TypeId is absent in incremental updates.
        public bool IsPlayer =>
            TypeId == 4 ||
            (TypeId is null && (((Guid >> 48) & 0xFFFFUL) == 0) && Level.HasValue && HasPosition);
        public bool HasPosition => X.HasValue && Y.HasValue && Z.HasValue;

        public void ApplyMonsterMove(MovementPacketCodec.MonsterMovePayloadData movement, long nowTick)
        {
            X = movement.StartX;
            Y = movement.StartY;
            Z = movement.StartZ;

            var dx = movement.EndX - movement.StartX;
            var dy = movement.EndY - movement.StartY;
            var dz = movement.EndZ - movement.StartZ;
            var travelDistanceSq = (dx * dx) + (dy * dy) + (dz * dz);
            if (movement.MoveType == MonsterMoveTypeStop || movement.DurationMs == 0 || travelDistanceSq <= 0.0001f)
            {
                HasActiveSpline = false;
                X = movement.EndX;
                Y = movement.EndY;
                Z = movement.EndZ;
                return;
            }

            HasActiveSpline = true;
            SplineStartX = movement.StartX;
            SplineStartY = movement.StartY;
            SplineStartZ = movement.StartZ;
            SplineEndX = movement.EndX;
            SplineEndY = movement.EndY;
            SplineEndZ = movement.EndZ;
            SplineDurationMs = movement.DurationMs;
            SplineStartAtTick = nowTick;
        }

        public bool TryGetProjectedPosition(long nowTick, out NavigationPoint position)
        {
            if (HasActiveSpline)
            {
                var elapsedMs = Math.Max(0L, nowTick - SplineStartAtTick);
                var durationMs = Math.Max(1u, SplineDurationMs);
                var t = Math.Clamp(elapsedMs / (float)durationMs, 0f, 1f);
                var x = Lerp(SplineStartX, SplineEndX, t);
                var y = Lerp(SplineStartY, SplineEndY, t);
                var z = Lerp(SplineStartZ, SplineEndZ, t);
                position = new NavigationPoint(x, y, z);

                if (t >= 1f)
                {
                    HasActiveSpline = false;
                    X = SplineEndX;
                    Y = SplineEndY;
                    Z = SplineEndZ;
                }

                return true;
            }

            if (HasPosition)
            {
                position = new NavigationPoint(X!.Value, Y!.Value, Z!.Value);
                return true;
            }

            position = new NavigationPoint(0f, 0f, 0f);
            return false;
        }

        public float DistanceSquaredTo(float x, float y, float z)
        {
            if (!HasPosition)
            {
                return float.MaxValue;
            }

            var dx = X!.Value - x;
            var dy = Y!.Value - y;
            var dz = Z!.Value - z;
            return (dx * dx) + (dy * dy) + (dz * dz);
        }

        private static float Lerp(float a, float b, float t) => a + ((b - a) * t);
    }

    private readonly record struct MovementTelemetry(
        WorldOpcode Opcode,
        uint? MovementTimeMs,
        float X,
        float Y,
        float Z,
        long LocalTickMs);

    private readonly record struct SelfAuthorityDriftTelemetry(
        long LocalTickMs,
        float ServerX,
        float ServerY,
        float ServerZ,
        float LocalX,
        float LocalY,
        float LocalZ,
        float DriftDistance,
        bool RecentlyMoving,
        bool LargeJump,
        WorldOpcode? SentOpcode,
        uint? SentMovementTimeMs,
        long? SentAgeMs,
        WorldOpcode? RecvOpcode,
        uint? RecvMovementTimeMs,
        long? RecvAgeMs);

    private readonly record struct QuestObjectiveRuntimeProgress(uint CurrentCount, uint RequiredCount);

    private readonly record struct SpeedSample(float X, float Y, float Z, long TickMs);
}
