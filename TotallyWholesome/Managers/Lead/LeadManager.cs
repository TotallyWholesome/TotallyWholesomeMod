using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ABI_RC.Core.Base;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Util;
using ABI.CCK.Components;
using cohtml;
using TotallyWholesome.Managers.AvatarParams;
using TotallyWholesome.Managers.Lead.LeadComponents;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using TotallyWholesome.Objects;
using TotallyWholesome.TWUI;
using TWNetCommon;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.NestedObjects;
using UnityEngine;
using WholesomeLoader;
using Object = UnityEngine.Object;
using StatusManager = TotallyWholesome.Managers.Status.StatusManager;

namespace TotallyWholesome.Managers.Lead
{
    public class LeadManager : ITWManager
    {
        public static LeadManager Instance;
        public static Action<LeadPair> OnLeadPairCreated;
        public static Action<LeadPair> OnLeadPairDestroyed;
        public static Action<LeadPair> OnFollowerPairCreated;
        public static Action<LeadPair> OnFollowerPairDestroyed;

        public string LastKey;
        public bool FollowerRequest;
        //LeadPair for the connection from this user to a master
        public LeadPair MasterPair;
        //LeadPairs for the connections from this user to their pets
        public LeadPair TugOfWarPair;
        public List<LeadPair> PetPairs = new();
        public bool ClearLeashWhileLeashed;
        public DateTime RecreatedLeashFromLastInstance;

        public string MasterId => MasterPair?.MasterID;

        public Dictionary<string, LeadPair> ActiveLeadPairs;
        public List<string> LastPairKeys; //Used for world change
        public List<string> LastFollowerPairKeys;
        public string LastMasterPairKey;

        //all pets settings
        public bool ForcedMute = false;
        public bool TempUnlockLeash = false;
        public bool DisableSeats = false;
        public bool DisableFlight = false;
        public bool LockToProp = false;
        public bool LockToWorld = false;
        public bool Blindfold = false;
        public bool Deafen = false;
        public SliderFloat TetherRange;
        public SliderFloat TetherRangeIPC;
        public Vector3 LeashPinPosition = Vector3.zero;
        public string PropTarget = null;

        private LeadRequest _pendingRequest;
        private bool _petRequest;
        private List<string> _pairKeys;
        private static SliderFloat _rSliderFloat;
        private static SliderFloat _gSliderFloat;
        private static SliderFloat _bSliderFloat;
        private static MultiSelection _propSelection;
        private static MultiSelection _propSelectionGlobal;
        private static MultiSelection _leashStyleSelection;
        private static Dictionary<string, string> _props;
        private static TWRaycaster _twRaycaster;

        public void Setup()
        {
            Instance = this;

            TetherRange = new SliderFloat("leashLengthSlider", 2f);
            TetherRange.OnValueUpdated += f => { TWNetSendHelpers.UpdateMasterSettingsAsync(); };
            TetherRangeIPC = new SliderFloat("leashLengthSliderIPC", 2f);
            TetherRangeIPC.OnValueUpdated += f =>
            {
                var leadPair = UserInterface.Instance.SelectedLeadPair;
                
                if (leadPair == null)
                    return;

                leadPair.LeadLength = f;
                TWNetSendHelpers.UpdateMasterSettingsAsync(leadPair);
            }; 
            _pairKeys = new List<string>();
            LastPairKeys = new List<string>();
            LastFollowerPairKeys = new List<string>();
            ActiveLeadPairs = new Dictionary<string, LeadPair>();
            _props = new Dictionary<string, string>();

            Patches.Patches.OnAvatarInstantiated += OnAvatarIsReady;
            Patches.Patches.OnWorldLeave += OnWorldLeave;
            Patches.Patches.UserLeave += OnPlayerLeave;
            Patches.Patches.OnInviteAccepted += OnInviteAccepted;
            Patches.Patches.OnPropSpawned += OnPropSpawned;
            Patches.Patches.OnPropDelete += OnPropDelete;

            TWNetListener.MasterSettingsEvent += OnMasterSettingsUpdate;
            TWNetListener.LeadAcceptEvent += OnAccept;
            TWNetListener.LeadRemoveEvent += OnRemove;
            TWNetListener.PetRequestEvent += OnRequestPet;
            TWNetListener.MasterRequestEvent += OnRequestMaster;
            TWNetListener.MasterRemoteControlEvent += OnMasterRemoteControl;
            TWNetListener.LeashConfigUpdate += LeashConfigUpdate;
            TWNetClient.OnTWNetDisconnected += OnTWNetDisconnected;

            if (!ColorUtility.TryParseHtmlString(Configuration.JSONConfig.LeashColour, out var currentLeadColor))
            {
              currentLeadColor = Color.white;  
            };
            
            _rSliderFloat = new SliderFloat("rFloat", currentLeadColor.r);
            _gSliderFloat = new SliderFloat("gFloat", currentLeadColor.g);
            _bSliderFloat = new SliderFloat("bFloat", currentLeadColor.b);
            _rSliderFloat.OnValueUpdated += f => OnColorChanged();
            _gSliderFloat.OnValueUpdated += f => OnColorChanged();
            _bSliderFloat.OnValueUpdated += f => OnColorChanged();

            _leashStyleSelection = new MultiSelection("Leash Style", Enum.GetNames(typeof(LeashStyle)), (int)Configuration.JSONConfig.LeashStyle);

            _leashStyleSelection.OnOptionUpdated += i =>
            {
                Configuration.JSONConfig.LeashStyle = (LeashStyle)i;
                Configuration.SaveConfig();
                
                TWNetSendHelpers.SendLeashConfigUpdate();
            };

            _propSelection = new MultiSelection("Prop Selection", Array.Empty<string>(), 0);
            _propSelectionGlobal = new MultiSelection("Prop Selection", Array.Empty<string>(), 0);

            _propSelection.OnOptionUpdated += i =>
            {
                if (UserInterface.Instance.SelectedLeadPair == null) return;
                
                UserInterface.Instance.SelectedLeadPair.PropTarget = _props.Keys.ToArray()[i];
                if (!UserInterface.Instance.SelectedLeadPair.LockToProp) return;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(UserInterface.Instance.SelectedLeadPair);
            };

            _propSelectionGlobal.OnOptionUpdated += i =>
            {
                PropTarget = _props.Keys.ToArray()[i];
                if (!LockToProp) return;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync();
            };
        }

