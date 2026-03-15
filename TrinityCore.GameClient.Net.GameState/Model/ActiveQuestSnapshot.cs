using TrinityCore.GameClient.Net.Protocol.World;

namespace TrinityCore.GameClient.Net.GameState.Model;

public sealed record ActiveQuestSnapshot(
    uint QuestId,
    string? Title,
    string ObjectivesSummary,
    bool IsCompleted,
    IReadOnlyList<QuestObjectiveProgressSnapshot> Objectives,
    IReadOnlyList<QuestItemObjectiveProgressSnapshot> ItemObjectives,
    IReadOnlyList<QuestPoiBlobSnapshot> PoiBlobs);

public sealed record QuestObjectiveProgressSnapshot(
    int ObjectiveIndex,
    QuestObjectiveKind Kind,
    uint? TargetEntryId,
    string Text,
    uint RequiredCount,
    uint CurrentCount,
    bool IsCompleted);

public sealed record QuestItemObjectiveProgressSnapshot(
    int ObjectiveIndex,
    uint ItemId,
    uint RequiredCount,
    uint? CurrentCount,
    bool IsCompleted);

public sealed record QuestPoiBlobSnapshot(
    uint BlobIndex,
    int ObjectiveIndex,
    uint MapId,
    uint WorldMapAreaId,
    uint Floor,
    uint Priority,
    uint Flags,
    IReadOnlyList<QuestPoiPointSnapshot> Points);

public sealed record QuestPoiPointSnapshot(int X, int Y);
