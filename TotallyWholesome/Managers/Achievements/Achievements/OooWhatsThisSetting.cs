using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    //[MasterRestrictionCondition(Restrictions.ChangeRemoteParam)]
    public class OooWhatsThisSetting : IAchievement
    {
        public string AchievementName => "Oooo, What's this setting?";
        public string AchievementDescription => "Change a pets remote avatar parameter";
        public AchievementRank AchievementRank => AchievementRank.Bronze;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}