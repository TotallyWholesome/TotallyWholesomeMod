using System;
using TotallyWholesome.Managers.AvatarParams;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.TWUI.Pages;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class MasterRestrictionCondition : Attribute, ICondition
    {
        private Restrictions[] _requiredRestrictions;
        
        public bool CheckCondition()
        {
            bool clear = true;

            if (LeadManager.Instance.PetPairs.Count == 0) return false;

            foreach (var restriction in _requiredRestrictions)
            {
                switch (restriction)
                {
                    case Restrictions.Gagged:
                        clear = LeadManager.Instance.ForcedMute || IndividualPetControl.Instance.SelectedLeadPair is { ForcedMute: true };
                        break;
                    case Restrictions.Blindfolded:
                        clear = LeadManager.Instance.Blindfold || IndividualPetControl.Instance.SelectedLeadPair is { Blindfold: true };
                        break;
                    case Restrictions.Deafened:
                        clear = LeadManager.Instance.Deafen || IndividualPetControl.Instance.SelectedLeadPair is { Deafen: true };
                        break;
                    case Restrictions.NoFlight:
                        clear = LeadManager.Instance.DisableFlight || IndividualPetControl.Instance.SelectedLeadPair is { DisableFlight: true };
                        break;
                    case Restrictions.NoSeats:
                        clear = LeadManager.Instance.DisableSeats || IndividualPetControl.Instance.SelectedLeadPair is { DisableSeats: true };
                        break;
                    case Restrictions.PinWorld:
                        clear = LeadManager.Instance.LockToWorld || IndividualPetControl.Instance.SelectedLeadPair is { LockToWorld: true };
                        break;
                    case Restrictions.PinProp:
                        clear = LeadManager.Instance.LockToProp || IndividualPetControl.Instance.SelectedLeadPair is { LockToProp: true };
                        break;
                    case Restrictions.TempUnlock:
                        clear = LeadManager.Instance.TempUnlockLeash || IndividualPetControl.Instance.SelectedLeadPair is { TempUnlockLeash: true };
                        break;
                    case Restrictions.ChangeRemoteParam:
                        clear = AvatarParameterManager.Instance.ChangedPetParam;
                        break;
                }

                if (!clear) break;
            }

            return clear;
        }

        public MasterRestrictionCondition(params Restrictions[] requiredRestrictions)
        {
            _requiredRestrictions = requiredRestrictions;
        }
    }
}