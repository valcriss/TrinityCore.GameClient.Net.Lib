using System.Buffers.Binary;
using System.Text;

namespace TrinityCore.GameClient.Net.Protocol.World;

public static class QuestPacketCodec
{
    private const uint GameObjectEntryMask = 0x80000000;

    public static byte[] BuildQuestGiverStatusQueryPayload(ulong questGiverGuid) => BitConverter.GetBytes(questGiverGuid);

    public static byte[] BuildQuestGiverHelloPayload(ulong questGiverGuid) => BitConverter.GetBytes(questGiverGuid);

    public static byte[] BuildQuestQueryPayload(uint questId)
    {
        var payload = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, questId);
        return payload;
    }

    public static byte[] BuildQuestPoiQueryPayload(IReadOnlyList<uint> questIds)
    {
        var safeQuestIds = questIds
            .Where(x => x != 0)
            .Distinct()
            .ToArray();
        var payload = new byte[4 + (safeQuestIds.Length * 4)];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0, 4), (uint)safeQuestIds.Length);
        for (var i = 0; i < safeQuestIds.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4 + (i * 4), 4), safeQuestIds[i]);
        }

        return payload;
    }

    public static byte[] BuildQuestGiverQueryQuestPayload(ulong questGiverGuid, uint questId, byte queryFlags = 0)
    {
        var payload = new byte[13];
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(0, 8), questGiverGuid);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8, 4), questId);
        payload[12] = queryFlags;
        return payload;
    }

    public static byte[] BuildQuestGiverAcceptQuestPayload(ulong questGiverGuid, uint questId, uint startCheat = 0)
    {
        var payload = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(0, 8), questGiverGuid);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8, 4), questId);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(12, 4), startCheat);
        return payload;
    }

    public static byte[] BuildQuestGiverCompleteQuestPayload(ulong questGiverGuid, uint questId)
    {
        var payload = new byte[12];
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(0, 8), questGiverGuid);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8, 4), questId);
        return payload;
    }

    public static byte[] BuildQuestGiverRequestRewardPayload(ulong questGiverGuid, uint questId)
        => BuildQuestGiverCompleteQuestPayload(questGiverGuid, questId);

    public static byte[] BuildQuestGiverChooseRewardPayload(ulong questGiverGuid, uint questId, uint rewardIndex)
    {
        var payload = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(0, 8), questGiverGuid);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8, 4), questId);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(12, 4), rewardIndex);
        return payload;
    }

    public static byte[] BuildQuestGiverCancelPayload() => [];

    public static QuestGiverStatusInfo ParseQuestGiverStatus(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var questGiverGuid = reader.ReadUInt64();
        var status = (QuestGiverStatus)reader.ReadByte();
        return new QuestGiverStatusInfo(questGiverGuid, status);
    }

    public static IReadOnlyList<QuestGiverStatusInfo> ParseQuestGiverStatusMultiple(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var count = reader.ReadUInt32();
        var items = new List<QuestGiverStatusInfo>((int)count);
        for (var i = 0; i < count; i++)
        {
            items.Add(new QuestGiverStatusInfo(
                reader.ReadUInt64(),
                (QuestGiverStatus)reader.ReadByte()));
        }

        return items;
    }

    public static QuestGiverMenu ParseQuestGiverQuestList(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var questGiverGuid = reader.ReadUInt64();
        var greeting = reader.ReadCString();
        var emoteDelay = reader.ReadUInt32();
        var emoteType = reader.ReadUInt32();
        var itemCount = reader.ReadByte();
        var items = new List<QuestGiverMenuItem>(itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            var questId = reader.ReadUInt32();
            var questIcon = reader.ReadUInt32();
            var questLevel = reader.ReadInt32();
            var flags = reader.ReadUInt32();
            var repeatableTurnIn = reader.ReadByte() != 0;
            var title = reader.ReadCString();
            items.Add(new QuestGiverMenuItem(questId, questIcon, questLevel, flags, repeatableTurnIn, title));
        }

        return new QuestGiverMenu(questGiverGuid, greeting, emoteDelay, emoteType, items);
    }

    public static QuestDialog ParseQuestGiverQuestDetails(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var questGiverGuid = reader.ReadUInt64();
        _ = reader.ReadUInt64(); // inform unit guid
        var questId = reader.ReadUInt32();
        var title = reader.ReadCString();
        var details = reader.ReadCString();
        var objectives = reader.ReadCString();
        var autoLaunched = reader.ReadByte() != 0;
        var flags = reader.ReadUInt32();
        var suggestedGroupNum = reader.ReadUInt32();
        _ = reader.ReadByte(); // start cheat
        var choiceItems = ReadChoiceItems(reader);
        var rewardItems = ReadChoiceItems(reader);
        var rewardMoney = reader.ReadUInt32();
        var rewardXp = reader.ReadUInt32();
        reader.Skip(sizeof(uint) + sizeof(float) + sizeof(uint) + sizeof(int) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint));
        reader.Skip(5 * sizeof(uint)); // RewardFactionID
        reader.Skip(5 * sizeof(int));  // RewardFactionValue
        reader.Skip(5 * sizeof(int));  // RewardFactionValueOverride
        var emoteCount = reader.ReadInt32();
        reader.Skip(Math.Max(0, emoteCount) * 8);

        return new QuestDialog(
            QuestDialogKind.Details,
            questGiverGuid,
            questId,
            title,
            details,
            objectives,
            autoLaunched,
            flags,
            suggestedGroupNum,
            false,
            false,
            0,
            rewardMoney,
            rewardXp,
            [],
            choiceItems,
            rewardItems);
    }

    public static QuestDialog ParseQuestGiverOfferReward(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var questGiverGuid = reader.ReadUInt64();
        var questId = reader.ReadUInt32();
        var title = reader.ReadCString();
        var rewardText = reader.ReadCString();
        var autoLaunched = reader.ReadByte() != 0;
        var flags = reader.ReadUInt32();
        var suggestedGroupNum = reader.ReadUInt32();
        var emoteCount = (int)reader.ReadUInt32();
        reader.Skip(Math.Max(0, emoteCount) * 8);
        var choiceItems = ReadChoiceItems(reader);
        var rewardItems = ReadChoiceItems(reader);
        var rewardMoney = reader.ReadUInt32();
        var rewardXp = reader.ReadUInt32();
        reader.Skip(sizeof(uint) + sizeof(float) + sizeof(uint) + sizeof(uint) + sizeof(int) + sizeof(uint) + sizeof(uint) + sizeof(uint));
        reader.Skip(5 * sizeof(uint)); // RewardFactionID
        reader.Skip(5 * sizeof(int));  // RewardFactionValue
        reader.Skip(5 * sizeof(int));  // RewardFactionValueOverride

        return new QuestDialog(
            QuestDialogKind.OfferReward,
            questGiverGuid,
            questId,
            title,
            rewardText,
            string.Empty,
            autoLaunched,
            flags,
            suggestedGroupNum,
            true,
            false,
            0,
            rewardMoney,
            rewardXp,
            [],
            choiceItems,
            rewardItems);
    }

    public static QuestDialog ParseQuestGiverRequestItems(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var questGiverGuid = reader.ReadUInt64();
        var questId = reader.ReadUInt32();
        var title = reader.ReadCString();
        var requestItemsText = reader.ReadCString();
        _ = reader.ReadUInt32(); // unknown
        _ = reader.ReadUInt32(); // emote
        var closeOnCancel = reader.ReadUInt32() != 0;
        var flags = reader.ReadUInt32();
        var suggestedGroupNum = reader.ReadUInt32();
        var requiredMoney = reader.ReadUInt32();
        var itemCount = reader.ReadUInt32();
        var requiredItems = new List<QuestRequirementItem>((int)itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            requiredItems.Add(new QuestRequirementItem(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32()));
        }

        var canComplete = reader.ReadUInt32() != 0;
        reader.Skip(sizeof(uint) * 3);

        return new QuestDialog(
            QuestDialogKind.RequestItems,
            questGiverGuid,
            questId,
            title,
            requestItemsText,
            string.Empty,
            false,
            flags,
            suggestedGroupNum,
            canComplete,
            closeOnCancel,
            requiredMoney,
            0,
            0,
            requiredItems,
            [],
            []);
    }

    public static byte ParseQuestGiverQuestInvalidReason(byte[] payload)
    {
        return payload.Length > 0 ? payload[0] : (byte)0;
    }

    public static QuestDefinition ParseQuestQueryResponse(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var questId = reader.ReadUInt32();
        _ = reader.ReadUInt32(); // quest method
        var questLevel = unchecked((int)reader.ReadUInt32());
        var questMinLevel = unchecked((int)reader.ReadUInt32());
        var questSortId = unchecked((int)reader.ReadUInt32());
        _ = reader.ReadUInt32(); // quest type
        var suggestedGroupNum = reader.ReadUInt32();

        reader.Skip(2 * 8); // required faction ids/values

        _ = reader.ReadUInt32(); // reward next quest
        _ = reader.ReadUInt32(); // reward xp difficulty
        _ = reader.ReadUInt32(); // reward money
        _ = reader.ReadUInt32(); // reward bonus money
        _ = reader.ReadUInt32(); // reward display spell
        _ = reader.ReadInt32();  // reward spell
        _ = reader.ReadUInt32(); // reward honor
        _ = reader.ReadSingle(); // reward kill honor
        _ = reader.ReadUInt32(); // start item
        var flags = reader.ReadUInt32();
        _ = reader.ReadUInt32(); // reward title id
        var requiredPlayerKills = reader.ReadUInt32();
        _ = reader.ReadUInt32(); // reward talents
        _ = reader.ReadInt32();  // reward arena points
        _ = reader.ReadUInt32(); // reward faction flags

        reader.Skip((4 * 2) * 4); // reward items/amount
        reader.Skip((6 * 2) * 4); // reward choice items/amount
        reader.Skip(5 * 4);       // reward faction id
        reader.Skip(5 * 4);       // reward faction value
        reader.Skip(5 * 4);       // reward faction value override

        var poiContinent = reader.ReadUInt32();
        var poiX = reader.ReadSingle();
        var poiY = reader.ReadSingle();
        var poiPriority = reader.ReadUInt32();

        var title = reader.ReadCString();
        var objectivesSummary = reader.ReadCString();
        var details = reader.ReadCString();
        var areaDescription = reader.ReadCString();
        var completedText = reader.ReadCString();

        var objectiveTargets = new RawObjectiveTarget[4];
        for (var i = 0; i < objectiveTargets.Length; i++)
        {
            objectiveTargets[i] = new RawObjectiveTarget(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32());
        }

        var itemObjectives = new List<QuestItemObjectiveDefinition>(6);
        for (var i = 0; i < 6; i++)
        {
            var itemId = reader.ReadUInt32();
            var requiredCount = reader.ReadUInt32();
            if (itemId != 0 || requiredCount != 0)
            {
                itemObjectives.Add(new QuestItemObjectiveDefinition(i, itemId, requiredCount));
            }
        }

        var objectiveTexts = new string[4];
        for (var i = 0; i < objectiveTexts.Length; i++)
        {
            objectiveTexts[i] = reader.ReadCString();
        }

        var objectives = new List<QuestObjectiveDefinition>(4);
        for (var i = 0; i < objectiveTargets.Length; i++)
        {
            var raw = objectiveTargets[i];
            var kind = QuestObjectiveKind.None;
            uint? targetEntryId = null;
            if ((raw.TargetId & GameObjectEntryMask) != 0)
            {
                kind = QuestObjectiveKind.GameObject;
                targetEntryId = raw.TargetId & ~GameObjectEntryMask;
            }
            else if (raw.TargetId != 0)
            {
                kind = QuestObjectiveKind.Creature;
                targetEntryId = raw.TargetId;
            }

            if (kind == QuestObjectiveKind.None &&
                raw.RequiredCount == 0 &&
                raw.SourceItemId == 0 &&
                string.IsNullOrWhiteSpace(objectiveTexts[i]))
            {
                continue;
            }

            objectives.Add(new QuestObjectiveDefinition(
                i,
                kind,
                targetEntryId,
                raw.RequiredCount,
                raw.SourceItemId == 0 ? null : raw.SourceItemId,
                objectiveTexts[i]));
        }

        if (requiredPlayerKills > 0)
        {
            objectives.Add(new QuestObjectiveDefinition(
                objectives.Count + itemObjectives.Count,
                QuestObjectiveKind.PlayerKill,
                null,
                requiredPlayerKills,
                null,
                string.Empty));
        }

        return new QuestDefinition(
            questId,
            title,
            objectivesSummary,
            details,
            areaDescription,
            completedText,
            questLevel,
            questMinLevel,
            questSortId,
            flags,
            suggestedGroupNum,
            requiredPlayerKills,
            poiContinent,
            poiX,
            poiY,
            poiPriority,
            objectives,
            itemObjectives);
    }

    public static IReadOnlyList<QuestPoiInfo> ParseQuestPoiQueryResponse(byte[] payload)
    {
        var reader = new PacketReader(payload);
        var questCount = reader.ReadUInt32();
        var result = new List<QuestPoiInfo>((int)questCount);
        for (var i = 0; i < questCount; i++)
        {
            var questId = reader.ReadUInt32();
            var poiCount = reader.ReadUInt32();
            var blobs = new List<QuestPoiBlob>((int)poiCount);
            for (var j = 0; j < poiCount; j++)
            {
                var blobIndex = reader.ReadUInt32();
                var objectiveIndex = reader.ReadInt32();
                var mapId = reader.ReadUInt32();
                var worldMapAreaId = reader.ReadUInt32();
                var floor = reader.ReadUInt32();
                var priority = reader.ReadUInt32();
                var flags = reader.ReadUInt32();
                var pointCount = reader.ReadUInt32();
                var points = new List<QuestPoiPoint>((int)pointCount);
                for (var k = 0; k < pointCount; k++)
                {
                    points.Add(new QuestPoiPoint(reader.ReadInt32(), reader.ReadInt32()));
                }

                blobs.Add(new QuestPoiBlob(
                    blobIndex,
                    objectiveIndex,
                    mapId,
                    worldMapAreaId,
                    floor,
                    priority,
                    flags,
                    points));
            }

            result.Add(new QuestPoiInfo(questId, blobs));
        }

        return result;
    }

    public static QuestTurnInResult ParseQuestGiverQuestComplete(byte[] payload)
    {
        var reader = new PacketReader(payload);
        return new QuestTurnInResult(
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32());
    }

    private static IReadOnlyList<QuestChoiceItem> ReadChoiceItems(PacketReader reader)
    {
        var itemCount = reader.ReadUInt32();
        var items = new List<QuestChoiceItem>((int)itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            items.Add(new QuestChoiceItem(
                reader.ReadUInt32(),
                reader.ReadUInt32(),
                reader.ReadUInt32()));
        }

        return items;
    }

    private sealed class PacketReader(byte[] data)
    {
        private readonly byte[] _data = data;
        private int _index;

        public byte ReadByte()
        {
            var value = _data[_index];
            _index++;
            return value;
        }

        public uint ReadUInt32()
        {
            var value = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(_index, 4));
            _index += 4;
            return value;
        }

        public int ReadInt32()
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(_index, 4));
            _index += 4;
            return value;
        }

        public float ReadSingle()
        {
            var raw = BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(_index, 4));
            _index += 4;
            return BitConverter.Int32BitsToSingle(raw);
        }

        public ulong ReadUInt64()
        {
            var value = BinaryPrimitives.ReadUInt64LittleEndian(_data.AsSpan(_index, 8));
            _index += 8;
            return value;
        }

        public string ReadCString()
        {
            var end = Array.IndexOf(_data, (byte)0, _index);
            if (end < 0)
            {
                end = _data.Length;
            }

            var value = Encoding.UTF8.GetString(_data, _index, end - _index);
            _index = Math.Min(end + 1, _data.Length);
            return value;
        }

        public void Skip(int count)
        {
            _index = Math.Min(_index + count, _data.Length);
        }
    }

    private readonly record struct RawObjectiveTarget(
        uint TargetId,
        uint RequiredCount,
        uint SourceItemId,
        uint SourceItemCount);
}
