using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [TugOfWarPullCondition(30)]
    public class TugOfWar : IAchievement
    {
        public string AchievementName => "Tug of War!";
        public string AchievementDescription => "Pull away from each other for atleast 30 seconds!";
        public AchievementRank AchievementRank => AchievementRank.Gold;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}