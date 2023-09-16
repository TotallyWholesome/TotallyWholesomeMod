using TotallyWholesome.Managers.Achievements.Conditions;
using TWNetCommon.Data.ControlPackets;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [PetCountCondition(6)]
    [PiShockOperationGlobalCondition]
    [PiShockOperationMasterCondition(ShockOperation.Shock, 100, 15)]
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