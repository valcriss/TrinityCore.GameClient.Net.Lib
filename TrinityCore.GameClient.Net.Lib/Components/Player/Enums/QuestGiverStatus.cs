namespace TrinityCore.GameClient.Net.Lib.Components.Player.Enums
{
    public enum QuestGiverStatus : sbyte
    {
        DIALOG_STATUS_NONE = 0,
        DIALOG_STATUS_UNAVAILABLE = 1,
        DIALOG_STATUS_LOW_LEVEL_AVAILABLE = 2,
        DIALOG_STATUS_LOW_LEVEL_REWARD_REP = 3,
        DIALOG_STATUS_LOW_LEVEL_AVAILABLE_REP = 4,
        DIALOG_STATUS_INCOMPLETE = 5,
        DIALOG_STATUS_REWARD_REP = 6,
        DIALOG_STATUS_AVAILABLE_REP = 7,
        DIALOG_STATUS_AVAILABLE = 8,
        DIALOG_STATUS_REWARD2 = 9, // no yellow dot on minimap
        DIALOG_STATUS_REWARD = 10 // yellow dot on minimap
    }
}
