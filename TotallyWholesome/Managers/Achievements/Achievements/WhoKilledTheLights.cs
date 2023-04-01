using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [MasterRestrictionCondition(Restrictions.Blindfolded)]
    [PetRestrictionCondition(Restrictions.Blindfolded)]
    public class WhoKilledTheLights : IAchievement
    {
        public string AchievementName => "Who killed the lights?";
        public string AchievementDescription => "Be blinded or blind your pet";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.Any;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}