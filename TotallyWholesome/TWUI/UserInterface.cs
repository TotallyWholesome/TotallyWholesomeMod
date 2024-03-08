using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI.CCK.Scripts;
using cohtml;
using MelonLoader;
using MelonLoader.ICSharpCode.SharpZipLib.Zip;
using TotallyWholesome.Managers;
using TotallyWholesome.Managers.Achievements;
using TotallyWholesome.Managers.AvatarParams;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Status;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using TotallyWholesome.TWUI.UIObjects.Objects;
using TWNetCommon.Data.NestedObjects;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome.TWUI
{
    public class UserInterface
    {
        public static UserInterface Instance;
        public static List<SliderFloat> SliderFloats = new();
        internal static bool TWUIReady;
        
        public string SelectedPlayerID;
        public string SelectedPlayerUsername;
        public string SelectedPetID;
        public string SelectedPetName;
        public string LastParamControlUserID;
        public LeadPair ParamControlLeadPair;
        public LeadPair SelectedLeadPair;
        public MultiSelection SelectedMultiSelect;
        public Action<string, string> OnOpenedPage;
        public Action<string, string> OnBackAction;

        public delegate void UIEventHandlerFunc();

        private Dictionary<string, UIEventHandlerFunc> _handlers = new();
        private List<string> _generatedSettingsToggleIDs = new();
        private List<string> _generatedUserPermsToggleIDs = new();
        private Dictionary<string, AvatarParameter> _generatedParameterToggles = new();
        private Dictionary<string, MasterRemoteParameter> _generatedRemoteControlToggles = new();
        private Dictionary<string, MasterRemoteParameter> _generatedRemoteControlSingles = new();
        private List<SliderFloat> _generatedRemoteControlSliders = new();
        private Dictionary<string, MultiSelection> _multiSelectionOptions = new();
        private static MultiSelection _branchSelection;

        public string ManagerName() => nameof(UserInterface);
        public int Priority() => 2;

        public void Setup()
        {
            Instance = this;
            
            Patches.UserJoin += UserJoin;
            Patches.UserLeave += UserLeave;
            Patches.EarlyWorldJoin += WorldJoinLeave;
            Patches.OnWorldLeave += WorldJoinLeave;
            Patches.OnMarkMenuAsReady += MenuRegnerate;
            
            //Check for outdated UI
            Con.Msg("Checking if TWUI is updated...");
            CheckUpdateUI();

            //Load all attributes into the handlers dictionary
            foreach (TypeInfo type in Assembly.GetExecutingAssembly().DefinedTypes.Where(x => x.ImplementedInterfaces.Contains(typeof(ITWManager))))
            {
                foreach (var method in type.GetMethods())
                {
                    foreach (UIEventHandlerAttribute attr in method.GetCustomAttributes(typeof(UIEventHandlerAttribute), false))
                    {
                        var func = (UIEventHandlerFunc)Delegate.CreateDelegate(typeof(UIEventHandlerFunc), null, method);
                        foreach (var action in attr.UIActions)
                        {
                            Con.Debug($"Registering UI Action {action}");
                            _handlers[action] = func;
                        }
                    }
                }
            }

            if (WholesomeLoader.WholesomeLoader.AvailableVersions != null)
            {
                var branches = WholesomeLoader.WholesomeLoader.AvailableVersions.Select(x =>  x.Branch).ToArray();
                var prettyNames = WholesomeLoader.WholesomeLoader.AvailableVersions.Select(x =>  x.BranchPrettyName).ToArray();
                var index = Array.IndexOf(branches, Configuration.JSONConfig.SelectedBranch);
                if(index == -1)
                    Array.IndexOf(branches, "live");
                _branchSelection = new MultiSelection("Branch Selection", prettyNames, index);

                _branchSelection.OnOptionUpdated += i =>
                {
                    Configuration.JSONConfig.SelectedBranch = branches[i];
                    Configuration.SaveConfig();
                    
                    UIUtils.ShowNotice("Notice", "Selecting a new branch will require you to restart ChilloutVR before it takes effect!");
                };
            }
        }
        
        public void LateSetup(){}

        public void MenuRegnerate(CVR_MenuManager mm)
        {
            Con.Debug("Registering callbacks");
            
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-ButtonAction", new Action<string>(HandleButtonAction));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-Toggle", new Action<string, bool>(OnToggle));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-BoneSelected", new Action<string, string>(BoneSelected));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-OpenManageUser", new Action<string>(OpenManageUser));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-PopupConfirmOK", new Action(ConfirmOK));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-PopupConfirmNo", new Action(ConfirmNo));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-PopupNoticeOK", new Action(NoticeClose));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-OpenMainMenu", new Action(OpenMainMenu));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-SelectedPlayer", new Action<string, string>(OnSelectedPlayer));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-SliderValueUpdated", new Action<string, string>(OnSliderUpdated));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-AvatarRemoteConfigCleared", new Action(RepopulateAvatarRemoteConfigMenu));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-OpenedPage", new Action<string, string>(OnOpenedPageEvent));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-BackAction", new Action<string, string>(OnBackActionEvent));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-SelectPet", new Action<string, string>(OnPetSelected));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-OpenParamControl", new Action<string>(OpenParamControl));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-RemoteParamControlCleared", new Action(RepopulateParamControl));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-DropdownSelected", new Action<int>(DropdownSelected));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-ShockerAction", new Action<string, string>(PiShockManager.OnShockerAction));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-NumSubmit", new Action<string>(OnNumberInputSubmitted));
            CVR_MenuManager.Instance.quickMenu.View.BindCall("twUI-UILoaded", new Action(OnTWUILoaded));
        }

        private void OnTWUILoaded()
        {
            Con.Debug("TWUI has fully loaded, setting up!");
            

            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twVersionUpdate", $"{BuildInfo.AssemblyVersion} {(BuildInfo.isBetaBuild ? "Beta Build" : "Release Build")}");

            
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twUserCountUpdate", TWNetClient.Instance.OnlineUsers.ToString());
            
            StatusManager.Instance.UpdateQuickMenuStatus();
            
 
            
            UIUtils.SetToggleState("GagPet", LeadManager.Instance.ForcedMute);
            UIUtils.SetToggleState("TempLeashUnlock", LeadManager.Instance.TempUnlockLeash);
            UIUtils.SetToggleState("HeightControl", PiShockManager.Instance.ShockHeightEnabled);
            UIUtils.SetToggleState("EnableStatus", Configuration.JSONConfig.EnableStatus);
            UIUtils.SetToggleState("DisplaySpecialBadge", Configuration.JSONConfig.DisplaySpecialStatus);
            UIUtils.SetToggleState("HideInPublics", Configuration.JSONConfig.HideInPublicWorlds);
            UIUtils.SetToggleState("ShowAutoAccept", Configuration.JSONConfig.ShowAutoAccept);
            UIUtils.SetToggleState("ShowDeviceStatus", Configuration.JSONConfig.ShowDeviceStatus);
            
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twUpdateSelectedBone", "master", Configuration.JSONConfig.MasterBoneTarget.ToString());
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twUpdateSelectedBone", "pet", Configuration.JSONConfig.PetBoneTarget.ToString());
            

            //Update slider states to match values
            Con.Debug("Update slider states");
            foreach (var slider in SliderFloats) 
                slider.UpdateSlider();

            UpdateAvatarRemoteConfig();

            UIUtils.SendModInit();
            LeadManager.Instance.OnColorChanged();

            if (Patches.TWInvites.Count == 0 || ViewManager.Instance == null || ViewManager.Instance.gameMenuView == null)
                return;
            
            ViewManager.Instance.FlagForUpdate(ViewManager.UpdateTypes.Invites);
        }

        private void OnNumberInputSubmitted(string input)
        {
            if (!float.TryParse(input, out var inputFloat))
            {
                UIUtils.ShowNotice("Invalid Input!", "You entered a value that is not a valid input!");
                return;
            }

            if (inputFloat > 9999 || inputFloat < -9999)
            {
                UIUtils.ShowNotice("Invalid Input!", "You entered a value that's outside the limits of 9999 and -9999! You must keep your value within those limits.");
                return;
            }
            
            UIUtils.NumberInputComplete?.Invoke(inputFloat);
            UIUtils.NumberInputComplete = null;
        }

        private void DropdownSelected(int index)
        {
            if (SelectedMultiSelect != null)
                SelectedMultiSelect.SelectedOption = index;
        }

        private void OpenParamControl(string userID)
        {
            var user = TWUtils.GetPlayerFromPlayerlist(userID);

            if (user == null)
                return;

            var leadPair = LeadManager.Instance.GetLeadPairForPet(user);

            if (leadPair == null)
                return;
            
            if (LastParamControlUserID != null && LastParamControlUserID.Equals(userID) && !leadPair.UpdatedEnabledParams)
            {
                //Menu is already configured
                CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twOpenParamControl");
                return;
            }

            if (leadPair.PetEnabledParameters.Count == 0)
            {
                UIUtils.ShowNotice("No Remote Params!", "This pet does not have any remote parameters enabled!");
                return;
            }

            LastParamControlUserID = userID;
            ParamControlLeadPair = leadPair;
            
            //We gotta rebuild the param control menu, lets wait till it's cleared!
            foreach (var slider in _generatedRemoteControlSliders)
                SliderFloats.Remove(slider);
            _generatedRemoteControlSliders.Clear();
            _generatedRemoteControlToggles.Clear();
            _multiSelectionOptions.Clear();
            _generatedRemoteControlSingles.Clear();
            
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twClearParamControl");
        }

        private void RepopulateParamControl()
        {
            //Lets do the thing!
            foreach (var param in ParamControlLeadPair.PetEnabledParameters)
            {
                switch ((CVRAdvancedSettingsEntry.SettingsType)param.ParameterType)
                {
                    case CVRAdvancedSettingsEntry.SettingsType.GameObjectToggle:
                        var toggleID = UIUtils.CreateToggle("RemoteControl", "AvatarButtons", param.ParameterTarget, param.ParameterTarget, $"Toggles the {param.ParameterTarget} parameter on this pets avatar",
                            param.ParameterValue > 0.5f);
                        _generatedRemoteControlToggles.Add(toggleID, param);
                        break;
                    case CVRAdvancedSettingsEntry.SettingsType.Slider:
                        var slider = UIUtils.CreateSlider("twUI-RemoteControl-SliderRoot", param.ParameterTarget, param.ParameterTarget, param.ParameterValue, 0f, 1f,
                            $"Control the {param.ParameterTarget} slider on your pet");
                        slider.OnValueUpdated += f =>
                        {
                            param.ParameterValue = f;
                            param.IsUpdated = true;
                            AvatarParameterManager.Instance.SendUpdatedParameters(ParamControlLeadPair);
                        };
                        _generatedRemoteControlSliders.Add(slider);
                        break;
                    case CVRAdvancedSettingsEntry.SettingsType.GameObjectDropdown:
                        var multiselect = new MultiSelection(param.ParameterTarget, param.ParameterOptions, (int)Math.Round(param.ParameterValue));
                        multiselect.OnOptionUpdated += i =>
                        {
                            param.ParameterValue = i;
                            param.IsUpdated = true;
                            AvatarParameterManager.Instance.SendUpdatedParameters(ParamControlLeadPair);
                        };
                        var action = $"twUI-Open{param.ParameterTarget}";
                        UIUtils.CreateButton("twUI-RemoteControl-MultiSelection", param.ParameterTarget, "list", $"Opens the multi selection menu for {param.ParameterTarget}", action);
                        _multiSelectionOptions.Add(action, multiselect);
                        break;
                    case CVRAdvancedSettingsEntry.SettingsType.InputSingle:
                        var actionInput = $"twUI-OpenInput{param.ParameterTarget}";
                        UIUtils.CreateButton("twUI-RemoteControl-SingleInput", param.ParameterTarget, "Settings", $"Opens the {param.ParameterTarget} single input page", actionInput);
                        _generatedRemoteControlSingles.Add(actionInput, param);
                        break;
                }
            }
            
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twOpenParamControl");
        }

        private void OnBackActionEvent(string targetPage, string lastPage)
        {
            if (targetPage.Equals("IndividualPetControl") && SelectedLeadPair != null && SelectedLeadPair.GlobalValuesUpdate)
            {
                UpdateIPCPage();
                SelectedLeadPair.GlobalValuesUpdate = false;
            }
            
            OnBackAction?.Invoke(targetPage, lastPage);
        }

        private void OnOpenedPageEvent(string targetPage, string lastPage)
        {
            if (targetPage.Equals("IndividualPetControl") && SelectedLeadPair != null && SelectedLeadPair.GlobalValuesUpdate)
            {
                UpdateIPCPage();
                SelectedLeadPair.GlobalValuesUpdate = false;
            }

            if (targetPage.Equals("MyAchievements") && AchievementManager.Instance.AchievementsUpdated)
            {
                AchievementManager.Instance.AchievementsUpdated = false;
                UpdateAchievementPage();
            }
            
            OnOpenedPage?.Invoke(targetPage, lastPage);
        }

        private void UpdateAchievementPage()
        {
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twClearAchievementList");

            foreach (var achievement in AchievementManager.Instance.LoadedAchievements)
            {
                CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twCreateAchievementButton", achievement.AchievementAwarded ? achievement.AchievementName : "???", achievement.AchievementAwarded ? achievement.AchievementDescription : "You must unlock this achievement before you can see the description!", Enum.GetName(typeof(AchievementRank), achievement.AchievementRank), !achievement.AchievementAwarded);
            }
        }

        public void UpdateAvatarRemoteConfig()
        {
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twClearAvatarRemoteConfig");
        }

        private void RepopulateAvatarRemoteConfigMenu()
        {
            var count = 0;
            _generatedParameterToggles.Clear();
            
            //Let's build the remote config menu
            foreach (var param in AvatarParameterManager.Instance.TWAvatarParameters)
            {
                var generatedID = $"FloatParams-AvatarRemote-{param.Name}";
                if(_generatedParameterToggles.ContainsKey(generatedID))
                {
                    Con.Warn($"Found duplicated param toggle! {param.Name} - {param.ParamType}");
                    continue;
                }

                CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twCreateToggle", "FloatParams", "AvatarRemote", param.Name, param.Name, $"Enables {param.Name} to be controlled by your master", param.RemoteEnabled);

                if (param.RemoteEnabled)
                    count++;
                
                _generatedParameterToggles.Add(generatedID, param);
            }
            
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twAvatarRemoteUpdateHeader", count);
        }

        private void OnSliderUpdated(string sliderID, string value)
        {
            if (!float.TryParse(value, out var valueFloat))
                return;

            var sliders = SliderFloats.Where(x => x.SliderID.Equals(sliderID));

            foreach (var slider in sliders)
                slider.SliderValue = valueFloat;
        }

        private void OnSelectedPlayer(string username, string userID)
        {
            SelectedPlayerUsername = username;
            SelectedPlayerID = userID;
        }
        
        private void OnPetSelected(string petName, string petID)
        {
            SelectedPetName = petName;
            SelectedPetID = petID;

            var petPlayer = TWUtils.GetPlayerFromPlayerlist(petID);

            if (petPlayer == null)
            {
                Con.Error("Attempted to select a pet that doesn't exist!");
                CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twRemovePet", petID);
                SelectedLeadPair = null;
                SelectedPetID = "";
                SelectedPetName = "";
                return;
            }

            var leadPair = LeadManager.Instance.GetLeadPairForPet(petPlayer);

            if (leadPair == null)
            {
                Con.Error("Selected LeadPair doesn't exist!");
                CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twRemovePet", petID);
                SelectedLeadPair = null;
                SelectedPetID = "";
                SelectedPetName = "";
                return;
            }

            SelectedLeadPair = leadPair;
            leadPair.GlobalValuesUpdate = false;
            UpdateIPCPage();
        }

        private void UpdateIPCPage()
        {
            //Update IndividualPetControls page
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twSliderSetValue", "leashLengthSliderIPC", SelectedLeadPair.LeadLength);
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twSliderSetValue", "lovenseStrengthSliderIPC", SelectedLeadPair.ToyStrength);
            //PiShock
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twSliderSetValue", "piShockStrengthSliderIPC", SelectedLeadPair.ShockStrength);
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twSliderSetValue", "piShockDurationSliderIPC", SelectedLeadPair.ShockDuration);
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twSliderSetValue", "piShockHeightSliderIPC", SelectedLeadPair.ShockHeight);
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twSliderSetValue", "piShockMaxStrengthSliderIPC", SelectedLeadPair.ShockHeightStrengthMax);
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twSliderSetValue", "piShockMinStrengthSliderIPC", SelectedLeadPair.ShockHeightStrengthMin);
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twSliderSetValue", "piShockStepStrengthSliderIPC", SelectedLeadPair.ShockHeightStrengthStep);

            UIUtils.SetToggleState("GagPetIPC", SelectedLeadPair.ForcedMute);
            UIUtils.SetToggleState("TempLeashUnlockIPC", SelectedLeadPair.TempUnlockLeash);
            UIUtils.SetToggleState("HeightControlIPC", SelectedLeadPair.ShockHeightEnabled);
            UIUtils.SetToggleState("LockToWorldIPC", SelectedLeadPair.LockToWorld);
            UIUtils.SetToggleState("LockToPropIPC", SelectedLeadPair.LockToProp);
            UIUtils.SetToggleState("DisableSeatsIPC", SelectedLeadPair.DisableSeats);
            UIUtils.SetToggleState("DisableFlightIPC", SelectedLeadPair.DisableFlight);
        }

        private void OpenMainMenu()
        {
            if (Configuration.JSONConfig.AcceptedTOS < Main.CurrentTOSLevel)
            {
                MelonCoroutines.Start(WaitBeforeOpeningEULAPrompt());
                return;
            }

            if (Configuration.JSONConfig.ShownUpdateNotice != 8)
            {
                MelonCoroutines.Start(WaitBeforeShowUpdateNotice());
                return;
            }

            if (!Configuration.JSONConfig.ShownDiscordNotice)
            {
                MelonCoroutines.Start(WaitBeforeOpeningDiscordNotice());
                return;
            }

            if (!Configuration.JSONConfig.ShownPiShockNotice)
            {
                MelonCoroutines.Start(WaitBeforeShowingPiShockNotice());
            }
        }
        
        private static IEnumerator WaitBeforeOpeningEULAPrompt()
        {
            yield return new WaitForSeconds(.2f);

            UIUtils.ShowConfirm("Totally Wholesome EULA", "<p>Before you can connect to TWNet you must agree to our End User License Agreement!</p><p>You can read the EULA at<br/>https://wiki.totallywholeso.me/eula</p>", "Agree", OnYes, "Decline", OnCancel);
        }

        private static IEnumerator WaitBeforeOpeningDiscordNotice()
        {
            yield return new WaitForSeconds(.2f);

            UIUtils.ShowNotice("Join Our Discord!", "<p>Thank you for using Totally Wholesome!</p><p>You can join our Discord to keep up to date on changes and such around Totally Wholesome!</p><p>You can find the invite in Status and ETC!</p>", "OK",
                () =>
                {
                    Configuration.JSONConfig.ShownDiscordNotice = true;
                    Configuration.SaveConfig();
                });
        }

        private static IEnumerator WaitBeforeShowUpdateNotice()
        {
            yield return new WaitForSeconds(.2f);

            UIUtils.ShowNotice("Welcome to TW 3.4!", "<p>Welcome to Totally Wholesome v3.4! Happy April Fools!</p><p>This update brings about many a new changes to Totally Wholesome! One big one is our (totally not jank) Achievement system! Have you ever wanted to earn achievements for doing random stuff in a lewd mod? No? Too bad! Now you can!</p><p>Changes:</p><p> - Achievements</p><p> - Status Rework</p><p> - Custom Leash Material</p><p> - Blindfolding/Deafening</p><p> - And more!</p><p>Check the TW Discord for more information!</p>", "OK",
                () =>
                {
                    Configuration.JSONConfig.ShownUpdateNotice = 8;
                    Configuration.SaveConfig();
                });
        }
        
        private static IEnumerator WaitBeforeShowingPiShockNotice()
        {
            yield return new WaitForSeconds(.2f);

            UIUtils.ShowConfirm("PiShock Joins the Fun!", "<p>PiShock internet connected shockers are now supported by Totally Wholesome!</p><p>This brings a whole new way to interact using Totally Wholesome!</p><p>To learn more please click \"Learn More\" below!</p>", "Learn More", () =>
                {
                    Process.Start("https://pishock.com/#/?campaign=tw");
                    Configuration.JSONConfig.ShownPiShockNotice = true;
                    Configuration.SaveConfig();
                },
                "Ok",
                () =>
                {
                    Configuration.JSONConfig.ShownPiShockNotice = true;
                    Configuration.SaveConfig();
                });
        }

        private static void OnCancel()
        {
            Con.Msg("You have declined the Totally Wholesome EULA, you will not be connected to TWNet.");
        }

        private static void OnYes()
        {
            Configuration.JSONConfig.AcceptedTOS = Main.CurrentTOSLevel;
            Configuration.SaveConfig();

            TWNetClient.Instance.ConnectClient();
        }

        private void NoticeClose()
        {
            UIUtils.NoticeOk?.Invoke();
        }

        private void ConfirmNo()
        {
            UIUtils.ConfirmNo?.Invoke();
        }

        private void ConfirmOK()
        {
            UIUtils.ConfirmYes?.Invoke();
        }

        private void OpenManageUser(string userID)
        {
            SelectedPlayerID = userID;
            
            foreach (var item in Enum.GetValues(typeof(AccessType)))
            {
                var accessType = (AccessType)item;
                var attribute = accessType.GetAttributeOfType<AccessAttribute>();

                if (attribute.User)
                {
                    UIUtils.SetToggleState(accessType.ToString(), ConfigManager.Instance.IsActiveUserOnly(accessType, userID), attribute.Category, "UserPerms");
                }
            }
            
            Con.Debug("Updated UserManagePage toggle states, opening menu.");
            
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twOpenUserManage");
        }

        private void BoneSelected(string bonePage, string bone)
        {
            if (!Enum.TryParse(bone, false, out HumanBodyBones hbBone))
                return;
            
            switch (bonePage)
            {
                case "master":
                    Configuration.JSONConfig.MasterBoneTarget = hbBone;
                    Configuration.SaveConfig();
                    break;
                case "pet":
                    Configuration.JSONConfig.PetBoneTarget = hbBone;
                    Configuration.SaveConfig();
                    break;
            }
            
            Con.Debug($"Updated selected bone: {bonePage}:{bone}");
        }

        private void OnToggle(string toggleID, bool state)
        {
            Con.Debug($"Toggle state changed for {toggleID} to {state}");

            if (_generatedSettingsToggleIDs.Contains(toggleID))
            {
                var idParts = toggleID.Split('-');
                if (idParts.Length == 3 && Enum.TryParse(idParts[2], false, out AccessType result))
                {
                    Con.Debug($"Found AccessType {result}");
                    ConfigManager.Instance.SetActive(result, state, toggleID);
                }

                return;
            }

            if (_generatedUserPermsToggleIDs.Contains(toggleID) && !string.IsNullOrWhiteSpace(SelectedPlayerID))
            {
                var idParts = toggleID.Split('-');
                if (idParts.Length == 3 && Enum.TryParse(idParts[2], false, out AccessType result))
                {
                    Con.Debug($"Found AccessType {result}");
                    ConfigManager.Instance.SetActive(result, state, toggleID, SelectedPlayerID);
                }
                
                return;
            }

            if (_generatedParameterToggles.ContainsKey(toggleID))
            {
                var param = _generatedParameterToggles[toggleID];
                
                AvatarParameterManager.Instance.SetParameterRemoteState(param.Name, state);
                return;
            }

            if (_generatedRemoteControlToggles.ContainsKey(toggleID))
            {
                var param = _generatedRemoteControlToggles[toggleID];
                param.ParameterValue = state ? 1f : 0f;
                param.IsUpdated = true;
                AvatarParameterManager.Instance.SendUpdatedParameters(ParamControlLeadPair);
                return;
            }

            if (Configuration.JSONConfig.PiShockShockers.Any(x => x.Key.Equals(toggleID)))
            {
                PiShockManager.Instance.ChangeShockerState(toggleID, state);
                return;
            }

            switch (toggleID)
            {
                case "EnableStatus":
                    Configuration.JSONConfig.EnableStatus = state;
                    Configuration.SaveConfig();
                    
                    StatusManager.Instance.SendStatusUpdate();
                    break;
                case "DisplaySpecialBadge":
                    Configuration.JSONConfig.DisplaySpecialStatus = state;
                    Configuration.SaveConfig();
                    
                    StatusManager.Instance.SendStatusUpdate();
                    break;
                case "HideInPublics":
                    Configuration.JSONConfig.HideInPublicWorlds = state;
                    Configuration.SaveConfig();
                    
                    StatusManager.Instance.SendStatusUpdate();
                    break;
                case "ShowAutoAccept":
                    Configuration.JSONConfig.ShowAutoAccept = state;
                    Configuration.SaveConfig();
                    
                    StatusManager.Instance.SendStatusUpdate();
                    break;
                case "ShowDeviceStatus":
                    Configuration.JSONConfig.ShowDeviceStatus = state;
                    Configuration.SaveConfig();
                    
                    StatusManager.Instance.SendStatusUpdate();
                    break;
                case "HeightControl":
                    PiShockManager.UpdateShockHeightControl(state);
                    break;
                case "GagPet":
                    LeadManager.Instance.ForcedMute = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync();
                    break;
                case "TempLeashUnlock":
                    LeadManager.Instance.TempUnlockLeash = state;
                    TWNetSendHelpers.UpdateMasterSettingsAsync();
                    break;
                case "TempLeashUnlockIPC":
                    if (SelectedLeadPair == null)
                        break;
                    SelectedLeadPair.TempUnlockLeash = state;
                    TWNetSendHelpers.UpdateMasterSettingsAsync(SelectedLeadPair);
                    break;
                case "GagPetIPC":
                    if (SelectedLeadPair == null)
                        break;
                    SelectedLeadPair.ForcedMute = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync(SelectedLeadPair);
                    break;
                case "HeightControlIPC":
                    if (SelectedLeadPair == null)
                        break;
                    PiShockManager.SetShockHeightControlIPC(enable: state);
                    break;
                case "LockToProp":
                    LeadManager.Instance.LockToProp = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync();
                    break;
                case "LockToWorld":
                    LeadManager.Instance.LockToWorld = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync();
                    break;
                case "DisableFlight":
                    LeadManager.Instance.DisableFlight = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync();
                    break;
                case "DisableSeats":
                    LeadManager.Instance.DisableSeats = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync();
                    break;
                case "Blindfold":
                    LeadManager.Instance.Blindfold = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync();
                    break;
                case "Deafen":
                    LeadManager.Instance.Deafen = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync();
                    break;
                case "LockToPropIPC":
                    if (SelectedLeadPair == null)
                        break;
                    SelectedLeadPair.LockToProp = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync(SelectedLeadPair);
                    break;
                case "LockToWorldIPC":
                    if (SelectedLeadPair == null)
                        break;
                    SelectedLeadPair.LockToWorld = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync(SelectedLeadPair);
                    break;
                case "DisableFlightIPC":
                    if (SelectedLeadPair == null)
                        break;
                    SelectedLeadPair.DisableFlight = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync(SelectedLeadPair);
                    break;
                case "DisableSeatsIPC":
                    if (SelectedLeadPair == null)
                        break;
                    SelectedLeadPair.DisableSeats = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync(SelectedLeadPair);
                    break;
                case "BlindfoldIPC":
                    if (SelectedLeadPair == null)
                        break;
                    SelectedLeadPair.Blindfold = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync(SelectedLeadPair);
                    break;
                case "DeafenIPC":
                    if (SelectedLeadPair == null)
                        break;
                    SelectedLeadPair.Deafen = state;
                    TWNetSendHelpers.SendMasterRemoteSettingsAsync(SelectedLeadPair);
                    break;
            }
        }

        [UIEventHandler("disconnectTWNet")]
        public static void DisconnectTWNet()
        {
            TWNetClient.Instance.DisconnectClient();
        }

        [UIEventHandler("reconnectTWNet")]
        public static void ReconnectTWNet()
        {
            if (TWNetClient.Instance.IsTWNetConnected())
            {
                UIUtils.ShowNotice("Notice", "You are already connected to TWNet, this function is only for reconnecting if a problem prevented you from reconnecting automatically!");
                return;
            }
            
            TWNetClient.Instance.DisconnectClient();
            TWNetClient.Instance.ConnectClient();
        }

        [UIEventHandler("clearNotifs")]
        public static void ClearNotifications()
        {
            NotificationSystem.ClearNotification();
        }

        [UIEventHandler("joinDiscord")]
        public static void JoinDiscord()
        {
            Process.Start("https://discord.gg/GbyjZYVEEx"); 
        }

        [UIEventHandler("openPiShock")]
        public static void OpenPiShockPage()
        {
            Process.Start("https://pishock.com/#/?campaign=tw");
        }

        [UIEventHandler("openLicences")]
        public static void OpenLicencesPage()
        {
            Process.Start("https://wiki.totallywholeso.me/third-party-licenses");
        }
        
        [UIEventHandler("openEULA")]
        public static void OpenEULA()
        {
            Process.Start("https://wiki.totallywholeso.me/eula");
        }

        [UIEventHandler("testToys")]
        public static void TestToys()
        {
            ButtplugManager.Instance.BeepBoop();
            PiShockManager.Instance.BeepBoop();
        }

        [UIEventHandler("LogoPositionX")]
        public static void LogoPositionX()
        {
            TWUtils.OpenKeyboard(Configuration.JSONConfig.LogoPositionX.ToString(), s =>
            {
                if (!int.TryParse(s, out var num))
                {
                    UIUtils.ShowNotice("Invalid Position", "You must only enter numbers for the position!");
                    return;
                }

                if (num > 1460 || num < -300)
                {
                    UIUtils.ShowNotice("Invalid Position", "X position must be no lower then -300 and no higher then 1460! Setting it outside this region may hide the TW button!");
                    return;
                }

                Configuration.JSONConfig.LogoPositionX = num;
                Configuration.SaveConfig();
                
                UIUtils.SendModInit();
                
                Con.Debug($"LogoPositionX updated to {s}");
            });       
        }

        [UIEventHandler("LogoPositionY")]
        public static void LogoPositionY()
        {
            TWUtils.OpenKeyboard(Configuration.JSONConfig.LogoPositionY.ToString(), s =>
            {
                if (!int.TryParse(s, out var num))
                {
                    UIUtils.ShowNotice("Invalid Position", "You must only enter numbers for the position!");
                    return;
                }

                if (num > 1460 || num < -300)
                {
                    UIUtils.ShowNotice("Invalid Position", "Y position must be no lower then -300 and no higher then 1460! Setting it outside this region may hide the TW button!");
                    return;
                }

                Configuration.JSONConfig.LogoPositionY = num;
                Configuration.SaveConfig();
                
                UIUtils.SendModInit();
                
                Con.Debug($"LogoPositionY updated to {s}");
            });    
        }

        [UIEventHandler("changeBranch")]
        public static void OnChangeBranch()
        {
            if (_branchSelection == null)
            {
                UIUtils.ShowNotice("No Branches", "You do not have an additional branches, if you should please check that your Rank Key is set! If you still have issues check the appropriate channels in the discord!");
                return;
            }
            
            UIUtils.OpenMultiSelect(_branchSelection);
        }

        private void WorldJoinLeave()
        {
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twChangeWorld");
        }

        private void UserLeave(CVRPlayerEntity player)
        {
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twRemovePet", player.Uuid);
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twRemovePlayer", player.Uuid);
        }

        private void UserJoin(CVRPlayerEntity player)
        {
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twAddPlayer", player.Username, player.Uuid, player.PlayerDescriptor.profileImageUrl);
        }

        private void HandleButtonAction(string action)
        {
            if (_multiSelectionOptions.ContainsKey(action))
            {
                var ms = _multiSelectionOptions[action];
                UIUtils.OpenMultiSelect(ms);
                return;
            }

            if (_generatedRemoteControlSingles.ContainsKey(action) && ParamControlLeadPair != null)
            {
                var param = _generatedRemoteControlSingles[action];
                UIUtils.OpenNumberInput(param.ParameterTarget, param.ParameterValue, f =>
                {
                    param.ParameterValue = f;
                    param.IsUpdated = true;
                    AvatarParameterManager.Instance.SendUpdatedParameters(ParamControlLeadPair);
                });
                
                return;
            }
            
            if (!_handlers.TryGetValue(action, out var func))
                return;
            
            Con.Debug($"Found action for {action}");

            func();
        }

        private void CheckUpdateUI()
        {
            bool updateNeeded = true;
            string resourceHash;
            
                
            using (var uiResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TotallyWholesome.TWUI.TWUIBuild.zip"))
                
            {
                if (uiResourceStream == null)
                {
                    Con.Error("Unable to load TWUI Resource! Please post this log in the TW Discord!");
                    return;
                }
                
                using var tempStream = new MemoryStream((int) uiResourceStream.Length);
                uiResourceStream.CopyTo(tempStream);
                
                resourceHash = TWUtils.CreateMD5(tempStream.ToArray());
                
                var uiDir = new DirectoryInfo("ChilloutVR_Data\\StreamingAssets\\Cohtml\\UIResources\\GameUI\\mods\\TWUI");
                if (uiDir.Exists)
                {
                    var file = uiDir.GetFiles().FirstOrDefault(x => x.Name.Equals("TWUIBuildHash"));

                    if (file != null)
                    {
                        var fileHash = File.ReadAllText(file.FullName);

                        updateNeeded = !resourceHash.Equals(fileHash, StringComparison.InvariantCultureIgnoreCase);
                    }
                }
                else
                {
                    uiDir.Create();
                }

                if (updateNeeded && resourceHash != null)
                {
                    Con.Msg("TWUI needs to be updated, extracting updated resources!");
                    
                    var fastZip = new FastZip();
                    
                    fastZip.ExtractZip(uiResourceStream, uiDir.FullName, FastZip.Overwrite.Always, null, "", "", true, true);
                    
                    File.WriteAllText(Path.Combine(uiDir.FullName, "TWUIBuildHash"), resourceHash);
                }
            }
        }
    }

    public class UIEventHandlerAttribute : Attribute
    {
        public string[] UIActions { get; private set; }

        public UIEventHandlerAttribute(params string[] uiActions)
        {
            UIActions = uiActions;
        }
    }
}