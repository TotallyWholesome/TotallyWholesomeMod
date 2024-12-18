using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[TWTagCondition]
public class SpecialTreatment2 : IAchievement
{
    public string AchievementName => "Special Treatment (fixed)";
    public string AchievementDescription => "Have a custom TW Rank Tag applied, this time it actually checks properly!";
    public AchievementRank AchievementRank => AchievementRank.Silver;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}