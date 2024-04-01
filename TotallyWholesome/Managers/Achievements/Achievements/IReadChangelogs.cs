using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[HangingCondition(30)]
[AvatarSwitchCondition(4)]
[MasterBypassCondition]
[SwitchableAvatarsCondition(2)]
public class IReadChangelogs : IAchievement
{
    public string AchievementName => "I Read Changelogs!";
    public string AchievementDescription => "Have all the new features used on you at once!";
    public AchievementRank AchievementRank => AchievementRank.Gold;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}