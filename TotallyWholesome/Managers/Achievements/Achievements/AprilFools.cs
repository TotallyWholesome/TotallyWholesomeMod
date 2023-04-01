using System;
using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [LoginCondition]
    [DateCondition(2023, 4, 1, 2023, 4, 2)]
    public class AprilFools : IAchievement
    {
        public string AchievementName => "You want sum fuk?";
        public string AchievementDescription => "Connect to TWNet on April 1st 2023!";
        public AchievementRank AchievementRank => AchievementRank.Gold;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.Hourly;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}