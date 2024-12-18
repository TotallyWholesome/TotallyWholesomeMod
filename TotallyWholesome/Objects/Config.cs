using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Notification;
using TotallyWholesome.Objects.ConfigObjects;
using UnityEngine;

namespace TotallyWholesome.Objects
{
    public class Config
    {
        public string SelectedBranch = "live";
        public int AcceptedTOS = 0;
        public int ShownUpdateNotice = 0;
        public string IntifaceReleaseVersion = "";
        public bool ShownDiscordNotice = false;
        public bool ShownPiShockNotice = false;

        //General TW Settings
        public string LeashColour = "#FFFFFF";
        public LeashStyle LeashStyle = LeashStyle.Classic;
        public List<string> ColourPresets = new List<string>();
        public int LogoPositionX = 1460;
        public int LogoPositionY = 0;

        //Pet Specific Settings
        public HumanBodyBones PetBoneTarget = HumanBodyBones.Neck;
        public float BlindnessRadius = 1f;
        public float DeafenAttenuation = -35f;
        public string BlindnessVisionColourString = "#7F7F7F";
        public List<string> SwitchingAllowedAvatars = new();

        [JsonIgnore]
        private Color? _blindfoldVisionColorPriv = null;
        [JsonIgnore]
        public Color BlindnessVisionColour
        {
            set
            {
                BlindnessVisionColourString = "#" + ColorUtility.ToHtmlStringRGB(value);
                _blindfoldVisionColorPriv = value;
            }
            get
            {
                if (!_blindfoldVisionColorPriv.HasValue)
                {
                    _blindfoldVisionColorPriv = new Color(0.5f, 0.5f, 0.5f, 1);
                    if (ColorUtility.TryParseHtmlString(BlindnessVisionColourString, out var colour))
                        _blindfoldVisionColorPriv = colour;
                }

                return _blindfoldVisionColorPriv.Value;
            }
        }

        public List<PiShockShocker> PiShockShockers = new();
        
        public ShockerPlatform SelectedShockerPlatform { get; set; } = ShockerPlatform.None;

        //Master Specific Settings
        public HumanBodyBones MasterBoneTarget = HumanBodyBones.RightHand;
        
        //Status Config
        public bool EnableStatus = false;
        public bool DisplaySpecialStatus = false;
        public string LoginKey = "";
        public bool HideInPublicWorlds = true;
        public bool ShowDeviceStatus = false;
        public bool ShowAutoAccept = true;

        //Notification Settings
        public float NotificationAlpha = .7f;
        public NotificationAlignment NotificationAlignment = NotificationAlignment.CenterMiddle;
        public bool NotificationCustomPlacement = false;
        public float NotificationX = 0f;
        public float NotificationY = 0f;
        
        public enum ShockerPlatform
        {
            None = 0,
            OpenShock = 1,
            PiShock = 2
        }
    }
}
