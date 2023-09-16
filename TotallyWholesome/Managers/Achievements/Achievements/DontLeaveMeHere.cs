using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [MasterRestrictionCondition(Restrictions.PinWorld)]
    [PetRestrictionCondition(Restrictions.PinWorld)]
    public class DontLeaveMeHere : IAchievement
    {
        public string AchievementName => "Don't leave me here!";
        public string AchievementDescription => "Use world position pinning!";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.Any;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}