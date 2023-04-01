using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [MasterRestrictionCondition(Restrictions.Gagged)]
    [PetRestrictionCondition(Restrictions.Gagged)]
    public class ChokedUp : IAchievement
    {
        public string AchievementName => "Choked up!";
        public string AchievementDescription => "Gag or get gagged";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.Any;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}