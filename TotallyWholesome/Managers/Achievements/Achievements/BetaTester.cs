using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [LoginCondition]
    [DateCondition(2023, 03, 18, 2023, 03, 31)]
    public class BetaTester : IAchievement
    {
        public string AchievementName => "Beta Tester!";
        public string AchievementDescription => "Play with Totally Wholesome 3.4 before April 1st!";
        public AchievementRank AchievementRank => AchievementRank.Gold;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.Disabled;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}