using System.Collections.Generic;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using TotallyWholesome.Managers.Lead.LeadComponents;
using TotallyWholesome.Objects;
using TWNetCommon;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.NestedObjects;
using UnityEngine;

namespace TotallyWholesome.Managers.Lead;

/// <summary>
/// Lead pair exists on the masters side for each pet they have, it basically just holds data to network and more
/// </summary>
public class LeadPair
{
    public LineController LineController;
    public TWPlayerObject Master;
    public TWPlayerObject Pet;
    public string MasterID;
    public string PetID;
    public string Key;
    public HumanBodyBones PetBoneTarget;
    public HumanBodyBones MasterBoneTarget;
    public float LeadLength;
    public float ToyStrength;
    public bool ForcedMute;
    public bool NoVisibleLeash;
    public bool TempUnlockLeash;
    public string PetLeashColour = null;
    public string MasterLeashColour = null;
    public bool InitialPairCreationComplete;
    public bool GlobalValuesUpdate = false;
    public Vector3 LeashPinPosition = Vector3.zero;
    public string PropTarget = null;
    public LeashStyle LeashStyle = LeashStyle.Classic;
    public bool DisableFlight = false;
    public bool DisableSeats = false;
    public bool LockToProp = false;
    public bool LockToWorld = false;
    public bool Blindfold = false;
    public bool Deafen = false;
    public bool MasterDeafenBypass = false;
    public List<MasterRemoteParameter> PetEnabledParameters = new List<MasterRemoteParameter>();
    public bool UpdatedEnabledParams;

    public NetworkedFeature EnabledFeatures;
    public List<string> SwitchableAvatars;
    public bool UpdatedSwitchableAvatars;
    public string TargetAvatar;


    public ShockerState Shocker = new();

    public sealed class ShockerState
    {
        public ushort Duration { get; set; } = 1000;
        public byte Intensity { get; set; } = 25;
        public HeightControlState HeightControl { get; set; } = new();

        public sealed class HeightControlState
        {
            public bool Enabled { get; set; } = false;
            public float Height { get; set; }
            public float StrengthMin { get; set; }
            public float StrengthMax { get; set; }
            public float StrengthStep { get; set; }
        }
    }

    public LeadPair(TWPlayerObject master, TWPlayerObject pet, string key, HumanBodyBones petBoneTarget,
        HumanBodyBones masterBoneTarget, float leadLength, bool forcedMute, bool noVisibleLeash, bool tempUnlockLeash)
    {
        Master = master;
        Pet = pet;
        Key = key;
        PetBoneTarget = petBoneTarget;
        MasterBoneTarget = masterBoneTarget;
        LeadLength = leadLength;
        ForcedMute = forcedMute;
        NoVisibleLeash = noVisibleLeash;
        TempUnlockLeash = tempUnlockLeash;
    }

    public bool AreWeFollower()
    {
        if (Pet == null) return false;
        return Pet.Uuid == MetaPort.Instance.ownerId;
    }

    public bool AreWeMaster()
    {
        if (Master == null) return false;
        return Master.Uuid == MetaPort.Instance.ownerId;
    }

    /// <summary>
    /// Overcomplicated check to cover all cases that the LeadPair could be in
    /// </summary>
    /// <param name="player">Player instance to be checked</param>
    /// <returns></returns>
    public bool IsPlayerInvolved(TWPlayerObject player)
    {
        if (player == null)
            return false;

        if (Master != null && Equals(Master, player))
            return true;
        if (Pet != null && Equals(Pet, player))
            return true;
        if (MasterID != null && MasterID == player.Uuid)
            return true;
        return PetID != null && PetID == player.Uuid;
    }
}