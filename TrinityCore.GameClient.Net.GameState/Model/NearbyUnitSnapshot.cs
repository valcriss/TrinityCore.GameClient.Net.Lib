using TrinityCore.GameClient.Net.Protocol.World;

namespace TrinityCore.GameClient.Net.GameState.Model;

public sealed record NearbyUnitSnapshot(
    ulong Guid,
    bool IsCreature,
    bool IsPlayer,
    bool IsCorpse,
    bool IsAttackableHint,
    bool IsDeadHint,
    int? FactionTemplateId,
    ulong OwnerGuid,
    ulong TargetGuid,
    int? Level,
    float? X,
    float? Y,
    float? Z,
    int? HealthPercent,
    uint? EntryId,
    uint? NpcFlags,
    bool IsQuestGiver,
    QuestGiverStatus? QuestGiverStatus);
