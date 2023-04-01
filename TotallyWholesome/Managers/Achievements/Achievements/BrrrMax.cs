using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [ButtplugDeviceCondition(1)]
    [VibrationDurationCondition(60, 100f)]
    public class BrrrMax : IAchievement
    {
        public string AchievementName => "Vrrrrrrrrrr!";
        public string AchievementDescription => "Have your vibe activated for 60 seconds at 100%!";
        public AchievementRank AchievementRank => AchievementRank.Gold;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.Constant;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}