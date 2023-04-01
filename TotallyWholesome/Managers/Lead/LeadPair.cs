using System.Collections.Generic;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using TotallyWholesome.Managers.Lead.LeadComponents;
using TotallyWholesome.Objects;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.NestedObjects;
using UnityEngine;

namespace TotallyWholesome.Managers.Lead
{
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
        public int ShockDuration = 0;
        public int ShockStrength = 0;
        public ShockOperation ShockOperation = ShockOperation.NoOp;
        public bool ShockHeightEnabled = false;
        public float ShockHeight;
        public float ShockHeightStrengthMin;
        public float ShockHeightStrengthMax;
        public float ShockHeightStrengthStep;
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
        public List<MasterRemoteParameter> PetEnabledParameters = new List<MasterRemoteParameter>();
        public bool UpdatedEnabledParams;

        public LeadPair(TWPlayerObject master, TWPlayerObject pet, string key, HumanBodyBones petBoneTarget, HumanBodyBones masterBoneTarget, float leadLength, bool forcedMute, bool noVisibleLeash, bool tempUnlockLeash)
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
}