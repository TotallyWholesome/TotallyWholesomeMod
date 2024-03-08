using System;
using System.Collections.Generic;
using TotallyWholesome.Managers.Lead;
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
        public List<string> SwitchingAllowedAvatars = new();

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
        
        public enum ShockerPlatform
        {
            None = 0,
            OpenShock = 1,
            PiShock = 2
        }
    }
}
