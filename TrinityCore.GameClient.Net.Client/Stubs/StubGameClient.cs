using TrinityCore.GameClient.Net.Client.Abstractions;
using TrinityCore.GameClient.Net.Client.Models;
using TrinityCore.GameClient.Net.Navigation.Abstractions;
using TrinityCore.GameClient.Net.Protocol.World;

namespace TrinityCore.GameClient.Net.Client.Stubs;

public sealed class StubGameClient(IAuthService authService, IRealmService realmService, IWorldSession worldSession) : IGameClient
{
    public bool AutoAcceptGroupInvites { get; set; } = true;

    public GroupMembershipInfo? CurrentGroup => null;

    public GroupInviteInfo? PendingGroupInvite => null;

    public MeleeAttackState CurrentMeleeAttack => new(false, 0);

    public IReadOnlyList<ReceivedChatMessage> DrainIncomingChatMessages() => [];

    public Task<bool> LoginAsync(AuthServerInfo server, AuthServerCredentials credentials, CancellationToken cancellationToken = default)
    {
        return authService.AuthenticateAsync(server, credentials, cancellationToken);
    }

    public Task<IReadOnlyList<RealmInfo>> GetRealmsAsync(CancellationToken cancellationToken = default)
    {
        return realmService.GetRealmsAsync(cancellationToken);
    }

    public Task<bool> ConnectRealmAsync(RealmInfo realm, CancellationToken cancellationToken = default)
    {
        return worldSession.ConnectRealmAsync(realm, cancellationToken);
    }

    public Task<IReadOnlyList<CharacterInfo>> GetCharactersAsync(CancellationToken cancellationToken = default)
    {
        return worldSession.GetCharactersAsync(cancellationToken);
    }

    public Task<bool> EnterWorldAsync(CharacterInfo character, CancellationToken cancellationToken = default)
    {
        return worldSession.EnterWorldAsync(character, cancellationToken);
    }

    public Task<bool> MoveToAsync(
        NavigationPoint destination,
        float arrivalRadius = 2.5f,
        int repathIntervalMs = 750,
        int stuckTimeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> StopMovementAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> FaceTowardsAsync(NavigationPoint destination, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SelectTargetAsync(ulong targetGuid, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> StartMeleeAttackAsync(ulong targetGuid, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> StopMeleeAttackAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> AcceptPendingGroupInviteAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> DeclinePendingGroupInviteAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SendChatMessageAsync(
        ChatChannel channel,
        string message,
        string? whisperTarget = null,
        string? channelName = null,
        ChatLanguage language = ChatLanguage.Auto,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(message));
    }

    public Task<bool> SendEmoteAsync(uint emoteId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(emoteId != 0);
    }

    public Task<bool> SendTextEmoteAsync(
        uint textEmoteId,
        ulong targetGuid = 0,
        uint emoteNum = 0,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(textEmoteId != 0);
    }

    public Task<QuestDefinition?> QueryQuestDefinitionAsync(uint questId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<QuestDefinition?>(new QuestDefinition(
            questId,
            "Stub Quest",
            "Stub objectives",
            "Stub details",
            string.Empty,
            string.Empty,
            1,
            1,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            [],
            []));
    }

    public Task<IReadOnlyList<QuestPoiInfo>> QueryQuestPoiAsync(IReadOnlyList<uint> questIds, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<QuestPoiInfo>>([]);
    }

    public Task<QuestGiverStatusInfo?> QueryQuestGiverStatusAsync(ulong questGiverGuid, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<QuestGiverStatusInfo?>(new QuestGiverStatusInfo(questGiverGuid, QuestGiverStatus.Available));
    }

    public Task<QuestGiverMenu?> OpenQuestGiverAsync(ulong questGiverGuid, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<QuestGiverMenu?>(new QuestGiverMenu(questGiverGuid, "Stub quest giver", 0, 0, []));
    }

    public Task<QuestDialog?> QueryQuestAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<QuestDialog?>(new QuestDialog(
            QuestDialogKind.Details,
            questGiverGuid,
            questId,
            "Stub Quest",
            "Stub details",
            "Stub objectives",
            false,
            0,
            0,
            false,
            false,
            0,
            0,
            0,
            [],
            [],
            []));
    }

    public Task<bool> AcceptQuestAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<QuestDialog?> CompleteQuestAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<QuestDialog?>(null);
    }

    public Task<QuestDialog?> RequestQuestRewardAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<QuestDialog?>(null);
    }

    public Task<bool> ChooseQuestRewardAsync(ulong questGiverGuid, uint questId, uint rewardIndex = 0, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> RequestRepopAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> ReclaimCorpseAsync(ulong corpseGuid = 0, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> LogoutAsync(int timeoutSeconds = 25, CancellationToken cancellationToken = default)
    {
        return worldSession.LogoutAsync(cancellationToken);
    }
}
