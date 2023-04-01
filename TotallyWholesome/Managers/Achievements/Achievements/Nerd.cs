using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [UserExistsCondition("5301af21-eb8d-7b36-3ef4-b623fa51c2c6")]
    public class Nerd : IAchievement
    {
        public string AchievementName => "Nerd!";
        public string AchievementDescription => "Be in the same instance as DDAkebono!";
        public AchievementRank AchievementRank => AchievementRank.Silver;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}