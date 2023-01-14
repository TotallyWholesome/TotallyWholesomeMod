using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ABI_RC.Core;
using ABI_RC.Core.Base;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.UI;
using ABI_RC.Systems.MovementSystem;
using ABI.CCK.Components;
using ABI.CCK.Scripts;
using DarkRift.Client;
using Dissonance.Audio.Capture;
using HarmonyLib;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using UnityEngine;
using WholesomeLoader;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace TotallyWholesome.Patches
{
    internal class Patches
    {
        public static Action<PlayerNameplate> OnNameplateRebuild;        
        public static Action OnUserLogin;
        public static Action EarlyWorldJoin;
        public static Action<CVRPlayerEntity> UserJoin;
        public static Action<CVRPlayerEntity> UserLeave;
        public static Action<RichPresenceInstance_t> OnWorldJoin;
        public static Action OnWorldLeave;
        public static Action OnGameNetworkConnected;
        public static Action<CVR_MenuManager> OnMarkMenuAsReady;
        public static Action<string> OnAvatarInstantiated;
        public static Action OnLocalAvatarReady;
        public static Action<string> OnKeyboardSubmitted;
        public static Action<Invite_t> OnInviteAccepted;
        public static Action OnChangingInstance;
        public static Action<CVRSpawnable> OnPropSpawned;
        public static Action<CVRSpawnable> OnPropDelete;

        public static bool IsForceMuted;
        public static bool IsMuffled;
        public static bool IsFlightLocked;
        public static bool AreSeatsLocked;
        public static string LastRichPresenseUpdate;
        public static string TargetWorldID;
        public static string TargetInstanceID;
        public static List<Invite_t> TWInvites = new();
        //These 2 numbers are still magic, changing it too far from these breaks the filter entirely
        public static float MagicFilterLevel = 433.1509f;
        public static float QLevel = 0.003219661f;

        public static DateTime TimeSinceLastUnmute = DateTime.Now;
        public static DateTime TimeSinceKeyboardOpen = DateTime.Now;

        private static float[] _a = new float[3];
        private static float[] _b = new float[3];

        private static float _in1, _in2, _out1, _out2;

        private static void ApplyPatches(Type type)
        {
            Con.Debug($"Applying {type.Name} patches!");
            try {
                HarmonyLib.Harmony.CreateAndPatchAll(type, BuildInfo.Name + "_Hooks");
            } catch (Exception e) {
                Con.Error($"Failed while patching {type.Name}!\n{e}");
            }
        }

        public static void SetupPatches()
        {
            Con.Debug("Setting up Patches...");
            
            ApplyPatches(typeof(NameplatePatches));
            ApplyPatches(typeof(CVRPlayerManagerJoin));
            ApplyPatches(typeof(CVRPlayerEntityLeave));
            ApplyPatches(typeof(AuthManagerPatches));
            ApplyPatches(typeof(NetworkManagerPatches));
            ApplyPatches(typeof(RichPresensePatch));
            ApplyPatches(typeof(InstancesPatches));
            ApplyPatches(typeof(CVRMenuManagerPatch));
            ApplyPatches(typeof(PuppetMasterPatch));
            ApplyPatches(typeof(ViewManagerPatches));
            ApplyPatches(typeof(MicrophoneCapturePatch));
            ApplyPatches(typeof(MicrophoneMutePatch));
            ApplyPatches(typeof(CVRObjectLoaderPatch));
            ApplyPatches(typeof(PlayerSetupPatches));
            ApplyPatches(typeof(AdvancedAvatarSettingsPatch));
            ApplyPatches(typeof(MovementSystemPatches));
            ApplyPatches(typeof(CVRSeatPatch));
            ApplyPatches(typeof(CVRSpawnablePatches));

            RecalculateCoefficients();

            Con.Debug("Finished with patching.");
        }

        private static void WorldLoaded()
        {
            EarlyWorldJoin?.Invoke();
        }

        private static void UserJoinPatch(CVRPlayerEntity player)
        {
            try
            {
                UserJoin?.Invoke(player);                
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }
        
        private static void UserLeavePatch(CVRPlayerEntity player)
        {
            try
            {
                UserLeave?.Invoke(player);                
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }

        public static void RecalculateCoefficients()
        {
            var omega = 2 * Math.PI * MagicFilterLevel;
            var alpha = Math.Sin(omega) / (2 * QLevel);
            var cosw = Math.Cos(omega);

            _b[0] = (float)((1 - cosw) / 2);
            _b[1] = (float)(1 - cosw);
            _b[2] = _b[0];

            _a[0] = (float)(1 + alpha);
            _a[1] = (float)(-2 * cosw);
            _a[2] = (float)(1 - alpha);
            
            Normalize();
            
            Con.Debug($"CoefficientUpdate {MagicFilterLevel}:{QLevel}");
            
            _in1 = _in2 = _out1 = _out2 = 0;
        }
        
        public static void Normalize()
        {
            var a0 = _a[0];

            if (Math.Abs(a0 - 1) < 1e-10f)
            {
                return;
            }

            if (Math.Abs(a0) < 1e-30f)
            {
                throw new ArgumentException("The coefficient a[0] can not be zero!");
            }

            for (var i = 0; i < _a.Length; _a[i++] /= a0) { }
            for (var i = 0; i < _b.Length; _b[i++] /= a0) { }
        }

        public static float ProcessSample(float sample)
        {
            var output = _b[0] * sample + _b[1] * _in1 + _b[2] * _in2 - _a[1] * _out1 - _a[2] * _out2;
            
            _in2 = _in1;
            _in1 = sample;
            _out2 = _out1;
            _out1 = output;
            
            return output;
        }
    }
    
    [HarmonyPatch(typeof(AuthUIManager))]
    class AuthManagerPatches
    {
        [HarmonyPatch("AuthResultHandle")]
        [HarmonyPostfix]
        private static void AuthResultPatch(AuthUIManager __instance, int __0, string __1, string __2)
        {
            if (__0 != 1) return;
            
            Patches.OnUserLogin?.Invoke();
        }
    }

    [HarmonyPatch(typeof(CVRObjectLoader))]
    class CVRObjectLoaderPatch
    {
        private static readonly MethodInfo _worldLoadedMethod = typeof(Patches).GetMethod("WorldLoaded", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo _targetMethod = typeof(AssetBundle).GetMethod(nameof(AssetBundle.Unload), BindingFlags.Public | BindingFlags.Instance);

        [HarmonyPatch(nameof(CVRObjectLoader.LoadIntoWorld), MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new CodeMatcher(instructions)
                .MatchForward(true, new CodeMatch(OpCodes.Ldc_I4_0), new CodeMatch(OpCodes.Callvirt, _targetMethod))
                .Insert(
                    new CodeInstruction(OpCodes.Call, _worldLoadedMethod)
                )
                .InstructionEnumeration();

            return code;
        }
    }
    
    [HarmonyPatch]
    class CVRPlayerManagerJoin
    {
        private static readonly MethodInfo _targetMethod = typeof(List<CVRPlayerEntity>).GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo _userJoinMethod = typeof(Patches).GetMethod("UserJoinPatch", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo _playerEntity = typeof(CVRPlayerManager).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).Single(t => t.GetField("p") != null).GetField("p");
        
        static MethodInfo TargetMethod()
        {
            return typeof(CVRPlayerManager).GetMethod(nameof(CVRPlayerManager.TryCreatePlayer), BindingFlags.Instance | BindingFlags.Public);
        }
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new CodeMatcher(instructions)
                .MatchForward(true, new CodeMatch(OpCodes.Callvirt, _targetMethod))
                .Insert(
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, _playerEntity),
                    new CodeInstruction(OpCodes.Call, _userJoinMethod)
                )
                .InstructionEnumeration();

            return code;
        }
    }

    [HarmonyPatch]
    class CVRPlayerEntityLeave
    {
        private static readonly MethodInfo _userLeaveMethod = typeof(Patches).GetMethod("UserLeavePatch", BindingFlags.Static | BindingFlags.NonPublic);
        
        static MethodInfo TargetMethod()
        {
            return typeof(CVRPlayerEntity).GetMethod(nameof(CVRPlayerEntity.Recycle), BindingFlags.Instance | BindingFlags.Public);
        }
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new CodeMatcher(instructions)
                .Advance(1)
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, _userLeaveMethod)
                )
                .InstructionEnumeration();
            
            return code;
        }
    }

    [HarmonyPatch(typeof(PlayerNameplate))]
    class NameplatePatches
    {
        [HarmonyPatch(nameof(PlayerNameplate.UpdateNamePlate))]
        [HarmonyPostfix]
        static void UpdateNameplate(PlayerNameplate __instance)
        {
            try
            {
                Patches.OnNameplateRebuild?.Invoke(__instance);
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }
    }

    [HarmonyPatch(typeof(Instances))]
    class InstancesPatches
    {
        [HarmonyPatch(nameof(Instances.SetJoinTarget))]
        [HarmonyPostfix]
        static void SetJoinTarget(string __0, string __1)
        {
            Patches.TargetWorldID = __1;
            Patches.TargetInstanceID = __0;
            
            Patches.OnChangingInstance?.Invoke();
        }
    }

    [HarmonyPatch(typeof(RichPresence))]
    class RichPresensePatch
    {
        [HarmonyPatch(nameof(RichPresence.DisplayMode), MethodType.Setter)]
        [HarmonyPrefix]
        static bool OnRichPresenseUpdated()
        {
            var rpInfo = TWUtils.GetRichPresenceInfo();

            if (rpInfo == null) return true;
            
            if (Patches.LastRichPresenseUpdate == rpInfo.InstanceMeshId) return true;
            
            Patches.LastRichPresenseUpdate = rpInfo.InstanceMeshId;
            
            Con.Debug("Connected to instance");
            
            try
            {
                Patches.OnWorldJoin?.Invoke(rpInfo);
            }
            catch (Exception e)
            {
                Con.Error(e);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CVR_MenuManager))]
    class CVRMenuManagerPatch
    {
        [HarmonyPatch("markMenuAsReady")]
        [HarmonyPostfix]
        static void MarkMenuAsReady(CVR_MenuManager __instance)
        {
            try
            {
                Patches.OnMarkMenuAsReady?.Invoke(__instance);
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }
    }

    [HarmonyPatch(typeof(PuppetMaster))]
    class PuppetMasterPatch
    {
        [HarmonyPatch(nameof(PuppetMaster.AvatarInstantiated))]
        [HarmonyPostfix]
        static void OnAvatarInstantiated(PuppetMaster __instance)
        {
            Patches.OnAvatarInstantiated?.Invoke(TWUtils.GetPlayerDescriptorFromPuppetMaster(__instance).ownerId);
        }
    }

    [HarmonyPatch(typeof(ViewManager))]
    class ViewManagerPatches
    {
        [HarmonyPatch("SendToWorldUi")]
        [HarmonyPostfix]
        static void SendToWorldUi(string value)
        {
            //Ensure that we check if the keyboard action was used within 3 minutes, this will avoid the next keyboard usage triggering the action
            if(DateTime.Now.Subtract(Patches.TimeSinceKeyboardOpen).TotalMinutes <= 3)
                Patches.OnKeyboardSubmitted?.Invoke(value);

            Patches.OnKeyboardSubmitted = null;
        }

        [HarmonyPatch(nameof(ViewManager.FlagForUpdate))]
        [HarmonyPrefix]
        static void UpdateInvitesPatch(ViewManager __instance, ViewManager.UpdateTypes type)
        {
            if (type != ViewManager.UpdateTypes.Invites) return;

            if (TWNetClient.Instance.TargetInstanceID != null)
            {
                foreach (var invite in __instance.Invites)
                {
                    Con.Debug($"Invite Received: Invite InstanceID - {invite.InstanceMeshId} | TargetInstanceID - {TWNetClient.Instance.TargetInstanceID}");
                    if (!invite.InstanceMeshId.Equals(TWNetClient.Instance.TargetInstanceID)) continue;
                    
                    TWNetClient.Instance.AutoAcceptTargetInvite(invite.InviteMeshId);
                    break;
                }
            }

            if (Patches.TWInvites.Count == 0) return;
            
            Con.Debug($"UpdateInvitePatchFired {Patches.TWInvites.Count} in TWInvites");

            foreach (var invite in Patches.TWInvites)
            {
                if(__instance.Invites.Contains(invite))
                    continue;
                
                __instance.Invites.Add(invite);
            }
        }

        [HarmonyPatch(nameof(ViewManager.RespondToInvite))]
        [HarmonyPrefix]
        static bool RespondToInvite(ViewManager __instance, string inviteId, string response)
        {
            if (!inviteId.StartsWith("twInvite_"))
                return true;

            var invite = Patches.TWInvites.FirstOrDefault(x => x.InviteMeshId == inviteId);

            if (invite == null)
                return false;
            
            if (response == "deny" || response == "silence")
            {
                Patches.TWInvites.Remove(invite);
                ViewManager.Instance.ExpireInviteIn(inviteId, 2f);
                ViewManager.Instance.RemoveInvite(inviteId);
                ViewManager.Instance.FlagForUpdate(ViewManager.UpdateTypes.Invites);
                return false;
            }
            
            Con.Debug($"Invite accepted! {inviteId} | {invite.SenderUsername}, {invite.WorldName}, {invite.InstanceName}");
            Patches.OnInviteAccepted?.Invoke(invite);
            
            Patches.TWInvites.Remove(invite);
            ViewManager.Instance.ExpireInviteIn(inviteId, 2f);
            ViewManager.Instance.RemoveInvite(inviteId);
            ViewManager.Instance.FlagForUpdate(ViewManager.UpdateTypes.Invites);
            
            Con.Debug($"Removed invite from TWInvites {Patches.TWInvites.Count} left");

            return false;
        }
    }

    [HarmonyPatch(typeof(BasicMicrophoneCapture))]
    class MicrophoneCapturePatch
    {
        [HarmonyPatch("ConsumeSamples")]
        [HarmonyPrefix]
        static void ConsumeSamples(ArraySegment<float> samples)
        {
            if (samples.Array == null || !(Patches.IsForceMuted && Patches.IsMuffled))
                return;

            for (int i = 0; i < samples.Count; i++)
            {
                samples.Array[i] = Patches.ProcessSample(samples.Array[i]);
            }
        }
    }

    [HarmonyPatch(typeof(Audio))]
    class MicrophoneMutePatch
    {
        [HarmonyPatch(nameof(Audio.SetMicrophoneActive))]
        [HarmonyPrefix]
        static void SetMicrophoneActive(ref bool muted)
        {
            if (Patches.IsForceMuted && !Patches.IsMuffled)
            {
                muted = true;

                if (DateTime.Now.Subtract(Patches.TimeSinceLastUnmute).Seconds >= 20)
                {
                    Patches.TimeSinceLastUnmute = DateTime.Now;
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "You are gagged!", 3f, TWAssets.MicrophoneOff);
                }
            }
        }
    }

    [HarmonyPatch(typeof(CVRAdvancedAvatarSettings))]
    class AdvancedAvatarSettingsPatch
    {
        [HarmonyPatch(nameof(CVRAdvancedAvatarSettings.LoadAvatarProfiles))]
        [HarmonyPostfix]
        static void OnLoadAvatarProfiles()
        {
            try
            {
                Patches.OnLocalAvatarReady?.Invoke();
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }
    }
    
    [HarmonyPatch(typeof(PlayerSetup))]
    class PlayerSetupPatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void OnStart(PlayerSetup __instance)
        {
            __instance.avatarSetupCompleted.AddListener(() =>
            {
                //Expect local ready after LoadAvatarProfiles to avoid issue where params are reset
                if (__instance.GetLocalAvatarDescriptor().avatarUsesAdvancedSettings) return; 
                
                try
                {
                    Patches.OnLocalAvatarReady?.Invoke();
                }
                catch (Exception e)
                {
                    Con.Error(e);
                }
            });
        }
    }

    [HarmonyPatch(typeof(NetworkManager))]
    class NetworkManagerPatches
    {
        [HarmonyPatch("OnGameNetworkConnected")]
        [HarmonyPostfix]
        static void OnGameNetworkConnected(NetworkManager __instance)
        {
            Con.Debug("Connected to Game Network");
            try
            {
                Patches.OnGameNetworkConnected?.Invoke();
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }
        
        [HarmonyPatch("OnGameNetworkConnectionClosed")]
        [HarmonyPostfix]
        static void OnGameNetworkClosed(object __0, DisconnectedEventArgs __1)
        {
            Con.Debug("Disconnected from instance");
            try
            {
                Patches.OnWorldLeave?.Invoke();
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }
    }
    
    [HarmonyPatch(typeof(CVRSpawnable))]
    class CVRSpawnablePatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void PropStartPostfix(CVRSpawnable __instance)
        {
            try
            {
                Patches.OnPropSpawned?.Invoke(__instance);
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }

        [HarmonyPatch(nameof(CVRSpawnable.OnDestroy))]
        [HarmonyPostfix]
        static void PropDeletePostfix(CVRSpawnable __instance)
        {
            try
            {
                Patches.OnPropDelete?.Invoke(__instance);
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }
    }

    [HarmonyPatch(typeof(MovementSystem))]
    class MovementSystemPatches
    {
        [HarmonyPatch(nameof(MovementSystem.ChangeFlight))]
        [HarmonyPrefix]
        static bool ChangeFlightPrefix(ref bool b)
        {
            if (!b || !Patches.IsFlightLocked) return true;
            
            if (b && Patches.IsFlightLocked)
            {
                b = false;
                NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has disabled flight!", 3f, TWAssets.Handcuffs);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CVRSeat))]
    class CVRSeatPatch
    {
        [HarmonyPatch(nameof(CVRSeat.SitDown))]
        [HarmonyPrefix]
        static bool SitDownPrefix()
        {
            if (!Patches.AreSeatsLocked) return true;
            
            NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has disable the usage of seats!", 3f, TWAssets.Handcuffs);
            return false;
        }
    }
}
