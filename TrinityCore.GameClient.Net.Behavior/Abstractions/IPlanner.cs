namespace TrinityCore.GameClient.Net.Behavior.Abstractions;

public interface IPlanner
{
    IReadOnlyList<IAction> Plan(IGoal goal);
}
