using TotallyWholesome.Managers.Achievements.Conditions;
using TWNetCommon.Data.ControlPackets.Shockers.Models;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [ShockerActionPetCondition(ControlType.Shock, 1, 1_000)]
    [VibrationDurationCondition(0, 1)]
    [PetRestrictionCondition(Restrictions.Blindfolded, Restrictions.Deafened, Restrictions.Gagged)]
    public class WomboCombo : IAchievement
    {
        public string AchievementName => "Wombo Combo!";
        public string AchievementDescription => "Get shocked and vibrated while blindfolded, deafened, and gagged!";
        public AchievementRank AchievementRank => AchievementRank.Gold;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}