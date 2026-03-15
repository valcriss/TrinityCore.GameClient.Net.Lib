namespace TrinityCore.GameClient.Net.Protocol.World;

public readonly record struct WorldPacketHeader(ushort Size, WorldOpcode Opcode, int HeaderLength);
