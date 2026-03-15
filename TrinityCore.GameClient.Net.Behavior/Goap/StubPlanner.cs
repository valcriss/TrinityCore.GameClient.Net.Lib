using TrinityCore.GameClient.Net.Behavior.Abstractions;

namespace TrinityCore.GameClient.Net.Behavior.Goap;

public sealed class StubPlanner : IPlanner
{
    public IReadOnlyList<IAction> Plan(IGoal goal) => [];
}
