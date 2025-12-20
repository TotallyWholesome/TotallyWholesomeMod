using ABI_RC.Systems.UI.UILib;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Network;
using TWNetCommon;

namespace TotallyWholesome.Managers.TWUI.Pages;

public class PetRestrictionsPage : ITWManager
{
    public static PetRestrictionsPage Instance;
    
    public int Priority => 5;

    private Page _restrictionsPage;
    private ToggleButton _tempUnlock, _disallowFlight, _disallowSeats, _blindfold, _deafen, _lockToWorld, _lockToProp, _gagPets, _masterDeafenBypass;
    private bool _globalRestrictions;
    private LeadPair _selectedLeadPair;

    public void Setup()
    {
        Instance = this;
    }

    public void LateSetup()
    {
        //Setup UI page
        _restrictionsPage = new Page("TotallyWholesome", "More Pet Restrictions");
        QuickMenuAPI.AddRootPage(_restrictionsPage);
        _restrictionsPage.PageDisplayName = "Global Pet Restrictions";

        var basic = _restrictionsPage.AddCategory("Basic Controls", false);

        _gagPets = basic.AddToggle("Gag Pets", "Gag your pets", LeadManager.Instance.ForcedMute);
        _gagPets.OnValueUpdated += b =>
        {
            if (_globalRestrictions)
            {
                LeadManager.Instance.ForcedMute = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync();
            }
            else
            {
                _selectedLeadPair.ForcedMute = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(_selectedLeadPair);
            }
        };

        _tempUnlock = basic.AddToggle("Temp Leash Unlock", "Temporarily unlock leashs of pets", false);
        _tempUnlock.OnValueUpdated += b =>
        {
            if (_globalRestrictions)
            {
                LeadManager.Instance.TempUnlockLeash = b;
                TWNetSendHelpers.UpdateMasterSettingsAsync();
            }
            else
            {
                _selectedLeadPair.TempUnlockLeash = b;
                TWNetSendHelpers.UpdateMasterSettingsAsync(_selectedLeadPair);
            }
        };
        _disallowFlight = basic.AddToggle("Disallow Flight", "Disables flight for all of your pets", false);
        _disallowFlight.OnValueUpdated += b =>
        {
            if (_globalRestrictions)
            {
                LeadManager.Instance.DisableFlight = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync();
            }
            else
            {
                _selectedLeadPair.DisableFlight = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(_selectedLeadPair);
            }
        };
        _disallowSeats = basic.AddToggle("Disallow Seats", "Disables the use of seats for all your pets", false);
        _disallowSeats.OnValueUpdated += b =>
        {
            if (_globalRestrictions)
            {
                LeadManager.Instance.DisableSeats = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync();
            }
            else
            {
                _selectedLeadPair.DisableSeats = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(_selectedLeadPair);
            }
        };
        _blindfold = basic.AddToggle("Blindfold", "Blindfolds all your pets", false);
        _blindfold.OnValueUpdated += b =>
        {
            if (_globalRestrictions)
            {
                LeadManager.Instance.Blindfold = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync();
            }
            else
            {
                _selectedLeadPair.Blindfold = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(_selectedLeadPair);
            }
        };
        _deafen = basic.AddToggle("Deafen", "Deafens all your pets", false);
        _deafen.OnValueUpdated += b =>
        {
            if (_globalRestrictions)
            {
                LeadManager.Instance.Deafen = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync();
            }
            else
            {
                _selectedLeadPair.Deafen = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(_selectedLeadPair);
            }

            _masterDeafenBypass.Hidden = !_deafen.ToggleValue;
        };
        _masterDeafenBypass = basic.AddToggle("Master Deafen Bypass", "Allows the masters voice to be heard normally even when deafened", false);
        _masterDeafenBypass.OnValueUpdated += b =>
        {
            if (_globalRestrictions)
            {
                LeadManager.Instance.MasterDeafenBypass = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync();
            }
            else
            {
                _selectedLeadPair.MasterDeafenBypass = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(_selectedLeadPair);
            }
        };

        var worldStuff = _restrictionsPage.AddCategory("World/Prop Pinning");
        var setWorldPin = worldStuff.AddButton("Set World Pin", "Aim", "Allows you to choose a new position in the world to attach the leash to");
        setWorldPin.OnPress += () =>
        {
            if(_globalRestrictions)
                LeadManager.SelectWorldPosition();
            else
                LeadManager.SelectWorldPositionIPC();
        };

        _lockToWorld = worldStuff.AddToggle("Lock Leash to World Pin", "Locks all pets to the set world position", false);
        _lockToWorld.OnValueUpdated += b =>
        {
            if (_globalRestrictions)
            {
                LeadManager.Instance.LockToWorld = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync();
            }
            else
            {
                _selectedLeadPair.LockToWorld = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(_selectedLeadPair);
            }
        };

        var selectProp = worldStuff.AddButton("Select Bound Prop", "ListX3", "Allows you to choose a prop you want the leash attached to");
        selectProp.OnPress += () =>
        {
            if (_globalRestrictions)
                LeadManager.SelectBoundProp();
            else
                LeadManager.SelectBoundPropIPC();
        };

        _lockToProp = worldStuff.AddToggle("Lock Leash to Prop", "Locks all pets to the selected prop", false);
        _lockToProp.OnValueUpdated += b =>
        {
            if (_globalRestrictions)
            {
                LeadManager.Instance.LockToProp = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync();
            }
            else
            {
                _selectedLeadPair.LockToProp = b;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(_selectedLeadPair);
            }
        };
    }