        public void LateSetup()
        {
            SetupTWRaycaster();
        }

        public int Priority() => 1;
        public string ManagerName() => "LeadManager";

        public void SetupTWRaycaster()
        {
            if (_twRaycaster == null)
            {
                var twRaycaster = Object.Instantiate(TWAssets.TWRaycaster);
                Object.DontDestroyOnLoad(twRaycaster);
                _twRaycaster = twRaycaster.GetComponent<TWRaycaster>();
            }
            
            if (MetaPort.Instance.isUsingVr)
            {
                var rightRaycaster = Object.FindObjectsOfType<ControllerRay>().FirstOrDefault(x => x.name.Equals("RayCasterRight"));
                if (rightRaycaster == null)
                {
                    Con.Error("An error occured! No RayCasterRight was found!");
                    return;
                }

                _twRaycaster.transform.parent = rightRaycaster.gameObject.transform;
            }
            else
            {
                _twRaycaster.transform.parent = PlayerSetup.Instance.desktopCamera.transform;
            }
            
            _twRaycaster.transform.localEulerAngles = Vector3.zero;
            _twRaycaster.transform.localPosition = MetaPort.Instance.isUsingVr ? Vector3.zero : new Vector3(0, -1, 0);
            _twRaycaster.gameObject.SetActive(false);
        }

        private void OnWorldLeave()
        {
            //Store last pairKeys for instance change functions
            LastPairKeys.Clear();
            LastPairKeys.AddRange(_pairKeys);
            LastFollowerPairKeys.AddRange(ActiveLeadPairs.Where(x => x.Value != null && x.Value.AreWeMaster()).Select(c => c.Key));
            _pairKeys.Clear();
            ActiveLeadPairs.Clear();
            LastKey = null;
            FollowerRequest = false;
            MasterPair = null;
            PetPairs.Clear();
            ForcedMute = false;
            TempUnlockLeash = false;
            LastMasterPairKey = null;
            Patches.Patches.IsForceMuted = false;
            _props.Clear();
        }
        
        public LeadPair GetLeadPairForPet(TWPlayerObject player)
        {
            return ActiveLeadPairs.FirstOrDefault(x => x.Value.Master.Uuid.Equals(MetaPort.Instance.ownerId) && x.Value.Pet.Uuid.Equals(player.Uuid)).Value;
        }

        #region UI Actions

        public void OnColorChanged()
        {
            var color = new Color(_rSliderFloat.SliderValue, _gSliderFloat.SliderValue, _bSliderFloat.SliderValue);
            var colorhtml = ColorUtility.ToHtmlStringRGB(color);
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twLeadColorChanged", colorhtml);
        }

        [UIEventHandler("selectWorldPosition")]
        public static void SelectWorldPosition()
        {
            CVR_MenuManager.Instance.ToggleQuickMenu(false);
            _twRaycaster.StartRaycaster(vector3 =>
            {
                Instance.LeashPinPosition = vector3;
                if (!Instance.LockToWorld) return;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync();
            });
        }
        
        [UIEventHandler("selectWorldPositionIPC")]
        public static void SelectWorldPositionIPC()
        {
            CVR_MenuManager.Instance.ToggleQuickMenu(false);
            _twRaycaster.StartRaycaster(vector3 =>
            {
                if (UserInterface.Instance.SelectedLeadPair == null) return;
                UserInterface.Instance.SelectedLeadPair.LeashPinPosition = vector3;
                if (!UserInterface.Instance.SelectedLeadPair.LockToWorld) return;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(UserInterface.Instance.SelectedLeadPair);
            });
        }

        [UIEventHandler("selectBoundProp")]
        public static void SelectBoundProp()
        {
            _propSelectionGlobal.Options = _props.Values.ToArray();
            UIUtils.OpenMultiSelect(_propSelectionGlobal);
        }

        [UIEventHandler("selectBoundPropIPC")]
        public static void SelectBoundPropIPC()
        {
            _propSelection.Options = _props.Values.ToArray();
            UIUtils.OpenMultiSelect(_propSelection);
        }

        [UIEventHandler("selectLeashStyle")]
        public static void SelectLeashStyle()
        {
            UIUtils.OpenMultiSelect(_leashStyleSelection);
        }

