using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[ButtplugDeviceCondition(1)]
public class ButtplugDeviceConnected : IAchievement
{
    public string AchievementName => "( \u0361\u00b0 \u035cʖ \u0361\u00b0)";
    public string AchievementDescription => "Connect a toy that uses Intiface/Buttplug.io!";
    public AchievementRank AchievementRank => AchievementRank.Bronze;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}