    public void OpenRestrictionsPage(LeadPair selectedPair = null)
    {
        _globalRestrictions = selectedPair == null;
        _selectedLeadPair = selectedPair;
        _restrictionsPage.PageDisplayName = selectedPair != null ? $"{selectedPair.Pet.Username}'s Restrictions" : "Global Pet Restrictions";

        if (selectedPair == null)
        {
            UpdateButtonStates(NetworkedFeature.AllowBlindfolding | NetworkedFeature.AllowDeafening | NetworkedFeature.DisableFlight | NetworkedFeature.AllowPinning | NetworkedFeature.AllowForceMute);
            _lockToProp.ToggleValue = LeadManager.Instance.LockToProp;
            _lockToWorld.ToggleValue = LeadManager.Instance.LockToWorld;
            _gagPets.ToggleValue = LeadManager.Instance.ForcedMute;
            _tempUnlock.ToggleValue = LeadManager.Instance.TempUnlockLeash;
            _disallowFlight.ToggleValue = LeadManager.Instance.DisableFlight;
            _disallowSeats.ToggleValue = LeadManager.Instance.DisableSeats;
            _blindfold.ToggleValue = LeadManager.Instance.Blindfold;
            _deafen.ToggleValue = LeadManager.Instance.Deafen;
            _masterDeafenBypass.ToggleValue = LeadManager.Instance.MasterDeafenBypass;
        }
        else
        {
            UpdateButtonStates(selectedPair.EnabledFeatures);
            _lockToProp.ToggleValue = selectedPair.LockToProp;
            _lockToWorld.ToggleValue = selectedPair.LockToWorld;
            _gagPets.ToggleValue = selectedPair.ForcedMute;
            _tempUnlock.ToggleValue = selectedPair.TempUnlockLeash;
            _disallowFlight.ToggleValue = selectedPair.DisableFlight;
            _disallowSeats.ToggleValue = selectedPair.DisableSeats;
            _blindfold.ToggleValue = selectedPair.Blindfold;
            _deafen.ToggleValue = selectedPair.Deafen;
            _masterDeafenBypass.ToggleValue = selectedPair.MasterDeafenBypass;
        }

        _masterDeafenBypass.Hidden = !_deafen.ToggleValue;

        _restrictionsPage.OpenPage();
    }

    public void UpdateButtonStates(NetworkedFeature enabledFeatures)
    {
        _deafen.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowDeafening);
        _blindfold.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowBlindfolding);
        _disallowSeats.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.DisableFlight);
        _disallowFlight.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.DisableFlight);
        _lockToWorld.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowPinning);
        _lockToProp.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowPinning);
        _gagPets.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowForceMute);
    }
}