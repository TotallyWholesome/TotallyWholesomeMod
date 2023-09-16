using TotallyWholesome.Managers.Achievements.Conditions;
using TWNetCommon.Data.ControlPackets;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [PiShockOperationPetCondition(ShockOperation.Shock, 100, 3)]
    public class Capacitor : IAchievement
    {
        public string AchievementName => "Capacitor";
        public string AchievementDescription => "Receive a 3 second or longer shock at 100% strength!";
        public AchievementRank AchievementRank => AchievementRank.Silver;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}