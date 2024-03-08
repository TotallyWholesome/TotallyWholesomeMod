using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[LoginCondition]
[DateCondition(2024, 02, 02, 2024, 03, 7)]
public class BetaTester35 : IAchievement
{
    public string AchievementName => "More Beta Testing!";
    public string AchievementDescription => "Play with Totally Wholesome 3.5 during the closed beta testing!";
    public AchievementRank AchievementRank => AchievementRank.Gold;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.Hourly;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}