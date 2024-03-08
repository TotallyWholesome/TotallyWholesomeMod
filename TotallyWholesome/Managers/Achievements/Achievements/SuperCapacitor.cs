using TotallyWholesome.Managers.Achievements.Conditions;
using TWNetCommon.Data.ControlPackets.Shockers.Models;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [ShockerActionPetCondition(ControlType.Shock, 100, 15_000)]
    public class SuperCapacitor : IAchievement
    {
        public string AchievementName => "Super Capacitor";
        public string AchievementDescription => "Receive a 15 second shock at 100% strength!";
        public AchievementRank AchievementRank => AchievementRank.Gold;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}