using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[HasMasterCondition]
[MasterBypassCondition]
public class CanYouHearMe : IAchievement
{
    public string AchievementName => "Can you hear me now?";
    public string AchievementDescription => "Have your master enable deafen bypass allowing you to hear them clearly when deafened";
    public AchievementRank AchievementRank => AchievementRank.Bronze;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}