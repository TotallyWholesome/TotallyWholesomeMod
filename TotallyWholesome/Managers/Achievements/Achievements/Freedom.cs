using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    //[MasterRestrictionCondition(Restrictions.TempUnlock)]
    [PetRestrictionCondition(Restrictions.TempUnlock)]
    public class Freedom : IAchievement
    {
        public string AchievementName => "Freedom!";
        public string AchievementDescription => "Use temporary leash unlock";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.Any;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}