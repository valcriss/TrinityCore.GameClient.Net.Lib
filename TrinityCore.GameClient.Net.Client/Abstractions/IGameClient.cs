using TrinityCore.GameClient.Net.Client.Models;
using TrinityCore.GameClient.Net.Navigation.Abstractions;
using TrinityCore.GameClient.Net.Protocol.World;

namespace TrinityCore.GameClient.Net.Client.Abstractions;

public interface IGameClient
{
    bool AutoAcceptGroupInvites { get; set; }
    GroupMembershipInfo? CurrentGroup { get; }
    GroupInviteInfo? PendingGroupInvite { get; }
    MeleeAttackState CurrentMeleeAttack { get; }
    IReadOnlyList<ReceivedChatMessage> DrainIncomingChatMessages();
    Task<bool> LoginAsync(AuthServerInfo server, AuthServerCredentials credentials, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RealmInfo>> GetRealmsAsync(CancellationToken cancellationToken = default);
    Task<bool> ConnectRealmAsync(RealmInfo realm, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CharacterInfo>> GetCharactersAsync(CancellationToken cancellationToken = default);
    Task<bool> EnterWorldAsync(CharacterInfo character, CancellationToken cancellationToken = default);
    Task<bool> MoveToAsync(
        NavigationPoint destination,
        float arrivalRadius = 2.5f,
        int repathIntervalMs = 750,
        int stuckTimeoutMs = 3000,
        CancellationToken cancellationToken = default);
    Task<bool> StopMovementAsync(CancellationToken cancellationToken = default);
    Task<bool> FaceTowardsAsync(NavigationPoint destination, CancellationToken cancellationToken = default);
    Task<bool> SelectTargetAsync(ulong targetGuid, CancellationToken cancellationToken = default);
    Task<bool> StartMeleeAttackAsync(ulong targetGuid, CancellationToken cancellationToken = default);
    Task<bool> StopMeleeAttackAsync(CancellationToken cancellationToken = default);
    Task<bool> AcceptPendingGroupInviteAsync(CancellationToken cancellationToken = default);
    Task<bool> DeclinePendingGroupInviteAsync(CancellationToken cancellationToken = default);
    Task<bool> SendChatMessageAsync(
        ChatChannel channel,
        string message,
        string? whisperTarget = null,
        string? channelName = null,
        ChatLanguage language = ChatLanguage.Auto,
        CancellationToken cancellationToken = default);
    Task<bool> SendEmoteAsync(uint emoteId, CancellationToken cancellationToken = default);
    Task<bool> SendTextEmoteAsync(
        uint textEmoteId,
        ulong targetGuid = 0,
        uint emoteNum = 0,
        CancellationToken cancellationToken = default);
    Task<QuestDefinition?> QueryQuestDefinitionAsync(uint questId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuestPoiInfo>> QueryQuestPoiAsync(IReadOnlyList<uint> questIds, CancellationToken cancellationToken = default);
    Task<QuestGiverStatusInfo?> QueryQuestGiverStatusAsync(ulong questGiverGuid, CancellationToken cancellationToken = default);
    Task<QuestGiverMenu?> OpenQuestGiverAsync(ulong questGiverGuid, CancellationToken cancellationToken = default);
    Task<QuestDialog?> QueryQuestAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default);
    Task<bool> AcceptQuestAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default);
    Task<QuestDialog?> CompleteQuestAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default);
    Task<QuestDialog?> RequestQuestRewardAsync(ulong questGiverGuid, uint questId, CancellationToken cancellationToken = default);
    Task<bool> ChooseQuestRewardAsync(ulong questGiverGuid, uint questId, uint rewardIndex = 0, CancellationToken cancellationToken = default);
    Task<bool> RequestRepopAsync(CancellationToken cancellationToken = default);
    Task<bool> ReclaimCorpseAsync(ulong corpseGuid = 0, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(int timeoutSeconds = 25, CancellationToken cancellationToken = default);
}
