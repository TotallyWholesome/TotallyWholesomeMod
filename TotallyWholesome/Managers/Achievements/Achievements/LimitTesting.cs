using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [LeashDistanceCondition(9.5f)]
    [PetCountCondition(6)]
    public class LimitTesting : IAchievement
    {
        public string AchievementName => "Limit Testing";
        public string AchievementDescription => "Have the maximum allowed pets at the max leash distance";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.Constant;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}