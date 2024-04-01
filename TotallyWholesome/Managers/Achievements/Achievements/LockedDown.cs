using TotallyWholesome.Managers.Achievements.Conditions;

namespace TotallyWholesome.Managers.Achievements.Achievements;

[PetRestrictionCondition(Restrictions.Blindfolded, Restrictions.Deafened, Restrictions.Gagged, Restrictions.NoFlight, Restrictions.NoSeats, Restrictions.PinEither)]
public class LockedDown : IAchievement
{
    public string AchievementName => "Locked Down";
    public string AchievementDescription => "Have every restriction enabled on you as a pet";
    public AchievementRank AchievementRank => AchievementRank.Silver;
    public AchievementCheckMode AchievementCheckMode => AchievementCheckMode.PerSecond;
    public AchievementConditionMode AchievementConditionMode => AchievementConditionMode.All;
    public ICondition[] AchievementConditions { get; set; }
    public bool AchievementAwarded { get; set; }
}