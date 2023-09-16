using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [PetCountCondition(1)]
    [LeashDistanceCondition(0f, DistanceCheckMode.Equal)]
    public class GetOverHere : IAchievement
    {
        public string AchievementName => "Get over here!";
        public string AchievementDescription => "Reduce the leash distance to the minimum";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}