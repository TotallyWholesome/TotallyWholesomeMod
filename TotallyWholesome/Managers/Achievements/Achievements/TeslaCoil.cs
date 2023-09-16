using TotallyWholesome.Managers.Achievements.Conditions;
using TWNetCommon.Data.ControlPackets;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [PetCountCondition(1)]
    [PiShockOperationMasterCondition(ShockOperation.Shock, 100, 3)]
    public class TeslaCoil : IAchievement
    {
        public string AchievementName => "Tesla Coil";
        public string AchievementDescription => "Send a 3 second long 100% shock to a pet";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}