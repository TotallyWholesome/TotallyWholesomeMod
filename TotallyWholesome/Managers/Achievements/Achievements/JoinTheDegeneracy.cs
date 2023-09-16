using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [LoginCondition]
    public class JoinTheDegeneracy : IAchievement
    {
        public string AchievementName => "Join the Degeneracy!";
        public string AchievementDescription => "Connect to TWNet after the Totally Wholesome 3.4 update!";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }

        public JoinTheDegeneracy()
        {
        }
    }
}