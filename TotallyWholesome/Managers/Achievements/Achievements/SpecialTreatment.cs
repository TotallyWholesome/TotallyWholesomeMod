using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[TWTagCondition]
public class SpecialTreatment : IAchievement
{
    public string AchievementName => "Special Treatment";
    public string AchievementDescription => "Have a custom TW Rank Tag applied";
    public AchievementRank AchievementRank => AchievementRank.Silver;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}