using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using ActionMenu;
using WholesomeLoader;
using TWNetCommon;
using TWNetCommon.Data;
using TotallyWholesome.Network;
using TotallyWholesome.Managers.Lead;
using System.Runtime.CompilerServices;
using TotallyWholesome.TWUI;

namespace TotallyWholesome.Managers.ModCompatibility
{
    public class ActionMenu : ITWManager
    {
        private bool hasAMInstalled;
        private ActionMenuIntegration _integration;

        public int Priority() => 1;
        public string ManagerName() => nameof(ActionMenu);
        
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void Setup()
        {

            if (TWUtils.GetMelonLoaderVersion() == "0.5.5")
            {
                Con.Warn("You are on MelonLoader version 0.5.5, ActionMenu integration will not be loaded!");
                return;
            }
            
            if (MelonMod.RegisteredMelons.Any(m => m.Info.Name.Equals("Action Menu")) )
            {
                hasAMInstalled = true;

                if (ConfigManager.Instance.IsActive(AccessType.UseActionMenu))
                    BuildActionMenu();

            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void BuildActionMenu()
        {
            _integration = new ActionMenuIntegration();
        }
        
        public void LateSetup()
        {

        }
    }

    internal class ActionMenuIntegration
    {
        object ActionMenuLib;

        public void LoadAMLib()
        {
            ActionMenuLib = new ActionMenuMod.Lib();
            API.OnGlobalMenuLoaded += API_OnGlobalMenuLoaded;
        }

        private void API_OnGlobalMenuLoaded(Menus menus)
        {
            if (!menus.ContainsKey("mods"))
                menus.Add("mods", new List<MenuItem>());

            Con.Msg("Registering ActionMenu");
            menus["mods"].Add(MainMenuWrapper());
        }

        private MenuItem MainMenuWrapper()
        {
            return DynamicMenuWrapper("Totally Wholesome", MainMenu, "TWIcon");
                
        }

        private List<MenuItem> MainMenu()
        {
            Con.Debug("Building ActionMenu");
            return new List<MenuItem>() {
                    MenuButtonWrapper("Remove Leash", () => TWNetClient.Instance.Send(new LeadAccept() { LeadRemove = true }, TWNetMessageTypes.LeadAccept), "Close"),
                    DynamicMenuWrapper("Pet Controls", () => PetControls(), "TWIcon"),
                    DynamicMenuWrapper("Individual Pets", IndividualPets, "delete all"),
                };
        }

        private List<MenuItem> IndividualPets()
        {
            var pets = new List<MenuItem>();
            foreach (var lead in LeadManager.Instance.ActiveLeadPairs.Values.Where(lead => lead.AreWeMaster()))
            {
                pets.Add(DynamicMenuWrapper(lead.Pet.CVRPlayer.Username, () => PetControls(lead), "TWIcon"));
            }
            return pets;
        }

        private List<MenuItem> PetControls(LeadPair pair = null)
        {
            if (pair == null)
            {
                return new List<MenuItem>(){
                    MenuRadialWrapper("Leash Distance", (f) => ApplyLeashRadialValue(f), "Link", LeadManager.Instance.TetherRange.SliderValue, 0, 10),
                    MenuRadialWrapper("Toy Strength", (f) => SetToyStrength(f), "Link", ButtplugManager.Instance.ToyStrength.SliderValue, 0, 100),
                    MenuToggleWrapper("Gag Pet", (f) => SetForcedMute(f), "Megaphone",  LeadManager.Instance.ForcedMute),
                    MenuToggleWrapper("Temp Unlock", (f) => SetTempUnlock(f), "Handcuffs", LeadManager.Instance.TempUnlockLeash),
                    DynamicMenuWrapper("PiShock", () => Pishock(), "delete all"),
                };
            }
            UserInterface.Instance.SelectedLeadPair = pair;
            return new List<MenuItem>(){
                    MenuRadialWrapper("Leash Distance", (f) => ApplyLeashRadialValue(f, pair), "Link", pair.LeadLength, 0, 10),
                    MenuRadialWrapper("Toy Strength", (f) => SetToyStrength(f, pair), "Link", pair.ToyStrength, 0, 100),
                    MenuToggleWrapper("Gag Pet", (f) => SetForcedMute(f, pair), "Megaphone",  pair.ForcedMute),
                    MenuToggleWrapper("Temp Unlock", (f) => SetTempUnlock(f, pair), "Handcuffs", pair.TempUnlockLeash),
                    DynamicMenuWrapper("PiShock", () => Pishock(pair), "delete all"),
                };
        }

        private List<MenuItem> Pishock(LeadPair pair = null)
        {
            if (pair == null)
            {
                return new List<MenuItem>(){
                    MenuRadialWrapper("Strength", (f) => PiShockManager.Instance.Strength.SliderValue = f, "Link", PiShockManager.Instance.Strength.SliderValue, 0, 100),
                    MenuRadialWrapper("Duration", (f) => PiShockManager.Instance.Duration.SliderValue = f, "Megaphone", PiShockManager.Instance.Duration.SliderValue, 0, 15),
                    MenuButtonWrapper("Send Beep", PiShockManager.BeepAction, "Handcuffs"),
                    MenuButtonWrapper("Send Vibration", PiShockManager.VibrateAction, "Handcuffs"),
                    MenuButtonWrapper("Send Shock", PiShockManager.ShockAction, "Handcuffs"),
                };
            }
            UserInterface.Instance.SelectedLeadPair = pair;
            return new List<MenuItem>(){
                    MenuRadialWrapper("Strentgh", (f) => pair.ShockStrength = (int)Math.Round(f), "Link", pair.ShockStrength, 0, 100),
                    MenuRadialWrapper("Duration", (f) => pair.ShockDuration = (int)Math.Round(f), "Megaphone", pair.ShockDuration, 0, 15),
                    MenuButtonWrapper("Send Beep", PiShockManager.BeepActionIPC, "Handcuffs"),
                    MenuButtonWrapper("Send Vibration", PiShockManager.VibrateActionIPC, "Handcuffs"),
                    MenuButtonWrapper("Send Shock", PiShockManager.ShockActionIPC, "Handcuffs"),
                };
        }

        public void ApplyLeashRadialValue(float value, LeadPair leadpair = null)
        {
            if (leadpair != null)
            {

                leadpair.LeadLength = value;
                TWNetSendHelpers.UpdateMasterSettingsAsync(leadpair);
            }
            else
            {
                LeadManager.Instance.TetherRange.SliderValue = value;
                TWNetSendHelpers.UpdateMasterSettingsAsync();
            }
        }

        public void SetToyStrength(float value, LeadPair pair = null)
        {
            if (pair == null)
            {
                ButtplugManager.Instance.ToyStrength.SliderValue = value;
                TWNetSendHelpers.SendButtplugUpdate();
            }
            else
            {
                pair.ToyStrength = value;
                TWNetSendHelpers.SendButtplugUpdate(pair);
            }
        }

        public void SetTempUnlock(bool state, LeadPair pair = null)
        {
            if (pair == null)
            {
                LeadManager.Instance.TempUnlockLeash = state;
                TWNetSendHelpers.UpdateMasterSettingsAsync();
            }
            else
            {
                pair.TempUnlockLeash = state;
                TWNetSendHelpers.UpdateMasterSettingsAsync(pair);
            }
        }

        public void SetForcedMute(bool state, LeadPair pair = null)
        {
            if (pair == null)
            {
                LeadManager.Instance.ForcedMute = state;
                TWNetSendHelpers.UpdateMasterSettingsAsync();
            }
            else
            {
                pair.ForcedMute = state;
                TWNetSendHelpers.UpdateMasterSettingsAsync(pair);
            }
        }

        public MenuItem MenuButtonWrapper(string name, Action action, string icon = null)
        {
            return new MenuItem() { name = name, action = ((ActionMenuMod.Lib)ActionMenuLib).BuildButtonItem(name.Replace(" ", ""), action), icon = icon };
        }
        public MenuItem MenuRadialWrapper(string name, Action<float> action, string icon = null, float defaultValue = 0, float minValue = 0, float maxValue = 1)
        {
            return new MenuItem() { name = name, action = ((ActionMenuMod.Lib)ActionMenuLib).BuildRadialItem(name.Replace(" ", ""), action, minValue, maxValue, defaultValue), icon = icon };
        }
        public MenuItem MenuToggleWrapper(string name, Action<bool> action, string icon = null, bool defaultValue = false)
        {
            return new MenuItem() { name = name, action = ((ActionMenuMod.Lib)ActionMenuLib).BuildToggleItem(name.Replace(" ", ""), action), icon = icon, enabled = defaultValue };
        }
        public MenuItem DynamicMenuWrapper(string name, Func<List<MenuItem>> action, string icon = null)
        {
            return new MenuItem() { name = name, action = ((ActionMenuMod.Lib)ActionMenuLib).BuildCallbackMenu(name.Replace(" ", ""), action), icon = icon };
        }
    }
}
