using System;
using TotallyWholesome.Managers.AvatarParams;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class PetRestrictionCondition : Attribute, ICondition
    {
        private Restrictions[] _requiredRestrictions;
        
        public bool CheckCondition()
        {
            bool clear = true;

            if (LeadManager.Instance.MasterPair == null) return false;

            foreach (var restriction in _requiredRestrictions)
            {
                switch (restriction)
                {
                    case Restrictions.Gagged:
                        clear = Patches.IsForceMuted;
                        break;
                    case Restrictions.Blindfolded:
                        clear = PlayerRestrictionManager.Instance.IsBlindfolded;
                        break;
                    case Restrictions.Deafened:
                        clear = PlayerRestrictionManager.Instance.IsDeafened;
                        break;
                    case Restrictions.NoFlight:
                        clear = Patches.IsFlightLocked;
                        break;
                    case Restrictions.NoSeats:
                        clear = Patches.AreSeatsLocked;
                        break;
                    case Restrictions.PinWorld:
                        clear = LeadManager.Instance.MasterPair.LockToWorld;
                        break;
                    case Restrictions.PinProp:
                        clear = LeadManager.Instance.MasterPair.LockToProp;
                        break;
                    case Restrictions.TempUnlock:
                        clear = LeadManager.Instance.MasterPair.TempUnlockLeash;
                        break;
                    case Restrictions.PinEither:
                        clear = LeadManager.Instance.MasterPair.LockToProp || LeadManager.Instance.MasterPair.LockToWorld;
                        break;
                    case Restrictions.ChangeRemoteParam:
                        clear = AvatarParameterManager.Instance.ChangedPetParam;
                        break;
                }



                if (!clear) break;
            }

            return clear;
        }

        public PetRestrictionCondition(params Restrictions[] requiredRestrictions)
        {
            _requiredRestrictions = requiredRestrictions;
        }
    }

    public enum Restrictions
    {
        Gagged,
        Blindfolded,
        Deafened,
        NoFlight,
        NoSeats,
        PinWorld,
        PinProp,
        PinEither,
        TempUnlock,
        ChangeRemoteParam
    }
}