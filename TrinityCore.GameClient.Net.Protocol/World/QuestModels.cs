namespace TrinityCore.GameClient.Net.Protocol.World;

public enum QuestGiverStatus : byte
{
    None = 0,
    Unavailable = 1,
    LowLevelAvailable = 2,
    LowLevelRewardRep = 3,
    LowLevelAvailableRep = 4,
    Incomplete = 5,
    RewardRep = 6,
    AvailableRep = 7,
    Available = 8,
    RewardNoMinimapDot = 9,
    Reward = 10
}

public enum QuestDialogKind
{
    Details,
    RequestItems,
    OfferReward
}

public enum QuestObjectiveKind
{
    None = 0,
    Creature = 1,
    GameObject = 2,
    Item = 3,
    PlayerKill = 4
}

public sealed record QuestGiverStatusInfo(ulong QuestGiverGuid, QuestGiverStatus Status);

public sealed record QuestChoiceItem(uint ItemId, uint Quantity, uint DisplayId);

public sealed record QuestRequirementItem(uint ItemId, uint Quantity, uint DisplayId);

public sealed record QuestGiverMenuItem(
    uint QuestId,
    uint QuestIcon,
    int QuestLevel,
    uint Flags,
    bool RepeatableTurnIn,
    string Title);

public sealed record QuestGiverMenu(
    ulong QuestGiverGuid,
    string Greeting,
    uint EmoteDelay,
    uint EmoteType,
    IReadOnlyList<QuestGiverMenuItem> Items);

public sealed record QuestDialog(
    QuestDialogKind Kind,
    ulong QuestGiverGuid,
    uint QuestId,
    string Title,
    string Text,
    string Objectives,
    bool AutoLaunched,
    uint Flags,
    uint SuggestedGroupNum,
    bool CanComplete,
    bool CloseOnCancel,
    uint RequiredMoney,
    uint RewardMoney,
    uint RewardXp,
    IReadOnlyList<QuestRequirementItem> RequiredItems,
    IReadOnlyList<QuestChoiceItem> ChoiceItems,
    IReadOnlyList<QuestChoiceItem> RewardItems);

public sealed record QuestTurnInResult(
    uint QuestId,
    uint RewardXp,
    uint RewardMoney,
    uint RewardHonor,
    uint RewardTalents,
    uint RewardArenaPoints);

public sealed record QuestObjectiveDefinition(
    int ObjectiveIndex,
    QuestObjectiveKind Kind,
    uint? TargetEntryId,
    uint RequiredCount,
    uint? SourceItemId,
    string Text);

public sealed record QuestItemObjectiveDefinition(
    int ObjectiveIndex,
    uint ItemId,
    uint RequiredCount);

public sealed record QuestDefinition(
    uint QuestId,
    string Title,
    string ObjectivesSummary,
    string Details,
    string AreaDescription,
    string CompletedText,
    int QuestLevel,
    int QuestMinLevel,
    int QuestSortId,
    uint Flags,
    uint SuggestedGroupNum,
    uint RequiredPlayerKills,
    uint PoiContinent,
    float PoiX,
    float PoiY,
    uint PoiPriority,
    IReadOnlyList<QuestObjectiveDefinition> Objectives,
    IReadOnlyList<QuestItemObjectiveDefinition> ItemObjectives);

public sealed record QuestPoiPoint(int X, int Y);

public sealed record QuestPoiBlob(
    uint BlobIndex,
    int ObjectiveIndex,
    uint MapId,
    uint WorldMapAreaId,
    uint Floor,
    uint Priority,
    uint Flags,
    IReadOnlyList<QuestPoiPoint> Points);

public sealed record QuestPoiInfo(
    uint QuestId,
    IReadOnlyList<QuestPoiBlob> Blobs);
