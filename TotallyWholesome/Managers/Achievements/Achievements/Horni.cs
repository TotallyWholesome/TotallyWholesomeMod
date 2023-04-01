using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements
{
    [UserExistsCondition("047b30bd-089d-887c-8734-b0032df5d176")]
    public class Horni : IAchievement
    {
        public string AchievementName => "Hornini";
        public string AchievementDescription => "Be in the same instance as Hordini!";
        public AchievementRank AchievementRank => AchievementRank.Silver;
        public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
        public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
        public ICondition[] AchievementConditions { get; set; }
        public bool AchievementAwarded { get; set; }
    }
}