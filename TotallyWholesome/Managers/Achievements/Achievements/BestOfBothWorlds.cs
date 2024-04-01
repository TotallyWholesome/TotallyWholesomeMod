using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[PetCountCondition(1)]
[HasMasterCondition]
public class BestOfBothWorlds : IAchievement
{
    public string AchievementName => "Best of Both Worlds";
    public string AchievementDescription => "Be someone's master and someone else's pet at the same time!";
    public AchievementRank AchievementRank => AchievementRank.Bronze;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}