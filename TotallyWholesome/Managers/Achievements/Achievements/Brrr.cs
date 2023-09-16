using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [ButtplugDeviceCondition(1)]
    [VibrationDurationCondition(60, 50f)]
    public class Brrr : IAchievement
    {
        public string AchievementName => "Brrr!";
        public string AchievementDescription => "Have your vibe activated for 60 seconds at 50% higher!";
        public AchievementRank AchievementRank => AchievementRank.Silver;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.Constant;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}