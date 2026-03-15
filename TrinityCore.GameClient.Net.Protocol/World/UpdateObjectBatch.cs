namespace TrinityCore.GameClient.Net.Protocol.World;

public sealed record MovementSnapshot(float X, float Y, float Z, float O);

public sealed record UpdateEntityValues(
    ulong Guid,
    IReadOnlyDictionary<int, uint> Fields,
    byte? TypeId = null,
    MovementSnapshot? Movement = null);

public sealed record UpdateObjectBatch(IReadOnlyList<UpdateEntityValues> Values, IReadOnlyList<ulong> OutOfRangeGuids);
