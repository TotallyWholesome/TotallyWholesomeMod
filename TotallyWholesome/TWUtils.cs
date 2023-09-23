using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.IO;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.IO.UserGeneratedContent;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI.CCK.Components;
using ABI.CCK.Scripts;
using cohtml;
using cohtml.Net;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Objects;
using TotallyWholesome.TWUI;
using TWNetCommon.Data.NestedObjects;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome
{
    public static class TWUtils
    {
        private static MD5 _hasher = MD5.Create();
        private static FieldInfo _animatorGetter = typeof(PuppetMaster).GetField("_animator", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _nameplateCanvasGetter = typeof(PlayerNameplate).GetField("_canvasGroup", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _getPlayerDescriptor = typeof(PuppetMaster).GetField("_playerDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _qmReady = typeof(CVR_MenuManager).GetField("_quickMenuReady", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _localAvatarDescriptor = typeof(PlayerSetup).GetField("_avatarDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _richPresenceLastMsgGetter = typeof(RichPresence).GetField("LastMsg", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo _mlVersionGetter = typeof(MelonLoader.BuildInfo).GetField(nameof(MelonLoader.BuildInfo.Version), BindingFlags.Public | BindingFlags.Static);
        private static FieldInfo _vmSpawnablesGetter = typeof(ViewManager).GetField("_spawneables", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo _selfUsername = typeof(MetaPort).Assembly.GetType("ABI_RC.Core.Networking.AuthManager").GetField("username", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo _internalCohtmlView = typeof(CohtmlControlledViewWrapper).GetField("_view", BindingFlags.Instance | BindingFlags.NonPublic);
        private static Dictionary<string, TWPlayerObject> _players = new();
        private static TWPlayerObject _ourPlayer;
        private static View _internalViewCache;

        public static View GetInternalView()
        {
            if (CVR_MenuManager.Instance == null || CVR_MenuManager.Instance.quickMenu == null) return null;

            if (_internalViewCache == null && _internalCohtmlView != null)
                _internalViewCache = (View)_internalCohtmlView.GetValue(CVR_MenuManager.Instance.quickMenu.View);

            return _internalViewCache;
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            return CreateMD5(inputBytes);
        }

        public static string CreateMD5(byte[] bytes)
        {
            byte[] hashBytes = _hasher.ComputeHash(bytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public static void LeaveWorld()
        {
            _players.Clear();
        }

        public static RichPresenceInstance_t GetRichPresenceInfo()
        {
            return _richPresenceLastMsgGetter.GetValue(null) as RichPresenceInstance_t;
        }

        public static string GetSelfUsername()
        {
            return (string)_selfUsername.GetValue(null);
        }

        public static string GetMelonLoaderVersion()
        {
            return (string)_mlVersionGetter.GetValue(null);
        } 

        public static void UserLeave(CVRPlayerEntity player)
        {
            if (_players.ContainsKey(player.Uuid))
                _players.Remove(player.Uuid);
        }

        public static bool IsQMReady()
        {
            return CVR_MenuManager.Instance != null && UserInterface.TWUIReady;
        }

        public static void OpenKeyboard(string currentValue, Action<string> callback)
        {
            Patches.OnKeyboardSubmitted = callback;
            Patches.TimeSinceKeyboardOpen = DateTime.Now;
            ViewManager.Instance.openMenuKeyboard(currentValue);
        }

        public static TWPlayerObject GetOurPlayer()
        {
            return _ourPlayer ??= new TWPlayerObject(null);
        }

        public static PlayerDescriptor GetPlayerDescriptorFromPuppetMaster(PuppetMaster pm)
        {
            return (PlayerDescriptor)_getPlayerDescriptor.GetValue(pm);
        }

        public static List<Spawnable_t> GetSpawnables()
        {
            if (ViewManager.Instance == null) return null;
            return (List<Spawnable_t>)_vmSpawnablesGetter.GetValue(ViewManager.Instance);
        }

        public static Animator GetAvatarAnimator(PuppetMaster pm)
        {
            if (pm == null) return null;
            return (Animator)_animatorGetter.GetValue(pm);
        }

        public static Canvas GetNameplateCanvas(PlayerNameplate nameplate)
        {
            return (Canvas)_nameplateCanvasGetter.GetValue(nameplate);
        }

        public static CVRAvatar GetLocalAvatarDescriptor(this PlayerSetup ps)
        {
            return (CVRAvatar)_localAvatarDescriptor.GetValue(ps);
        }
        
        public static TWPlayerObject GetPlayerFromPlayerlist(string userID)
        {
            if (MetaPort.Instance.ownerId.Equals(userID))
                return GetOurPlayer();

            if (_players.ContainsKey(userID))
                return _players[userID];
            
            foreach (var player in CVRPlayerManager.Instance.NetworkPlayers)
            {
                if (player.Uuid.Equals(userID)) return new TWPlayerObject(player);
            }

            return null;
        }

        public static Transform GetRootGameObject(GameObject root, string target)
        {
            if (root == null || root.transform == null)
                return null;

            for (int i = 0; i < root.transform.childCount; i++)
            {
                var transform = root.transform.GetChild(i);

                if (transform.name.Equals(target) && transform.gameObject.activeSelf)
                {
                    Con.Debug($"Found {target} on {root.transform.root.gameObject.name}!");
                    return transform;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the colours for Master and Pet in a leadpair, does a small check to avoid colours being very close to each other
        /// If colours are too close pet will be adjusted slightly
        /// </summary>
        /// <param name="masterID">Master VRC UserID</param>
        /// <param name="petID">Pet VRC UserID</param>
        /// <returns>Master and Pet colours with the adjustments applied</returns>
        public static (Color, Color) GetColourForLeadPair(string masterID, string petID)
        {
            var hash1 = _hasher.ComputeHash(Encoding.UTF8.GetBytes(masterID));
            var hueValue1 = (float)hash1[3].Combine(hash1[4])/65535;
            
            var hash2 = _hasher.ComputeHash(Encoding.UTF8.GetBytes(petID));
            var hueValue2 = (float)hash2[3].Combine(hash2[4])/65536;

            var diff = hueValue2 - hueValue1;

            if (diff <= .05 && diff >= -0.05)
            {
                hueValue2 += .1f;
                if (hueValue2 > 1)
                {
                    hueValue2 -= 1f;
                }
            }

            return (Color.HSVToRGB(hueValue1, .8f, .8f), Color.HSVToRGB(hueValue2, .8f, .8f));
        }

        public static int Combine(this byte b1, byte concat)
        {
            int combined = b1 << 8 | concat;
            return combined;
        }

        public static Tuple<Material, LineTextureMode> GetStyleMat(LeashStyle style)
        {
            switch (style)
            {
                case LeashStyle.Classic:
                    return new Tuple<Material, LineTextureMode>(TWAssets.Classic, LineTextureMode.RepeatPerSegment); 
                case LeashStyle.Gradient:
                    return new Tuple<Material, LineTextureMode>(TWAssets.Gradient, LineTextureMode.Stretch);
                case LeashStyle.Magic:
                    return new Tuple<Material, LineTextureMode>(TWAssets.Magic, LineTextureMode.Stretch); 
                case LeashStyle.Chain:
                    return new Tuple<Material, LineTextureMode>(TWAssets.Chain, LineTextureMode.RepeatPerSegment);
                case LeashStyle.Leather:
                    return new Tuple<Material, LineTextureMode>(TWAssets.Leather, LineTextureMode.RepeatPerSegment);
                case LeashStyle.Custom:
                    return new Tuple<Material, LineTextureMode>(null, LineTextureMode.RepeatPerSegment);
                default:
                    return new Tuple<Material, LineTextureMode>(TWAssets.Classic, LineTextureMode.RepeatPerSegment);
            }
        }

        #region Type Conversion

        public static Vector3 ToVector3(this TWVector3 vector3)
        {
            if (vector3 == null) return Vector3.zero;
            if (float.IsInfinity(vector3.X) || float.IsNaN(vector3.X) || float.IsInfinity(vector3.Y) || float.IsNaN(vector3.Y) || float.IsInfinity(vector3.Z) || float.IsNaN(vector3.Z)) return Vector3.zero;

            return new Vector3(vector3.X, vector3.Y, vector3.Z);
        }

        public static TWVector3 ToTWVector3(this Vector3 vector3)
        {
            if(float.IsInfinity(vector3.x) || float.IsNaN(vector3.x) || float.IsInfinity(vector3.y) || float.IsNaN(vector3.y) || float.IsInfinity(vector3.z) || float.IsNaN(vector3.z)) return TWVector3.Zero;

            return new TWVector3(vector3.x, vector3.y, vector3.z);
        }

        #endregion
    }
}