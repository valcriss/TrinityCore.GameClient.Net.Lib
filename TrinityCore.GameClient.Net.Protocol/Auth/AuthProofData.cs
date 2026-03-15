using System.Numerics;

namespace TrinityCore.GameClient.Net.Protocol.Auth;

public sealed record AuthProofData(BigInteger SessionKey, byte[] SessionKeyBytes, byte[] ClientPublicEphemeral, byte[] ClientM1, byte[] ExpectedM2);
