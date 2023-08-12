using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using MelonLoader;
using TotallyWholesome.Managers;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome
{
    public static class BuildInfo
    {
        public const string Name = "TotallyWholesome";
        public const string Author = "Totally Wholesome Team";
        public const string Company = "TotallyWholesome";
        public const string AssemblyVersion = "3.4.23";
        public const string TWVersion = "3.4.23";
        public const bool isBetaBuild = false;
        public const string DownloadLink = "https://totallywholeso.me/";
    }

    public class Main : MelonMod
    {
        public static Main Instance;

        public static int CurrentTOSLevel = 1;

        public Queue<Action> MainThreadQueue = new Queue<Action>();
        public bool Quitting;
        public string TargetWorld;

        private TWNetClient _twNetClient;
        private Thread _mainThread;
        private bool _openedTosPopup;
        private bool _firstWorldJoin;
        private List<ITWManager> _managers;
        
        //Temporary to allow updates to rollout for the new loader
        public override void OnApplicationStart()
        {
            OnInitializeMelon();
        }
        
        public override void OnInitializeMelon()
        {
            Instance = this;

            
            Con.Msg($"Welcome to Totally Wholesome! You are on version {BuildInfo.TWVersion} {(BuildInfo.isBetaBuild ? "Beta Build" : "Release Build")}");
            #endif

            Patches.SetupPatches();

            Patches.OnUserLogin += OnPlayerLoggedIn;
            Patches.OnWorldLeave += TWUtils.LeaveWorld;
            Patches.EarlyWorldJoin += TWUtils.LeaveWorld;
            Patches.UserLeave += TWUtils.UserLeave;
            Patches.EarlyWorldJoin += EarlyWorldJoin;

            _mainThread = Thread.CurrentThread;
            
            new ConfigManager().Setup();

            Configuration.Initialize();
            Configuration.SaveConfig(); // Save to build/Add new Config Items to json

            _twNetClient = new TWNetClient();

            //Load our sprite assets
            TWAssets.LoadAssets();

            var type = typeof(ITWManager);
            _managers = new List<ITWManager>();
            foreach (var manager in Assembly.GetExecutingAssembly().DefinedTypes
                         .Where(x => x.ImplementedInterfaces.Contains(type)).OrderBy(x => x.Name))
            {
                if (!(Activator.CreateInstance(manager) is ITWManager twManager)) continue;
                _managers.Add(twManager);
            }
            
            foreach (var manager in _managers.OrderByDescending(x => x.Priority()))
            {
                try
                {
                    Con.Debug($"Loading TW Manager {manager.ManagerName()}");
                    manager.Setup();
                }
                catch (Exception e)
                {
                    Con.Error($"TW Manager {manager.ManagerName()} failed to load!");
                    Con.Error(e);
                }
            }
        }

        public bool IsOnMainThread(Thread thread)
        {
            return thread.Equals(_mainThread);
        }

        private void OnPlayerLoggedIn()
        {
            Con.Msg("Attempting auth with TWNet...");

            if (Configuration.JSONConfig.AcceptedTOS >= CurrentTOSLevel)
            {
                _openedTosPopup = true;
                _twNetClient.ConnectClient();
            }
            else
            {
                Con.Debug("User has not accept TOS yet, waiting before connection!");
            }
        }

        private IEnumerator WaitForKeyPopup()
        {
            yield return new WaitForSeconds(5f);

            if (_openedTosPopup) yield break;

            NotificationSystem.EnqueueNotification("TW Net", "Before you can use Totally Wholesome you must accept our End User License Agreement, please go to the Totally Wholesome tab in your Quick Menu!", 10f, TWAssets.Megaphone);

            _openedTosPopup = true;
        }
        
        private void EarlyWorldJoin()
        {
            if (!_firstWorldJoin)
            {
                _firstWorldJoin = true;
                
                Patches.LateStartupPatches();

                NotificationSystem.UseCVRNotificationSystem = ConfigManager.Instance.IsActive(AccessType.UseOldHudMessage);
                NotificationSystem.SetupNotifications();
                
                //Add our actions into CVRs join and leave
                CVRPlayerManager.Instance.OnPlayerEntityCreated += entity => Patches.UserJoin.Invoke(entity);
                CVRPlayerManager.Instance.OnPlayerEntityRecycled += entity => Patches.UserLeave.Invoke(entity); 
                
                PlayerSetup.Instance.avatarSetupCompleted.AddListener(() =>
                {
                    Patches.OnAvatarInstantiated?.Invoke(MetaPort.Instance.ownerId);
                });
                
                Con.Debug("Calling LateSetup on loaded managers");
                
                foreach (var manager in _managers.OrderByDescending(x => x.Priority()))
                {
                    try
                    {
                        manager.LateSetup();
                    }
                    catch (Exception e)
                    {
                        Con.Error($"TW Manager {manager.ManagerName()} failed during LateSetup!");
                        Con.Error(e);
                    }
                }
                
                if (Configuration.JSONConfig.AcceptedTOS < CurrentTOSLevel)
                    MelonCoroutines.Start(WaitForKeyPopup());
            }
        }

        public override void OnUpdate()
        {
            //Fire any queued actions on main thread
            if (MainThreadQueue.Count > 0)
                MainThreadQueue.Dequeue().Invoke();
        }

        public override void OnApplicationQuit()
        {
            Con.Debug("Closing connection to TWNet!");
            Quitting = true;
            TWNetClient.Instance.DisconnectClient();
            ButtplugManager.Instance.ShutDown();
        }
    }
}
