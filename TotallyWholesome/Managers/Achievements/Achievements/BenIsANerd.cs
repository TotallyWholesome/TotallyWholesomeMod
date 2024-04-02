using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[LeashUserCondition(true, "95fa1cdd-51bc-8a91-8160-69ad9b96b3b2")]
public class BenIsANerd : IAchievement
{
    public string AchievementName => "14 Werewolves";
    public string AchievementDescription => "Have Ben become your master!";
    public AchievementRank AchievementRank => AchievementRank.Gold;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}