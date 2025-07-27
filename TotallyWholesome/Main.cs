using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util.AssetFiltering;
using ABI_RC.Systems.GameEventSystem;
using BTKUILib;
using MelonLoader;
using Semver;
using TotallyWholesome.Managers;
using TotallyWholesome.Managers.Lead.LeadComponents;
using TotallyWholesome.Managers.Shockers;
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
        public const string AssemblyVersion = "3.6.12";
        public const string TWVersion = "3.6.12-nightly";
        public const string DownloadLink = "https://totallywholeso.me/";
    }

    public class Main : MelonMod
    {
        public static Main Instance;

        public static int CurrentTOSLevel = 1;

        public ConcurrentQueue<Action> MainThreadQueue = new();
        public bool Quitting;

        private TWNetClient _twNetClient;
        private Thread _mainThread;
        private bool _openedTosPopup;
        private bool _firstWorldJoin;
        private List<ITWManager> _managers;
        
        public override void OnInitializeMelon()
        {
            Instance = this;

            
            #if BETA && !ADMIN
            Con.Msg($"Welcome to Totally Wholesome! You are on version {BuildInfo.TWVersion} Beta Build Commit: {ThisAssembly.Git.Commit}|{ThisAssembly.Git.Branch}");
            #elif !BETA
            Con.Msg($"Welcome to Totally Wholesome! You are on version {BuildInfo.TWVersion} Release Build Commit: {ThisAssembly.Git.Commit}|{ThisAssembly.Git.Branch}");
            


            if (!RegisteredMelons.Any(x => x.Info.Name.Equals("BTKUILib") && x.Info.SemanticVersion != null && x.Info.SemanticVersion.CompareTo(new SemVersion(2, 1)) >= 0))
            {
                Con.Error("BTKUILib was not detected or it outdated! TotallyWholesome cannot function without it!");
                Con.Error("Please download BTKUILib version 2.0.0 or greater!");
                return;
            }

            var dir = "ChilloutVR_Data\\StreamingAssets\\Cohtml\\UIResources\\GameUI\\mods\\TWUI";

            if (Directory.Exists(dir))
            {
                Con.Msg("Detected legacy TWUI, deleting!");
                Directory.Delete(dir, true);
            }

            Patches.SetupPatches();

            Patches.OnUserLogin += OnPlayerLoggedIn;
            QuickMenuAPI.OnMenuRegenerate += EarlyWorldJoin;

            // CVR events
            CVRGameEventSystem.Authentication.OnLogin.AddListener(_ => Patches.OnUserLogin?.Invoke());
            
            _mainThread = Thread.CurrentThread;

            Configuration.Initialize();
            Configuration.SaveConfig(); // Save to build/Add new Config Items to json

            _twNetClient = new TWNetClient();

            //Load our sprite assets
            TWAssets.LoadAssets();

            // TODO: Think of something to do with this, and make it prettier, kthxbye
            
            var type = typeof(ITWManager);
            _managers = new List<ITWManager>();
            foreach (var manager in Assembly.GetExecutingAssembly().DefinedTypes
                         .Where(x => x.ImplementedInterfaces.Contains(type)).OrderBy(x => x.Name))
            {
                if (!(Activator.CreateInstance(manager) is ITWManager twManager)) continue;
                _managers.Add(twManager);
            }
            
            foreach (var manager in _managers.OrderByDescending(x => x.Priority))
            {
                try
                {
                    Con.Debug($"Loading TW Manager {manager.GetType().FullName}");
                    manager.Setup();
                }
                catch (Exception e)
                {
                    Con.Error($"TW Manager {manager.GetType().FullName} failed to setup!", e);
                }
            }
        }

        internal bool IsOnMainThread(Thread thread = null)
        {
            thread ??= Thread.CurrentThread;

            return thread.Equals(_mainThread);
        }

        private void OnPlayerLoggedIn()
        {
            Con.Msg("Attempting auth with TWNet...");

            //Add CustomLeashMatConfig to avatar whitelist
            SharedFilter.AvatarWhitelist.Add(typeof(CustomLeashMatConfig));

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
        
        private void EarlyWorldJoin(CVR_MenuManager _)
        {
            if (!_firstWorldJoin)
            {
                _firstWorldJoin = true;
                
                Patches.LateStartupPatches();

                NotificationSystem.UseCVRNotificationSystem = ConfigManager.Instance.IsActive(AccessType.UseOldHudMessage);
                
                //Add our actions into CVRs join and leave
                QuickMenuAPI.UserJoin += entity => Patches.UserJoin.Invoke(entity);
                QuickMenuAPI.UserLeave += entity => Patches.UserLeave.Invoke(entity);

                CVRGameEventSystem.Avatar.OnLocalAvatarLoad.AddListener((avatar) =>
                {
                    Patches.OnAvatarInstantiated?.Invoke(MetaPort.Instance.ownerId);
                });
                
                Con.Debug("Calling LateSetup on loaded managers");
                
                
                foreach (var manager in _managers.OrderByDescending(x => x.Priority))
                {
                    try
                    {
                        manager.LateSetup();
                    }
                    catch (Exception e)
                    {
                        Con.Error($"TW Manager {manager.GetType().FullName} failed during LateSetup!", e);
                    }
                }
                
                if (Configuration.JSONConfig.AcceptedTOS < CurrentTOSLevel)
                    MelonCoroutines.Start(WaitForKeyPopup());
            }
        }

        public override void OnUpdate()
        {
            //Fire any queued actions on main thread
            if (MainThreadQueue.IsEmpty) return;
            if (MainThreadQueue.TryDequeue(out var item))
                item.Invoke();
        }

        public override async void OnApplicationQuit()
        {
            Con.Debug("Closing connection to TWNet!");
            Quitting = true;
            TWNetClient.Instance.DisconnectClient();
            ButtplugManager.Instance.ShutDown();
            if (ShockerManager.Instance.ShockerProvider is not IAsyncDisposable disposable) return;
            await disposable.DisposeAsync();
        }
    }
}
