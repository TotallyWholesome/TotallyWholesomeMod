using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [MasterRestrictionCondition(Restrictions.PinProp)]
    [PetRestrictionCondition(Restrictions.PinProp)]
    public class GettingAttached : IAchievement
    {
        public string AchievementName => "Getting Attached";
        public string AchievementDescription => "Attach or get attached to a prop";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.Any;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}