        [UIEventHandler("saveColor")]
        public static void SaveColor()
        {
            var newColor = new Color(_rSliderFloat.SliderValue, _gSliderFloat.SliderValue, _bSliderFloat.SliderValue);
            Configuration.JSONConfig.LeashColour = "#" + ColorUtility.ToHtmlStringRGB(newColor);
            Configuration.SaveConfig();
            
            TWNetSendHelpers.SendLeashConfigUpdate();
        }

        [UIEventHandler("clearLeads")]
        public static void ClearLeads()
        {
            UIUtils.ShowConfirm("Clear All Leads?", "This will remove all leads associated with you, are you sure?", "Yes", () =>
            {
                //Reset IsForceMuted on clear all leads
                Patches.Patches.IsForceMuted = false;

                if (Instance.MasterPair != null)
                {
                    //Cleared leash while leashed
                    Instance.ClearLeashWhileLeashed = true;
                }
                
                TWNetClient.Instance.Send(new LeadAccept()
                {
                    LeadRemove = true
                }, TWNetMessageTypes.LeadAccept);
                
                UIUtils.ShowToast($"Removed all associated leashes!");
            });
        }

        [UIEventHandler("removeLeashIPC")]
        public static void RemoveLeashIPC()
        {
            var leadPair = UserInterface.Instance.SelectedLeadPair;
                
            if (leadPair == null)
                return;
            
            UIUtils.ShowConfirm("Remove Leash?", $"This will remove the leash with {leadPair.Pet.Username}", "Yes", () =>
            {
                TWNetClient.Instance.Send(new LeadAccept()
                {
                    Key = leadPair.Key,
                    LeadRemove = true
                }, TWNetMessageTypes.LeadAccept); 
                
                UIUtils.ShowToast($"Removed leash with {leadPair.Pet.Username}!");
            });
        }

        [UIEventHandler("requestPet")]
        public static void RequestToBePet()
        {
            UIUtils.ShowConfirm("Send Request?", $"Are you sure you would like to request {UserInterface.Instance.SelectedPlayerUsername} to be your master?", "Yes", () =>
            {
                string key = Guid.NewGuid().ToString();

                LeadRequest request = new LeadRequest()
                {
                    Target = UserInterface.Instance.SelectedPlayerID,
                    BoneTarget = (int)Configuration.JSONConfig.PetBoneTarget,
                    NoVisibleLeash = ConfigManager.Instance.IsActive(AccessType.NoVisibleLeash, UserInterface.Instance.SelectedPlayerID),
                    PrivateLeash = ConfigManager.Instance.IsActive(AccessType.PrivateLeash, UserInterface.Instance.SelectedPlayerID),
                    LeashColour = ConfigManager.Instance.IsActive(AccessType.UseCustomLeashColour) ? Configuration.JSONConfig.LeashColour : "",
                    Key = key
                };

                //Set FollowerRequest for handing the broadcast event
                Instance.FollowerRequest = true;
                //Set LastKey for validating the broadcast event
                Instance.LastKey = key;
                        
                TWNetClient.Instance.Send(request, TWNetMessageTypes.LeadRequest);
                
                UIUtils.ShowToast($"Requested {UserInterface.Instance.SelectedPlayerUsername} to be your master!");
            });
        }

        [UIEventHandler("requestMaster")]
        public static void RequestToBeMaster()
        {
            UIUtils.ShowConfirm("Send Request?", $"Are you sure you would like to request {UserInterface.Instance.SelectedPlayerUsername} to become your pet?", "Yes", () =>
            {
                string key = Guid.NewGuid().ToString();

                LeadRequest request = new LeadRequest()
                {
                    Target = UserInterface.Instance.SelectedPlayerID,
                    BoneTarget = (int)Configuration.JSONConfig.MasterBoneTarget,
                    LeadLength = Instance.TetherRange.SliderValue,
                    NoVisibleLeash = ConfigManager.Instance.IsActive(AccessType.NoVisibleLeash, UserInterface.Instance.SelectedPlayerID),
                    PrivateLeash = ConfigManager.Instance.IsActive(AccessType.PrivateLeash, UserInterface.Instance.SelectedPlayerID),
                    LeashColour = ConfigManager.Instance.IsActive(AccessType.UseCustomLeashColour) ? Configuration.JSONConfig.LeashColour : "",
                    LeashStyle = (int)Configuration.JSONConfig.LeashStyle,
                    MasterRequest = true,
                    DisableFlight = Instance.DisableFlight,
                    DisableSeats = Instance.DisableSeats,
                    TempUnlockLeash = Instance.TempUnlockLeash,
                    Key = key
                };

                //Set FollowerRequest for handing the broadcast event
                Instance.FollowerRequest = false;
                //Set LastKey for validating the broadcast event
                Instance.LastKey = key;
                        
                TWNetClient.Instance.Send(request, TWNetMessageTypes.LeadRequest);
                
                UIUtils.ShowToast($"Requested {UserInterface.Instance.SelectedPlayerUsername} to be your pet!");
            });
        }
        
        private void OnInviteAccepted(Invite_t invite)
        {
            if (!invite.InviteMeshId.Contains(_pendingRequest.Key))
            {
                NotificationSystem.EnqueueNotification("Totally Wholesome", "You accepted a request that did not exist!", 4f, TWAssets.Alert);
                return;
            }
            
            if(_petRequest)
                TWNetSendHelpers.AcceptPetRequest(_pendingRequest.Key, _pendingRequest.RequesterID);
            else
                TWNetSendHelpers.AcceptMasterRequest(_pendingRequest.Key, _pendingRequest.RequesterID);
        }

