using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[PetTimeCondition(60)]
public class FullTimePet : IAchievement
{
    public string AchievementName => "Full Time Pet";
    public string AchievementDescription => "Be leashed for an hour continuously";
    public AchievementRank AchievementRank => AchievementRank.Silver;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}