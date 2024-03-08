namespace TotallyWholesome.Managers.Achievements
{
    public interface IAchievement
    {
        public string AchievementName { get; }

        public string AchievementDescription { get; }

        public AchievementRank AchievementRank { get; }

        public AchievementCheckMode AchievementCheckMode { get; }

        public AchievementConditionMode AchievementConditionMode { get; }

        public ICondition[] AchievementConditions { get; set; }

        public bool AchievementAwarded { get; set; }
    }

    public enum AchievementRank
    {
        Bronze,
        Silver,
        Gold
    }

    public enum AchievementConditionMode
    {
        All,
        Any
    }

    public enum AchievementCheckMode
    {
        Constant,
        PerSecond,
        PerMinute,
        Hourly,
    }
}