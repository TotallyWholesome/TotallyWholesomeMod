using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [RecreatedLeashCondition(6)]
    public class PortalMagic : IAchievement
    {
        public string AchievementName => "Portal Magic";
        public string AchievementDescription => "Get dragged into 6 new instances and have the leash be created on the other side";
        public AchievementRank AchievementRank => AchievementRank.Gold;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
        public AchievementConditionMode AchievementConditionMode { get; }
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}