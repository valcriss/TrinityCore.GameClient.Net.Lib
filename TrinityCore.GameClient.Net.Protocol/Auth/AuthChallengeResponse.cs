namespace TrinityCore.GameClient.Net.Protocol.Auth;

public sealed record AuthChallengeResponse(AuthResult Result, byte[] B, byte[] G, byte[] N, byte[] Salt, byte[] Unknown3);
