using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[MasterAvatarSwitchCondition(6)]
[AvatarSwitchCondition(6)]
public class Indecisive : IAchievement
{
    public string AchievementName => "Indecisive";
    public string AchievementDescription => "Switch your pets or have your avatar switch 6 times";
    public AchievementRank AchievementRank => AchievementRank.Silver;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.Any;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}