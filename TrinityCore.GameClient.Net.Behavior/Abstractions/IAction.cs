namespace TrinityCore.GameClient.Net.Behavior.Abstractions;

public interface IAction
{
    string Name { get; }
    int Cost { get; }
}
