using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [PetCountCondition(6)]
    public class GottaCatchEmAll : IAchievement
    {
        public string AchievementName => "Gotta Catch 'Em All";
        public string AchievementDescription => "Have the maximum amount of pets allowed at one time";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}