using TotallyWholesome.Managers.Achievements.Conditions;
using TWNetCommon.Data.ControlPackets.Shockers.Models;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [PetCountCondition(6)]
    [ShockerActionMasterCondition(ControlType.Shock, 100, 15_000)]
    public class Chernobyl : IAchievement
    {
        public string AchievementName => "Chernobyl";
        public string AchievementDescription => "Send a 100% strength shock for 15 seconds to 6 pets!";
        public AchievementRank AchievementRank => AchievementRank.Silver;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}