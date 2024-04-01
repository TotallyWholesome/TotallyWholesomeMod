using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[HangingCondition(20)]
public class HangingOut : IAchievement
{
    public string AchievementName => "Hanging Out";
    public string AchievementDescription => "Hang above the ground on a leash for more then 20 seconds";
    public AchievementRank AchievementRank => AchievementRank.Silver;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}