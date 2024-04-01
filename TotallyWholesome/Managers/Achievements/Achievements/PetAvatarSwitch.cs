using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[AvatarSwitchCondition]
public class PetAvatarSwitch : IAchievement
{
    public string AchievementName => "The New Drip";
    public string AchievementDescription => "Have your avatar switched by your master";
    public AchievementRank AchievementRank => AchievementRank.Bronze;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}