using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [RecreatedLeashCondition(1)]
    public class WelcomeBack : IAchievement
    {
        public string AchievementName => "Welcome Back!";
        public string AchievementDescription => "Have a leash be recreated after a pet or master joins the instance";
        public AchievementRank AchievementRank => AchievementRank.Silver;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerMinute;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}