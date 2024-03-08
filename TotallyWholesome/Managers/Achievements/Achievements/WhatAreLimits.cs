using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [ConfigManagerCondition(AccessType.AllowBeep, AccessType.AllowBlindfolding, AccessType.AllowDeafening, AccessType.AllowShock, AccessType.AllowShock, AccessType.AllowVibrate, AccessType.AllowForceMute, AccessType.AllowHeightControl, AccessType.AllowMovementControls, AccessType.AllowShockControl, AccessType.AllowToyControl, AccessType.EnableToyControl, AccessType.AllowWorldPropPinning, AccessType.AutoAcceptMasterRequest, AccessType.AutoAcceptPetRequest, AccessType.FollowMasterWorldChange)]
    [ShockerNoLimitsLimitCondition]
    public class WhatAreLimits : IAchievement
    {
        public string AchievementName => "What are limits?";
        public string AchievementDescription => "Have all consent options enabled and have no limits set on your PiShock shocker";
        public AchievementRank AchievementRank => AchievementRank.Silver;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}