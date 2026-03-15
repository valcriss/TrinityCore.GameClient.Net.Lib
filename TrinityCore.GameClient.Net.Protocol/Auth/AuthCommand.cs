namespace TrinityCore.GameClient.Net.Protocol.Auth;

public enum AuthCommand : byte
{
    LogonChallenge = 0x00,
    LogonProof = 0x01,
    RealmList = 0x10
}
