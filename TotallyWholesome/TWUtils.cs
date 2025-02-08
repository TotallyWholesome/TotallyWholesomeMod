using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Networking.IO.UserGeneratedContent;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI.CCK.Components;
using Microsoft.IO;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Lead.LeadComponents;
using TotallyWholesome.Objects;
using TotallyWholesome.Utils;
using TWNetCommon.Data.NestedObjects;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome
{
    public static class TWUtils
    {
        private static MD5 _hasher = MD5.Create();
        private static FieldInfo _animatorGetter = typeof(PuppetMaster).GetField("_animator", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _getPlayerDescriptor = typeof(PuppetMaster).GetField("_playerDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _localAvatarDescriptor = typeof(PlayerSetup).GetField("_avatarDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _richPresenceLastMsgGetter = typeof(RichPresence).GetField("LastMsg", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo _vmSpawnablesGetter = typeof(ViewManager).GetField("_spawneables", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo _commsPipelineGetter = typeof(PuppetMaster).GetField("_pipeline", BindingFlags.NonPublic | BindingFlags.Instance);

        private static PropertyInfo _commsAudioSourceGetter = typeof(MetaPort).Assembly.GetType("ABI_RC.Systems.Communications.Audio.Components.Comms_AudioTap").GetProperty("_audioSource", BindingFlags.NonPublic | BindingFlags.Instance);
        private static PropertyInfo _selfUsername = typeof(MetaPort).Assembly.GetType("ABI_RC.Core.Networking.AuthManager").GetProperty("Username", BindingFlags.Static | BindingFlags.Public);
        private static PropertyInfo _currentInstancePrivacyGetter = typeof(MetaPort).GetProperty("CurrentInstancePrivacy");
        private static FieldInfo _currentInstancePrivacyField = typeof(MetaPort).GetField("CurrentInstancePrivacy");
        private static TWPlayerObject _ourPlayer;

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

        public static string GetCurrentInstancePrivacy()
        {
            if (_currentInstancePrivacyGetter != null)
                return (string)_currentInstancePrivacyGetter.GetValue(MetaPort.Instance);
            return (string)_currentInstancePrivacyField.GetValue(MetaPort.Instance);
        }

        public static AudioSource GetPlayerCommsAudioSource(this PuppetMaster pm)
        {
            var commsPipeline = _commsPipelineGetter.GetValue(pm);
            if (commsPipeline == null) return null;
            AudioSource commsAudioSource = _commsAudioSourceGetter.GetValue(commsPipeline) as AudioSource;
            return commsAudioSource;
        }

        public static void AddCVRNotification(string inviteID, string senderUsername, string inviteText)
        {
            var cvrInvite = new Invite_t();

            cvrInvite.InviteMeshId = $"twInvite_{inviteID}";
            cvrInvite.SenderUsername = senderUsername;
            cvrInvite.WorldName = inviteText;
            cvrInvite.InstanceName = inviteText;

            Patches.TWInvites.Add(cvrInvite);

            if (ViewManager.Instance == null || ViewManager.Instance.gameMenuView == null)
                return;

            ViewManager.Instance.FlagForUpdate(ViewManager.UpdateTypes.Invites);
        }

        public static void GetAvatarFromAPI(string avatarID, Action<AvatarDetailsResponse> onSuccess)
        {
            TwTask.Run(async () =>
            {
                var avatarDetails = await ApiConnection.MakeRequest<AvatarDetailsResponse>(ApiConnection.ApiOperation.AvatarDetail, new { avatarID });
                if (!avatarDetails.IsSuccessStatusCode) return;

                //Got a good avatar response
                Main.Instance.MainThreadQueue.Enqueue(() => onSuccess(avatarDetails.Data));
            });
        }

        public static RichPresenceInstance_t GetRichPresenceInfo()
        {
            return _richPresenceLastMsgGetter.GetValue(null) as RichPresenceInstance_t;
        }

        public static string GetSelfUsername()
        {
            return (string)_selfUsername.GetValue(null);
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

        public static CVRAvatar GetLocalAvatarDescriptor(this PlayerSetup ps)
        {
            return (CVRAvatar)_localAvatarDescriptor.GetValue(ps);
        }
        
        public static TWPlayerObject GetPlayerFromPlayerlist(string userID)
        {
            if (userID == null) return null;

            if (MetaPort.Instance.ownerId.Equals(userID))
                return GetOurPlayer();

            return (from player in CVRPlayerManager.Instance.NetworkPlayers where player.Uuid.Equals(userID) select new TWPlayerObject(player)).FirstOrDefault();
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

        public static int RandomFromUserID(string userID)
        {
            var hash1 = _hasher.ComputeHash(Encoding.UTF8.GetBytes(userID));
            var floatValue = (float)hash1[3].Combine(hash1[4])/65535;

            return (int)(floatValue * 100);
        }

        public static int Combine(this byte b1, byte concat)
        {
            int combined = b1 << 8 | concat;
            return combined;
        }

        public static Tuple<Material, LeashConfigAttribute> GetStyleMat(LeashStyle style)
        {
            LeashConfigAttribute attribute = LeashConfigAttribute.DefaultConfig;
            Material leashMaterial = null;

            if (Enum.IsDefined(typeof(LeashStyle), style))
            {
                var type = style.GetType();
                var memberInfo = type.GetMember(style.ToString());
                var attributes = memberInfo[0].GetCustomAttributes(typeof(LeashConfigAttribute), false);
                if (attributes.Length > 0)
                    attribute = (LeashConfigAttribute)attributes[0];
            }

            switch (style)
            {
                case LeashStyle.Classic:
                    leashMaterial = TWAssets.Classic;
                    break;
                case LeashStyle.Gradient:
                    leashMaterial = TWAssets.Gradient;
                    break;
                case LeashStyle.Magic:
                    leashMaterial = TWAssets.Magic;
                    break;
                case LeashStyle.Chain:
                    leashMaterial = TWAssets.Chain;
                    break;
                case LeashStyle.Leather:
                    leashMaterial = TWAssets.Leather;
                    break;
                case LeashStyle.Amogus:
                    leashMaterial = TWAssets.Amogus;
                    break;
                case LeashStyle.LGBT:
                    leashMaterial = TWAssets.LGBT;
                    break;
                case LeashStyle.Bisexual:
                    leashMaterial = TWAssets.Bisexual;
                    break;
                case LeashStyle.Polysexual:
                    leashMaterial = TWAssets.Polysexual;
                    break;
                case LeashStyle.Pansexual:
                    leashMaterial = TWAssets.Pansexual;
                    break;
                case LeashStyle.Lesbian:
                    leashMaterial = TWAssets.Lesbian;
                    break;
                case LeashStyle.Gay:
                    leashMaterial = TWAssets.Gay;
                    break;
                case LeashStyle.Asexual:
                    leashMaterial = TWAssets.Asexual;
                    break;
                case LeashStyle.Trans:
                    leashMaterial = TWAssets.Trans;
                    break;
                case LeashStyle.Nonbinary:
                    leashMaterial = TWAssets.Nonbinary;
                    break;
                case LeashStyle.Genderfluid:
                    leashMaterial = TWAssets.Genderfluid;
                    break;
                case LeashStyle.Christmas:
                    leashMaterial = TWAssets.Christmas;
                    break;
            }

            return new Tuple<Material, LeashConfigAttribute>(leashMaterial, attribute);
        }

        public static HumanBodyBones? GetBodyBoneFromLeadAttachIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return HumanBodyBones.Neck;
                case 1:
                    return HumanBodyBones.Spine;
                case 2:
                    return HumanBodyBones.Hips;
                case 3:
                    return HumanBodyBones.LeftFoot;
                case 4:
                    return HumanBodyBones.RightFoot;
                case 5:
                    return HumanBodyBones.LeftHand;
                case 6:
                    return HumanBodyBones.RightHand;
            }

            return null;
        }

        public static int GetLeadAttachIndexFromBodyBone(this HumanBodyBones bone)
        {
            switch (bone)
            {
                case HumanBodyBones.Neck:
                    return 0;
                case HumanBodyBones.Spine:
                    return 1;
                case HumanBodyBones.Hips:
                    return 2;
                case HumanBodyBones.LeftFoot:
                    return 3;
                case HumanBodyBones.RightFoot:
                    return 4;
                case HumanBodyBones.LeftHand:
                    return 5;
                case HumanBodyBones.RightHand:
                    return 6;
            }

            return 0;
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