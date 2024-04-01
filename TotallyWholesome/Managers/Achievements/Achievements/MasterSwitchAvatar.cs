using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[MasterAvatarSwitchCondition]
public class MasterSwitchAvatar : IAchievement
{
    public string AchievementName => "That Looks Better";
    public string AchievementDescription => "Use remote avatar switching on your pet";
    public AchievementRank AchievementRank => AchievementRank.Bronze;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}