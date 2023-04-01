using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;
using Newtonsoft.Json;
using TotallyWholesome.Notification;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome.Managers.Achievements
{
    public class AchievementManager : ITWManager
    {
        public static AchievementManager Instance;
        
        public string ManagerName() => "AchievementManager";
        public int Priority() => 0;

        public List<IAchievement> LoadedAchievements = new();
        public bool AchievementsUpdated;

        private string _achievementStore = Path.Combine(Configuration.SettingsPath, "AchievementStore.json");
        private List<string> _awardedAchievements = new();

        public void Setup()
        {
            Instance = this;
            
            var interfaceType = typeof(IAchievement);
            var achievements = Assembly.GetExecutingAssembly().GetTypes().Where(x => interfaceType.IsAssignableFrom(x) && x != interfaceType).ToArray();
            
            //TODO: Switch this to be stored serverside
            try
            {
                if(File.Exists(_achievementStore))
                    _awardedAchievements = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(_achievementStore));
            }
            catch (Exception e)
            {
                Con.Error("Unable to load achievement store! Your achievement status has been reset!");
                Con.Error(e);
            }

            foreach (var achievementType in achievements)
            {
                IAchievement achievementInstance = Activator.CreateInstance(achievementType) as IAchievement;

                if (achievementInstance == null)
                {
                    Con.Error($"Unable create instance of achievement {achievementType.FullName}!");
                    continue;
                }

                var conditionType = typeof(ICondition);
                var conditions = achievementType.GetCustomAttributes(false).Where(x => conditionType.IsInstanceOfType(x)).ToArray();

                List<ICondition> conditionAttributes = new List<ICondition>();

                foreach (var condition in conditions)
                {
                    ICondition convert = (ICondition)condition;
                    conditionAttributes.Add(convert);
                }

                achievementInstance.AchievementConditions = conditionAttributes.ToArray();

                achievementInstance.AchievementAwarded = _awardedAchievements.Contains(achievementType.Name);

                LoadedAchievements.Add(achievementInstance);
            }

            AchievementsUpdated = true;
            
            Patches.Patches.EarlyWorldJoin += () =>
            {
                //Start AchievementManagerCoroutine for processing conditions 
                MelonCoroutines.Start(AchievementManagerCoroutine());
                MelonCoroutines.Start(AchievementManagerCoroutineMinute());
                MelonCoroutines.Start(AchievementManagerCoroutineHourly());
                MelonCoroutines.Start(AchievementManagerCoroutineSecond());
            };
            
            Con.Debug($"AchievementManager has loaded {LoadedAchievements.Count} achievements and is ready!");
        }
        
        public void LateSetup()
        {
            
        }

        private void AwardAchievement(IAchievement achievement)
        {
            switch (achievement.AchievementRank)
            {
                case AchievementRank.Bronze:
                    NotificationSystem.EnqueueAchievement(achievement.AchievementName, TWAssets.BadgeBronze);
                    break;
                case AchievementRank.Silver:
                    NotificationSystem.EnqueueAchievement(achievement.AchievementName, TWAssets.BadgeSilver);
                    break;
                case AchievementRank.Gold:
                    NotificationSystem.EnqueueAchievement(achievement.AchievementName, TWAssets.BadgeGold);
                    break;
            }
            Con.Debug($"Achievement Awarded: {achievement.AchievementName} | {achievement.AchievementDescription} | {achievement.AchievementRank}");
            achievement.AchievementAwarded = true;
            AchievementsUpdated = true;
            _awardedAchievements.Add(achievement.GetType().Name);
            File.WriteAllText(_achievementStore, JsonConvert.SerializeObject(_awardedAchievements));
        }

        #region Coroutines

        private IEnumerator AchievementManagerCoroutine()
        {
            var achievements = LoadedAchievements.Where(x => !x.AchievementAwarded && x.AchievementCheckMode == AchievementCheckMode.Constant).ToArray();
            
            while (!Main.Instance.Quitting)
            {
                //Run checks
                foreach (var achievement in achievements)
                {
                    if(achievement.AchievementAwarded) continue;
                    
                    if (achievement.AchievementConditionMode == AchievementConditionMode.Any && achievement.AchievementConditions.Any(x => x.CheckCondition()))
                    {
                        AwardAchievement(achievement);
                        continue;
                    }
                    
                    if (achievement.AchievementConditions.All(x => x.CheckCondition()))
                    {
                        //Award achievement
                        AwardAchievement(achievement);
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private IEnumerator AchievementManagerCoroutineSecond()
        {
            var achievements = LoadedAchievements.Where(x => !x.AchievementAwarded && x.AchievementCheckMode == AchievementCheckMode.PerSecond).ToArray();
            
            while (!Main.Instance.Quitting)
            {
                //Run checks
                foreach (var achievement in achievements)
                {
                    if(achievement.AchievementAwarded) continue;

                    if (achievement.AchievementConditionMode == AchievementConditionMode.Any && achievement.AchievementConditions.Any(x => x.CheckCondition()))
                    {
                        AwardAchievement(achievement);
                        continue;
                    }
                    
                    if (achievement.AchievementConditions.All(x => x.CheckCondition()))
                    {
                        //Award achievement
                        AwardAchievement(achievement);
                    }
                }
                
                yield return new WaitForSeconds(1f);
            }
        }
        
        private IEnumerator AchievementManagerCoroutineMinute()
        {
            var achievements = LoadedAchievements.Where(x => !x.AchievementAwarded && x.AchievementCheckMode == AchievementCheckMode.PerMinute).ToArray();
            
            while (!Main.Instance.Quitting)
            {
                //Run checks
                foreach (var achievement in achievements)
                {
                    if(achievement.AchievementAwarded) continue;
                    
                    if (achievement.AchievementConditionMode == AchievementConditionMode.Any && achievement.AchievementConditions.Any(x => x.CheckCondition()))
                    {
                        AwardAchievement(achievement);
                        continue;
                    }
                    
                    if (achievement.AchievementConditions.All(x => x.CheckCondition()))
                    {
                        //Award achievement
                        AwardAchievement(achievement);
                    }
                }
                
                yield return new WaitForSeconds(60f);
            }
        }
        
        private IEnumerator AchievementManagerCoroutineHourly()
        {
            var achievements = LoadedAchievements.Where(x => !x.AchievementAwarded && x.AchievementCheckMode == AchievementCheckMode.Hourly).ToArray();
            
            while (!Main.Instance.Quitting)
            {
                //Run checks
                foreach (var achievement in achievements)
                {
                    if(achievement.AchievementAwarded) continue;
                    
                    if (achievement.AchievementConditionMode == AchievementConditionMode.Any && achievement.AchievementConditions.Any(x => x.CheckCondition()))
                    {
                        AwardAchievement(achievement);
                        continue;
                    }
                    
                    if (achievement.AchievementConditions.All(x => x.CheckCondition()))
                    {
                        //Award achievement
                        AwardAchievement(achievement);
                    }
                }
                
                yield return new WaitForSeconds(3600f);
            }
        }

        #endregion
    }
}