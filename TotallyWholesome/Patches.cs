using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.Systems.Movement;
using ABI.CCK.Components;
using ABI.CCK.Scripts;
using HarmonyLib;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using WholesomeLoader;

namespace TotallyWholesome
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

        public static DateTime TimeSinceLastUnmute = DateTime.Now;
        public static DateTime TimeSinceKeyboardOpen = DateTime.Now;

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
            ApplyPatches(typeof(RichPresensePatch));
            ApplyPatches(typeof(InstancesPatches));
            ApplyPatches(typeof(PuppetMasterPatch));
            ApplyPatches(typeof(ViewManagerPatches));
            //ApplyPatches(typeof(MicrophoneCapturePatch));
            ApplyPatches(typeof(PlayerSetupPatches));
            ApplyPatches(typeof(AdvancedAvatarSettingsPatch));
            ApplyPatches(typeof(MovementSystemPatches));
            ApplyPatches(typeof(CVRSeatPatch));
            ApplyPatches(typeof(CVRSpawnablePatches));

            CVRGameEventSystem.Instance.OnConnected.AddListener((message) =>
            {
                try
                {
                    OnGameNetworkConnected?.Invoke();
                }
                catch (Exception e)
                {
                    Con.Error("An error occured within OnGameNetworkConnected!");
                    Con.Error(e);
                }
            });
            
            CVRGameEventSystem.Instance.OnDisconnected.AddListener((message) =>
            {
                try
                {
                    OnWorldLeave?.Invoke();
                }
                catch (Exception e)
                {
                    Con.Error("An error occured within OnWorldLeave!");
                    Con.Error(e);
                }
            });

            CVRGameEventSystem.Instance.OnConnectionLost.AddListener((message) =>
            {
                try
                {
                    OnWorldLeave?.Invoke();
                }
                catch (Exception e)
                {
                    Con.Error("An error occured within OnWorldLeave!");
                    Con.Error(e);
                }
            });
            
            CVRGameEventSystem.Instance.OnConnectionRecovered.AddListener((message) =>
            {
                try
                {
                    OnGameNetworkConnected?.Invoke();
                }
                catch (Exception e)
                {
                    Con.Error("An error occured within OnGameNetworkConnected!");
                    Con.Error(e);
                }
            });
            
            CVRGameEventSystem.World.OnLoad.AddListener((message) =>
            {
                try
                {
                    EarlyWorldJoin?.Invoke();
                }
                catch (Exception e)
                {
                    Con.Error("An error occured within EarlyWorldJoin!");
                    Con.Error(e);
                }
            });

            Con.Debug("Finished with patching.");
        }

        public static void LateStartupPatches()
        {
            ApplyPatches(typeof(MicrophoneMutePatch));
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

    //TODO: UNFUCKLE THIS SHIT AAAA
    /*
    [HarmonyPatch]
    public class MicrophoneCapturePatch
    {
        //These 2 numbers are still magic, changing it too far from these breaks the filter entirely
        public static float MagicFilterLevel = 433.1509f;
        public static float QLevel = 0.003219661f;
        
        private static float[] _a = new float[3];
        private static float[] _b = new float[3];

        private static float _in1, _in2, _out1, _out2;

        public static StringWriter Writer;
        
        private delegate void vx_after_capture_audio_read_t(IntPtr callback_handle, IntPtr session_group_handle, IntPtr initial_target_uri, IntPtr pcm_frames, int pcm_frame_count, int audio_frame_rate, int channels_per_frame);

        [MonoPInvokeCallback(typeof(vx_after_capture_audio_read_t))]
        private static unsafe void vx_after_capture_audio_read(IntPtr callback_handle, IntPtr session_group_handle, IntPtr initial_target_uri, IntPtr pcm_frames, int pcm_frame_count, int audio_frame_rate, int channels_per_frame)
        {
            try
            {
                if (session_group_handle == IntPtr.Zero || initial_target_uri == IntPtr.Zero || pcm_frames == IntPtr.Zero || !(Patches.IsForceMuted && Patches.IsMuffled)) return;

                int pcmDataCount = pcm_frame_count * channels_per_frame;
                
                if(pcmDataCount <= 0) return;

                short* pcmFrameData = (short*)pcm_frames;

                for (int i = 0; i < pcmDataCount; i++)
                {
                    var data = pcmFrameData[i];
                    var fixedFloat = (float)(data - short.MinValue) / (short.MaxValue - short.MinValue);
                    var output = _b[0] * fixedFloat+ _b[1] * _in1 + _b[2] * _in2 - _a[1] * _out1 - _a[2] * _out2;
            
                    _in2 = _in1;
                    _in1 = fixedFloat;
                    _out2 = _out1;
                    _out1 = output;
                    
                    pcmFrameData[i] = (short)(output * (short.MaxValue - short.MinValue) + short.MinValue);
                    
                   Writer.WriteLine("Data: Orig - " + fixedFloat + " Adj - " + output);
                }
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }

        static MethodBase TargetMethod()
        {
            return typeof(MetaPort).Assembly.GetType("ABI_RC.Core.Vivox.Internal.VivoxPCMReader").GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic);
        }
        static void Postfix(ref vx_sdk_config_t config)
        {
            config.pf_on_audio_unit_after_capture_audio_read = (SWIGTYPE_p_f_p_void_p_q_const__char_p_q_const__char_p_short_int_int_int__void)Activator.CreateInstance(typeof(SWIGTYPE_p_f_p_void_p_q_const__char_p_q_const__char_p_short_int_int_int__void), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { Marshal.GetFunctionPointerForDelegate<vx_after_capture_audio_read_t>(new vx_after_capture_audio_read_t(vx_after_capture_audio_read)), true }, null, null);
            Con.Debug("Created and set our after_capture_audio_read delegate");
            
            RecalculateCoefficients();
            Writer = new StringWriter();
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
    }
    */

    [HarmonyPatch]
    class MicrophoneMutePatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(MetaPort).Assembly.GetType("ABI_RC.Systems.Communications.VivoxServiceManager").GetProperty("InputDeviceMute", BindingFlags.Instance | BindingFlags.NonPublic)?.SetMethod;
        }
        
        static void Prefix(ref bool __0)
        {
            if (Patches.IsForceMuted)// && !Patches.IsMuffled)
            {
                __0 = true;

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

    [HarmonyPatch(typeof(BetterBetterCharacterController))]
    class MovementSystemPatches
    {
        [HarmonyPatch(nameof(BetterBetterCharacterController.ChangeFlight))]
        [HarmonyPrefix]
        static bool ChangeFlightPrefix(ref bool isFlying)
        {
            if (!isFlying || !Patches.IsFlightLocked) return true;

            isFlying = false;
            NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has disabled flight!", 3f, TWAssets.Handcuffs);

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