        #endregion

        #region Network Event

        public void OnRequestMaster(LeadRequest packet)
        {
            if (ConfigManager.Instance.IsActive(AccessType.BlockUser, packet.RequesterID)) return;
            
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {

                if ((ConfigManager.Instance.IsActive(AccessType.AutoAcceptFriendsOnly) && Friends.FriendsWith(packet.RequesterID)) ||
                    !ConfigManager.Instance.IsActive(AccessType.AutoAcceptFriendsOnly))
                {
                    if (ConfigManager.Instance.IsActive(AccessType.AutoAcceptMasterRequest, packet.RequesterID))
                    {
                        Con.Debug("Auto accepted master request");
                        NotificationSystem.EnqueueNotification("Totally Wholesome", "You have auto accepted a master request!", 4f, TWAssets.Crown);
                        TWNetSendHelpers.AcceptMasterRequest(packet.Key, packet.RequesterID);
                        return;
                    }
                }

                var requester = TWUtils.GetPlayerFromPlayerlist(packet.RequesterID);
                _pendingRequest = packet;
                _petRequest = false;
                
                UIUtils.AddCVRNotification(packet.Key, "Totally Wholesome", $" | {requester.Username} is requesting to become your pet!");
            });
        }

        public void OnRequestPet(LeadRequest packet)
        {
            if (ConfigManager.Instance.IsActive(AccessType.BlockUser, packet.RequesterID)) return;

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {

                if ((ConfigManager.Instance.IsActive(AccessType.AutoAcceptFriendsOnly) && Friends.FriendsWith(packet.RequesterID)) ||
                    !ConfigManager.Instance.IsActive(AccessType.AutoAcceptFriendsOnly))
                {
                    if (ConfigManager.Instance.IsActive(AccessType.AutoAcceptPetRequest, packet.RequesterID))
                    {
                        Con.Debug("Auto accepted pet request");
                        NotificationSystem.EnqueueNotification("Totally Wholesome", "You have auto accepted a pet request!", 4f, TWAssets.Handcuffs);
                        TWNetSendHelpers.AcceptPetRequest(packet.Key, packet.RequesterID);
                        return;
                    }
                }

                var requester = TWUtils.GetPlayerFromPlayerlist(packet.RequesterID);
                _pendingRequest = packet;
                _petRequest = true;
                
                UIUtils.AddCVRNotification(packet.Key, "Totally Wholesome", $" | {requester.Username} is requesting for you to become their pet!");
            });
        }

        public void OnMasterSettingsUpdate(MasterSettings packet)
        {
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                //Check within the queued up event, just incase the pair gets deleted before this is ran somehow
                if (!ActiveLeadPairs.ContainsKey(packet.Key)) return;
                if (ActiveLeadPairs[packet.Key].LineController == null) return;

                ActiveLeadPairs[packet.Key].LeadLength = packet.LeadLength;
                ActiveLeadPairs[packet.Key].LineController.UpdateLeadLength(packet.LeadLength);

                ActiveLeadPairs[packet.Key].TempUnlockLeash = packet.TempUnlockLeash;
                ActiveLeadPairs[packet.Key].LineController.SetTempUnlockLeash(packet.TempUnlockLeash);
            });
        }

        private void OnMasterRemoteControl(MasterRemoteControl packet)
        {
            if (MasterPair == null || !MasterPair.Key.Equals(packet.Key))
                return;

            MasterPair.ForcedMute = packet.GagPet;

            ApplyForcedMute(packet.GagPet);

            bool shouldUpdate = false;
            
            if (MasterPair.PropTarget != packet.PropTarget && ConfigManager.Instance.IsActive(AccessType.AllowWorldPropPinning, MasterPair.MasterID))
            {
                Con.Debug("PropTarget was updated! Setting FollowerPair and marking for update!");
                var lockCheck = !string.IsNullOrWhiteSpace(packet.PropTarget);
                shouldUpdate = lockCheck != MasterPair.LockToProp || (lockCheck && MasterPair.PropTarget != packet.PropTarget);
                MasterPair.LockToProp = lockCheck;
                MasterPair.PropTarget = packet.PropTarget;
            }

            var vectorFromNetwork = packet.LeashPinPosition.ToVector3();

            if (!MasterPair.LockToProp && MasterPair.LeashPinPosition != vectorFromNetwork && ConfigManager.Instance.IsActive(AccessType.AllowWorldPropPinning, MasterPair.MasterID))
            {
                Con.Debug("LeashPinPosition was updated! Setting FollowerPair and marking for update!");
                var lockCheck = !Equals(packet.LeashPinPosition, TWVector3.Zero);
                shouldUpdate = lockCheck != MasterPair.LockToWorld || (lockCheck && MasterPair.LeashPinPosition != vectorFromNetwork);
                MasterPair.LeashPinPosition = vectorFromNetwork;
                MasterPair.LockToWorld = lockCheck;
            }
            
            if(shouldUpdate)
                TWNetSendHelpers.SendLeashConfigUpdate(MasterPair);
        }

        private void ApplyForcedMute(bool mute)
        {
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (mute && !Patches.Patches.IsForceMuted && ConfigManager.Instance.IsActive(AccessType.AllowForceMute, MasterId))
                {
                    //Clear before sending to ensure the gag message appears on top
                    NotificationSystem.ClearNotification();
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "You have been gagged!", 3f, TWAssets.MicrophoneOff);
                    if (!ConfigManager.Instance.IsActive(AccessType.EnableMuffledMode, MasterId))
                        Audio.SetMicrophoneActive(true);

                    Patches.Patches.IsForceMuted = true;
                    Patches.Patches.IsMuffled = ConfigManager.Instance.IsActive(AccessType.EnableMuffledMode, MasterId);
                }
                else if (!mute && Patches.Patches.IsForceMuted)
                {
                    NotificationSystem.ClearNotification();
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "You have been ungagged!", 3f, TWAssets.MicrophoneOff);
                    Patches.Patches.IsForceMuted = false;
                    Patches.Patches.IsMuffled = false;
                }
                
                //Always reapply TWGag state
                AvatarParameterManager.Instance.TrySetParameter("TWGag", mute && ConfigManager.Instance.IsActive(AccessType.AllowForceMute, MasterId) ? 1 : 0);
            });
        }

        public void OnRemove(LeadAccept packet)
        {
            Con.Debug("Attempting to remove a leash from a follower");

            if (!ActiveLeadPairs.ContainsKey(packet.Key)) return;

            LeadPair pair = ActiveLeadPairs[packet.Key];
            ActiveLeadPairs.Remove(packet.Key);

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                RemoveLeash(pair);
            });
        }

        public void OnAccept(LeadAccept packet)
        {
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                LeadPair pair = new LeadPair(null, null, packet.Key, (HumanBodyBones)packet.PetBoneTarget, (HumanBodyBones)packet.MasterBoneTarget, packet.LeadLength, packet.ForcedMute, packet.NoVisibleLeash, packet.TempUnlockLeash);
                ActiveLeadPairs[packet.Key] = pair;
                pair.MasterID = packet.MasterID;
                pair.PetID = packet.FollowerID;
                if (!string.IsNullOrWhiteSpace(packet.PetLeashColour) && Regex.Match(packet.PetLeashColour, "^#(?:[0-9a-fA-F]{3}){1,2}$").Success)
                    pair.PetLeashColour = packet.PetLeashColour;

                if (!string.IsNullOrWhiteSpace(packet.MasterLeashColour) && Regex.Match(packet.MasterLeashColour, "^#(?:[0-9a-fA-F]{3}){1,2}$").Success)
                    pair.MasterLeashColour = packet.MasterLeashColour;

                if (!string.IsNullOrWhiteSpace(packet.PropTarget))
                    pair.PropTarget = packet.PropTarget;

                if (Enum.IsDefined(typeof(LeashStyle), packet.LeashStyle))
                    pair.LeashStyle = (LeashStyle)packet.LeashStyle;

                if (packet.LeashPinPosition.Equals(TWVector3.Zero))
                    pair.LeashPinPosition = packet.LeashPinPosition.ToVector3();

                TWPlayerObject follower = TWUtils.GetPlayerFromPlayerlist(packet.FollowerID);
                TWPlayerObject master = TWUtils.GetPlayerFromPlayerlist(packet.MasterID);

                if (follower == null || master == null) return;

                pair.Master = master;
                pair.Pet = follower;

                //We are the follower
                if (follower.Equals(TWUtils.GetOurPlayer()))
                {
                    if ((!FollowerRequest || !packet.Key.Equals(LastKey)) && !LastPairKeys.Contains(packet.Key))
                    {
                        Con.Error("A follower accept was sent but we are not expecting a follower accept, or the key is wrong!");
                        return;
                    }

                    if (LastPairKeys.Contains(packet.Key))
                        RecreatedLeashFromLastInstance = DateTime.Now;

                    StatusManager.Instance.UpdatePetMasterMark(packet.MasterID, false, true);

                    if(!_pairKeys.Contains(packet.Key))
                        _pairKeys.Add(packet.Key);

                    //Reset last key after setting up lead
                    LastKey = null;
                    LastMasterPairKey = packet.Key;
                }

                //We are the master
                if (master.Equals(TWUtils.GetOurPlayer()))
                {
                    if ((FollowerRequest || !packet.Key.Equals(LastKey)) && !LastPairKeys.Contains(packet.Key))
                    {
                        Con.Error("A master accept was sent but we are not expecting a master accept, or the key is wrong!");
                        return;
                    }
                    
                    if (LastPairKeys.Contains(packet.Key))
                        RecreatedLeashFromLastInstance = DateTime.Now;

                    StatusManager.Instance.UpdatePetMasterMark(packet.FollowerID, true, false);
                    
                    CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twAddPet", follower.Username, follower.Uuid);

                    AvatarParameterManager.Instance.TrySetParameter("TWMaster", 1);

                    if(!_pairKeys.Contains(packet.Key))
                        _pairKeys.Add(packet.Key);

                    //Reset last key after setting up lead
                    LastKey = null;
                }
                
                ApplyLeash(pair, packet.LeadLength);
            });
        }
        
        private void LeashConfigUpdate(LeashConfigUpdate packet)
        {
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (!ActiveLeadPairs.ContainsKey(packet.Key)) return;
            
                var leadPair = ActiveLeadPairs[packet.Key];

                if (!string.IsNullOrWhiteSpace(packet.PetLeashColour) && Regex.Match(packet.PetLeashColour, "^#(?:[0-9a-fA-F]{3}){1,2}$").Success)
                {
                    leadPair.PetLeashColour = packet.PetLeashColour;
                    
                    if (leadPair.LineController != null && ColorUtility.TryParseHtmlString(packet.PetLeashColour, out var colour))
                    {
                        leadPair.LineController.UpdateLineColours(null, colour);
                    }
                }

                if (!string.IsNullOrWhiteSpace(packet.MasterLeashColour) && Regex.Match(packet.MasterLeashColour, "^#(?:[0-9a-fA-F]{3}){1,2}$").Success)
                {
                    leadPair.MasterLeashColour = packet.MasterLeashColour;

                    if (leadPair.LineController != null && ColorUtility.TryParseHtmlString(packet.MasterLeashColour, out var colour))
                    {
                        leadPair.LineController.UpdateLineColours(colour);
                    }
                }

                if (!string.IsNullOrWhiteSpace(packet.PropTarget) && ((MasterPair == leadPair && ConfigManager.Instance.IsActive(AccessType.AllowWorldPropPinning, leadPair.MasterID)) || MasterPair != leadPair))
                {
                    leadPair.PropTarget = packet.PropTarget;
                    leadPair.LockToProp = true;

                    var prop = CVRSyncHelper.Props.FirstOrDefault(x => x.InstanceId.Equals(packet.PropTarget));

                    if (prop != null && leadPair.LineController != null)
                    {
                        Transform propTarget = TWUtils.GetRootGameObject(prop.Spawnable.gameObject, "TWLPropAnchor");
                        if (propTarget == null)
                            propTarget = prop.Spawnable.transform;

                        leadPair.LineController.targetOverride = propTarget;
                    }
                }
                else
                {
                    leadPair.LockToProp = false;
                    
                    if (leadPair.LineController != null)
                        leadPair.LineController.targetOverride = null;
                }

                if (Enum.IsDefined(typeof(LeashStyle), packet.LeashStyle))
                {
                    leadPair.LeashStyle = (LeashStyle)packet.LeashStyle;

                    if (leadPair.LineController != null)
                    {
                        //Apply line renderer Mat
                        var matinfo = TWUtils.GetStyleMat(leadPair.LeashStyle);

                        if (matinfo.Item1 == null)
                        {
                            if (ConfigManager.Instance.IsActive(AccessType.HideCustomLeashStyle, leadPair.MasterID))
                            {
                                leadPair.LineController.UpdateLineMaterial(TWAssets.Classic, matinfo.Item2);
                    
                            }
                            else
                            {
                                try
                                {
                                    Transform matTransfrom = TWUtils.GetRootGameObject(leadPair.Master.AvatarObject, "TWLCustomLeadMat");
                                    Material customMat = matTransfrom.GetComponent<MeshRenderer>().materials[0];
                                    leadPair.LineController.UpdateLineMaterial(customMat, matinfo.Item2);
                                    Con.Debug("Applied custom lead mat");
                                }
                                catch
                                {
                                    leadPair.LineController.UpdateLineMaterial(TWAssets.Classic, matinfo.Item2);
                                    Con.Debug("Failed to apply custom lead mat");
                                }
                            }
                        }
                        else
                        {
                            leadPair.LineController.UpdateLineMaterial(matinfo.Item1, matinfo.Item2);
                        }
                    }
                }

                if (!Equals(packet.LeashPinPosition, TWVector3.Zero) && ((MasterPair == leadPair && ConfigManager.Instance.IsActive(AccessType.AllowWorldPropPinning, leadPair.MasterID)) || MasterPair != leadPair))
                {
                    leadPair.LeashPinPosition = packet.LeashPinPosition.ToVector3();
                    leadPair.LockToWorld = true;
                    if (leadPair.LineController != null)
                        leadPair.LineController.targetOverrideVector = leadPair.LeashPinPosition;
                }
                else
                {
                    leadPair.LockToWorld = false;
                    if(leadPair.LineController != null)
                        leadPair.LineController.targetOverrideVector = Vector3.zero;
                }
            });
        }

        #endregion

        #region Lead Functions

        public void RemoveLeash(LeadPair pair)
        {
            if (pair.Pet == null || pair.LineController == null) return;

            LineRenderer lineRenderer = pair.LineController.line;
            if (lineRenderer != null) 
                Object.Destroy(lineRenderer);

            if (Equals(pair.Pet, TWUtils.GetOurPlayer()))
            {
                pair.LineController.gameObject.layer = LayerMask.NameToLayer("PlayerLocal");
                MasterPair = null;
                OnFollowerPairDestroyed?.Invoke(pair);
                AvatarParameterManager.Instance.TrySetParameter("TWCollar", 0);

                StatusManager.Instance.UpdatePetMasterMark(pair.Master.Uuid, false, false);

                //Remove gag when leash is removed
                ApplyForcedMute(false);
            }
            
            //Delete and reset renderer after all other resets complete
            pair.LineController.ResetRenderer();
            Object.Destroy(pair.LineController);

            if (pair.Master.Uuid.Equals(MetaPort.Instance.ownerId))
            {
                if(PetPairs.Contains(pair))
                    PetPairs.Remove(pair);

                if (TugOfWarPair == pair)
                    TugOfWarPair = null;
                
                CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twRemovePet", pair.Pet.Uuid);
                StatusManager.Instance.UpdatePetMasterMark(pair.Pet.Uuid, false, false);
            }

            if (!ActiveLeadPairs.Values.Any(x => x.Master != null && x.Master.Equals(TWUtils.GetOurPlayer())))
                AvatarParameterManager.Instance.TrySetParameter("TWMaster", 0);
            OnLeadPairDestroyed?.Invoke(pair);
        }

        private void OnPlayerLeave(CVRPlayerEntity player)
        {
            foreach (var pair in ActiveLeadPairs.Where(x => x.Value.IsPlayerInvolved(TWUtils.GetPlayerFromPlayerlist(player.Uuid))).ToArray())
            {
                ActiveLeadPairs.Remove(pair.Key);
                OnLeadPairDestroyed?.Invoke(pair.Value);

                if (MasterPair != null && pair.Value.Key == MasterPair.Key)
                {
                    MasterPair = null;
                    OnFollowerPairDestroyed?.Invoke(pair.Value);
                }

                if (PetPairs.Contains(pair.Value))
                    PetPairs.Remove(pair.Value);

                if (TugOfWarPair == pair.Value)
                    TugOfWarPair = null;
                
                if (pair.Value.AreWeMaster())
                {
                    CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twRemovePet", pair.Value.Pet.Uuid);
                }
            }
        }

        private void OnAvatarIsReady(string userID)
        {
            var player = TWUtils.GetPlayerFromPlayerlist(userID);
            
            foreach (var pair in ActiveLeadPairs.Where(x => x.Value.IsPlayerInvolved(player)))
            {
                Con.Debug($"Reapplying leash involving {player.Username}!");
                if (pair.Value.Master == null || pair.Value.Pet == null)
                {
                    TWPlayerObject follower = TWUtils.GetPlayerFromPlayerlist(pair.Value.PetID);
                    TWPlayerObject master = TWUtils.GetPlayerFromPlayerlist(pair.Value.MasterID);

                    //Ensure Master and Follower are not null before continuing
                    if (master == null || follower == null) return;

                    //We are the follower
                    if (follower.Equals(TWUtils.GetOurPlayer()))
                    {
                        if ((!FollowerRequest || !pair.Value.Key.Equals(LastKey)) && !LastPairKeys.Contains(pair.Value.Key))
                        {
                            Con.Error("A follower request was sent but we are not expecting a follower request, or the key is wrong!");
                            return;
                        }

                        StatusManager.Instance.UpdatePetMasterMark(pair.Value.MasterID, false, true);

                        if(!_pairKeys.Contains(pair.Value.Key))
                            _pairKeys.Add(pair.Value.Key);

                        //Reset last key after setting up lead
                        LastKey = null;
                        LastMasterPairKey = pair.Key;
                    }

                    //We are the master
                    if (master.Equals(TWUtils.GetOurPlayer()))
                    {
                        if ((FollowerRequest || !pair.Value.Key.Equals(LastKey)) && !LastPairKeys.Contains(pair.Value.Key))
                        {
                            Con.Error("A master request was sent but we are not expecting a master request, or the key is wrong!");
                            return;
                        }

                        StatusManager.Instance.UpdatePetMasterMark(pair.Value.PetID, true, false);
                        
                        CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twAddPet", follower.Username, follower.Uuid);

                        AvatarParameterManager.Instance.TrySetParameter("TWMaster", 1);

                        if(!_pairKeys.Contains(pair.Value.Key))
                            _pairKeys.Add(pair.Value.Key);

                        //Reset last key after setting up lead
                        LastKey = null;
                    }

                    pair.Value.Master = master;
                    pair.Value.Pet = follower;
                }

                ApplyLeash(pair.Value, pair.Value.LeadLength);
            }
        }

        private void OnTWNetDisconnected()
        {
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                foreach (var pair in ActiveLeadPairs)
                {
                    RemoveLeash(pair.Value);
                }

                ActiveLeadPairs.Clear();
            });

            Con.Msg("Cleared all active leads after disconnection");
        }
        
        private void OnPropSpawned(CVRSpawnable obj)
        {
            //Add prop to our lists if needed
            var ourSpawnables = TWUtils.GetSpawnables();
            var spawnable = ourSpawnables.FirstOrDefault(x => x.SpawnableId == obj.guid);
            if (spawnable != null && !_props.ContainsKey(obj.instanceId))
            {
                _props.Add(obj.instanceId, spawnable.SpawnableName);
            }
            
            var pairs = ActiveLeadPairs.Values.Where(x => x.PropTarget != null && x.PropTarget.Equals(obj.instanceId)).ToArray();

            if (pairs.Length == 0) return;

            Transform propTarget = TWUtils.GetRootGameObject(obj.gameObject, "TWLPropAnchor");
            if (propTarget == null)
                propTarget = obj.transform;

            foreach (var pair in pairs)
            {
                pair.LineController.targetOverride = propTarget;
            }
        }
        
        private void OnPropDelete(CVRSpawnable obj)
        {
            if (_props.ContainsKey(obj.instanceId))
                _props.Remove(obj.instanceId);
        }

        private void ApplyLeash(LeadPair leadPair, float leadLength = 3f)
        {
            Con.Debug($"Applying leash to leadpair - {leadPair.Key}");

            if (leadPair.Pet == null || leadPair.Master == null) return;
            if (leadPair.Pet.Equals(leadPair.Master)) return;

            if (!ActiveLeadPairs.ContainsKey(leadPair.Key))
                ActiveLeadPairs.Add(leadPair.Key, leadPair);

            OnLeadPairCreated?.Invoke(leadPair);

            if (leadPair.Pet.AvatarObject == null || leadPair.Master.AvatarObject == null) return;

            Transform petBone = TWUtils.GetRootGameObject(leadPair.Pet.AvatarObject, "TWLPetAnchor");
            if (petBone == null && leadPair.Pet.AvatarAnimator != null)
                petBone = leadPair.Pet.AvatarAnimator.GetBoneTransform(leadPair.PetBoneTarget);

            Transform masterBone = TWUtils.GetRootGameObject(leadPair.Master.AvatarObject, "TWLMasterAnchor");
            if (masterBone == null && leadPair.Master.AvatarAnimator != null)
                masterBone = leadPair.Master.AvatarAnimator.GetBoneTransform(leadPair.MasterBoneTarget);

            if (petBone == null || masterBone == null) return;

            LineController followerController = petBone.gameObject.GetComponent<LineController>();
            LineRenderer lineRenderer = petBone.gameObject.GetComponent<LineRenderer>();

            if (followerController == null)
                followerController = petBone.gameObject.AddComponent<LineController>();
            if (lineRenderer == null)
                lineRenderer = petBone.gameObject.AddComponent<LineRenderer>();
            var colours = TWUtils.GetColourForLeadPair(leadPair.Master.Uuid, leadPair.Pet.Uuid);
            if (leadPair.MasterLeashColour != null)
                ColorUtility.TryParseHtmlString(leadPair.MasterLeashColour, out colours.Item1);
            if (leadPair.PetLeashColour != null)
                ColorUtility.TryParseHtmlString(leadPair.PetLeashColour, out colours.Item2);

            //Enable and setup renderer if a valid leader is known
            followerController.SetupRenderer(masterBone, leadPair.AreWeFollower() ? leadPair.Pet : null, leadPair.Master, lineRenderer, 20f, 20, leadLength, 0.5f, leadPair.NoVisibleLeash, colours.Item1, colours.Item2);
            
            
            //Apply line renderer Mat
            var matinfo = TWUtils.GetStyleMat(leadPair.LeashStyle);

            if (matinfo.Item1 == null)
            {
                if (ConfigManager.Instance.IsActive(AccessType.HideCustomLeashStyle, leadPair.MasterID))
                {
                    followerController.UpdateLineMaterial(TWAssets.Classic, matinfo.Item2);
                    
                }
                else
                {
                    try
                    {
                        Transform matTransfrom = TWUtils.GetRootGameObject(leadPair.Master.AvatarObject, "TWLCustomLeadMat");
                        Material customMat = matTransfrom.GetComponent<MeshRenderer>().materials[0];
                        followerController.UpdateLineMaterial(customMat, matinfo.Item2);
                        Con.Debug("Applied custom lead mat");
                    }
                    catch
                    {
                        followerController.UpdateLineMaterial(TWAssets.Classic, matinfo.Item2);
                        Con.Debug("Failed to apply custom lead mat");
                    }  
                }
            }
            else
            {
                followerController.UpdateLineMaterial(matinfo.Item1, matinfo.Item2);
            }

            //Set the prop target if the prop exists
            var prop = CVRSyncHelper.Props.FirstOrDefault(x => x.InstanceId.Equals(leadPair.PropTarget));

            if (prop != null && ConfigManager.Instance.IsActive(AccessType.AllowWorldPropPinning, leadPair.MasterID))
            {
                Transform propTarget = TWUtils.GetRootGameObject(prop.Spawnable.gameObject, "TWLPropAnchor");
                if (propTarget == null)
                    propTarget = prop.Spawnable.transform;

                followerController.targetOverride = propTarget;
            }

            if (leadPair.LeashPinPosition != Vector3.zero && ConfigManager.Instance.IsActive(AccessType.AllowWorldPropPinning, leadPair.MasterID))
                followerController.targetOverrideVector = leadPair.LeashPinPosition;

            //Ensure we set temp unlock so it gets enforced on avatar changes/resets
            followerController.SetTempUnlockLeash(leadPair.TempUnlockLeash);

            leadPair.LineController = followerController;
            
            if (leadPair.AreWeMaster() && !PetPairs.Contains(leadPair))
            {
                PetPairs.Add(leadPair);

                if (MasterPair != null && Equals(leadPair.Pet, MasterPair.Master))
                    TugOfWarPair = leadPair;
            }

            if (!leadPair.AreWeFollower()) return;

            var inversePair = PetPairs.FirstOrDefault(x => Equals(x.Pet, leadPair.Master) && Equals(x.Master, leadPair.Pet));
            if (inversePair != null)
                TugOfWarPair = inversePair;

            petBone.gameObject.layer = LayerMask.NameToLayer("PlayerNetwork");
            MasterPair = leadPair;

            if(!leadPair.InitialPairCreationComplete)
                OnFollowerPairCreated?.Invoke(leadPair);

            leadPair.InitialPairCreationComplete = true;

            AvatarParameterManager.Instance.TrySetParameter("TWCollar", 1);

            ApplyForcedMute(leadPair.ForcedMute);
        }

        #endregion
    }
}