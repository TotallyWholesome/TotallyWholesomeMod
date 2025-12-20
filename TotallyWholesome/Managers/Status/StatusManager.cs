using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.UI.UILib;
using TotallyWholesome.Managers.ModCompatibility.CompatbilityReflections;
using TotallyWholesome.Managers.Shockers.OpenShock.Config;
using TotallyWholesome.Managers.Shockers.PiShock.Config;
using TotallyWholesome.Managers.TWUI;
using TotallyWholesome.Network;
using TotallyWholesome.Utils;
using TWNetCommon;
using TWNetCommon.Data;
using UnityEngine;
using WholesomeLoader;
using Object = UnityEngine.Object;

namespace TotallyWholesome.Managers.Status
{
    public class StatusManager : ITWManager
    {
        public static StatusManager Instance;
        
        private Dictionary<string, StatusComponent> _statusComponents;
        private Dictionary<string, StatusUpdate> _knownStatuses;

        private bool _isPublicWorld;
        private StatusUpdate _ourLastStatusUpdate;
        private DateTime _lastStatusUpdate;
        private Task _statusUpdateTask;
        private StatusUpdate _nextUpdatePacket;

        public int Priority => 1;

        public void Setup()
        {
            Instance = this;
            
            _statusComponents = new Dictionary<string, StatusComponent>();
            _knownStatuses = new Dictionary<string, StatusUpdate>();

            ButtplugManager.Instance.ButtplugDeviceRemoved += DeviceChangeStatusUpdate;
            ButtplugManager.Instance.ButtplugDeviceAdded += DeviceChangeStatusUpdate;
            //PiShockManager.Instance.PiShockDeviceUpdated += DeviceChangeStatusUpdate;
            
            Patches.OnNameplateRebuild += OnNameplateRebuild;
            Patches.OnWorldLeave += OnWorldLeave;
            Patches.UserLeave += OnPlayerLeave;
            Patches.OnWorldJoin += OnInstanceJoin;
            TWNetClient.OnTWNetAuthenticated += OnTWNetAuthenticated;
            QuickMenuAPI.OnMenuGenerated += _ =>
            {
                Con.Debug("MenuGenerated fired");
                UpdateQuickMenuStatus();
            };
        }

        public void LateSetup()
        {
        }

        public void SetTWBadgeHideStatus(bool state)
        {
            if (state)
            {
                foreach (var component in _statusComponents)
                {
                    component.Value.ResetStatus();
                }
            }
            else
            {
                foreach (var status in _knownStatuses.ToArray())
                {
                    //Reapply the latest known status of a user
                    OnStatusUpdate(status.Value);
                }
            }
        }
        
        
        private void OnInstanceJoin()
        {
            var publicWorld = !Instances.IsInPrivateInstance();

            if (publicWorld == _isPublicWorld) return;

            _isPublicWorld = publicWorld;
            
            if(Configuration.JSONConfig.HideInPublicWorlds)
                SendStatusUpdate();
        }

        /// <summary>
        ///  Device update received from shockers or buttplugio
        /// </summary>
        public void DeviceChangeStatusUpdate()
        {
            if(Configuration.JSONConfig.ShowDeviceStatus && Configuration.JSONConfig.EnableStatus && !(_isPublicWorld && Configuration.JSONConfig.HideInPublicWorlds))
                SendStatusUpdate(true);
        }

        public void SendStatusUpdate(bool fromDevice = false)
        {
            var update = new StatusUpdate
            {
                EnableStatus = Configuration.JSONConfig.EnableStatus && !(_isPublicWorld && Configuration.JSONConfig.HideInPublicWorlds)
            };

            if (update.EnableStatus)
            {
                update.IsLookingForGroup = (ConfigManager.Instance.IsActive(AccessType.AutoAcceptPetRequest) || ConfigManager.Instance.IsActive(AccessType.AutoAcceptMasterRequest)) && !ConfigManager.Instance.IsActive(AccessType.AutoAcceptFriendsOnly) && Configuration.JSONConfig.ShowAutoAccept;
                update.DisplaySpecialRank = Configuration.JSONConfig.DisplaySpecialStatus;
                update.MasterAutoAccept = ConfigManager.Instance.IsActive(AccessType.AutoAcceptMasterRequest) && !ConfigManager.Instance.IsActive(AccessType.AutoAcceptFriendsOnly) && Configuration.JSONConfig.ShowAutoAccept;
                update.PetAutoAccept = ConfigManager.Instance.IsActive(AccessType.AutoAcceptPetRequest) && !ConfigManager.Instance.IsActive(AccessType.AutoAcceptFriendsOnly) && Configuration.JSONConfig.ShowAutoAccept;
                update.PiShockDevice = (OpenShockConfig.Config.Shockers.Count > 0 || PiShockConfig.Config.Shockers.Count > 0) && Configuration.JSONConfig.ShowDeviceStatus;
                if(ButtplugManager.Instance.buttplugClient != null)
                    update.ButtplugDevice = ButtplugManager.Instance.buttplugClient.Devices.Length > 0 && Configuration.JSONConfig.ShowDeviceStatus;
            }

            _nextUpdatePacket = update;

            // Send instantly if not from a device
            if (!fromDevice)
            {
                TwTask.Run(TWNetClient.Instance.SendAsync(_nextUpdatePacket, TWNetMessageType.StatusUpdate));
                return;
            }
            
            //If update is coming from a device then we limit it's speed
            if (_statusUpdateTask is { IsCompleted: false })
                return;

            _statusUpdateTask = TwTask.Run(async () =>
            {
                var timeBetweenLast = DateTime.UtcNow.Subtract(_lastStatusUpdate).Milliseconds;
                var timeToWait = 50 - timeBetweenLast;

                // Only wait if we actually have to wait for more than 0ms
                if (timeToWait > 0)
                    await Task.Delay(timeToWait);

                _lastStatusUpdate = DateTime.UtcNow;
                
#pragma warning disable CS4014 // Dont wait, since we we use this task for rate limiting
                TwTask.Run(TWNetClient.Instance.SendAsync(_nextUpdatePacket, TWNetMessageType.StatusUpdate));
#pragma warning restore CS4014
            });
        }

        public void OnStatusUpdate(StatusUpdate packet)
        {
            if (packet.UserID == MetaPort.Instance.ownerId)
            {
                _ourLastStatusUpdate = packet;
                
                Main.Instance.MainThreadQueue.Enqueue(() => {
                    if (!CVR_MenuManager.IsReadyStatic) return;
                    UpdateQuickMenuStatus();
                });
            }

            if (packet.UserID == null) return;
            
            //Add or Update KnownStatuses
            _knownStatuses[packet.UserID] = packet;
            if (!_statusComponents.ContainsKey(packet.UserID)) return;
            
            if (ConfigManager.Instance.IsActive(AccessType.HideNameplateBadges)) return;

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (!_statusComponents.ContainsKey(packet.UserID))
                {
                    Con.Error($"{packet.UserID} does not exist in the statusComponents list!");
                    return;
                }
                
                var component = _statusComponents[packet.UserID];

                if (component == null) return;
                if (component.gameObject == null) return;


                if (!packet.EnableStatus)
                {
                    component.ResetStatus();
                    return;
                }


                component.specialMark.gameObject.SetActive(packet.DisplaySpecialRank); //Controlled by server
                component.specialMarkText.text = packet.SpecialRank;


                //Status will be shown and updated
                component.gameObject.SetActive(true);
                if (packet.IsLookingForGroup && !packet.PetAutoAccept && !packet.MasterAutoAccept)
                {
                    //Old client, display single colour mode
                    component.UpdateAutoAcceptGroup(false,false,true, false);
                }
                else
                {
                    //Updated client, show complete status indicator
                    component.UpdateAutoAcceptGroup(packet.PiShockDevice, packet.ButtplugDevice, packet.PetAutoAccept, packet.MasterAutoAccept);
                }

                //Enable beta icon if build is release-beta
                #if BETA
                component.backgroundImage.sprite = packet.ActiveBetaUser ? TWAssets.TWTagBetaIcon : TWAssets.TWTagNormalIcon;
                

                if (ColorUtility.TryParseHtmlString(packet.SpecialRankColour, out var colour))
                    component.specialMark.color = colour;
                if (ColorUtility.TryParseHtmlString(packet.SpecialRankTextColour, out var colour2))
                    component.specialMarkText.color = colour2;
            });
        }

        public void UpdateQuickMenuStatus()
        {
            if (_ourLastStatusUpdate == null) return;
            
            TWMenu.TWStatusUpdate.TriggerEvent(string.IsNullOrWhiteSpace(_ourLastStatusUpdate.SpecialRankColour) ? "#ffffff": _ourLastStatusUpdate.SpecialRankColour, string.IsNullOrWhiteSpace(_ourLastStatusUpdate.SpecialRankTextColour) ? "#ffffff": _ourLastStatusUpdate.SpecialRankTextColour, _ourLastStatusUpdate.SpecialRank, _ourLastStatusUpdate.DisplaySpecialRank, _ourLastStatusUpdate.PetAutoAccept, _ourLastStatusUpdate.MasterAutoAccept, _ourLastStatusUpdate.ButtplugDevice, _ourLastStatusUpdate.PiShockDevice);
        }

        public void UpdatePetMasterMark(string userID, bool pet, bool master)
        {
            if (userID == null)
                return;
            
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (!_statusComponents.ContainsKey(userID))
                    return;
                if (!_knownStatuses.ContainsKey(userID))
                    return;

                var component = _statusComponents[userID];
                var knownStatus = _knownStatuses[userID];

                if (!knownStatus.EnableStatus)
                    return;

                component.masterIndicator.SetActive(master);
                component.petIndicator.SetActive(pet);
            });
        }
        
        private void OnTWNetAuthenticated()
        {
            SendStatusUpdate();
        }

        internal void OnNameplateRebuild(PlayerNameplate nameplate)
        {
            if(!VRCPlatesAdapter.IsVRCPlatesEnabled())
                OnNameplateRebuild(nameplate.player, nameplate.transform);
        }

        //Setup the prefab on all nameplates, but call ResetStatus to hide the object
        internal void OnNameplateRebuild(PlayerDescriptor player, Transform nameplate)
        {
            if (player == null) return;
            if (player.gameObject == null) return;
            var userID = player.ownerId;
            
            if (_statusComponents.ContainsKey(userID) && _statusComponents[userID] != null) return;

            _statusComponents.Remove(userID);

            Transform parent = null;

            parent = VRCPlatesAdapter.IsVRCPlatesEnabled() ? nameplate : nameplate.transform.Find("Canvas");

            GameObject newStatus = Object.Instantiate(TWAssets.StatusPrefab, parent);
            RectTransform rectTransform = newStatus.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(1, 0);
            rectTransform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

            //Adjust positions and scales to fit VRCPlates
            if (VRCPlatesAdapter.IsVRCPlatesEnabled())
            {
                rectTransform.localPosition = new Vector3(550, 0, 0);
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            }

            StatusComponent component = newStatus.AddComponent<StatusComponent>();
            component.SetupStatus(newStatus);
            component.ResetStatus();
            
            _statusComponents.Add(userID, component);

            if (!_knownStatuses.ContainsKey(userID)) return;
            
            OnStatusUpdate(_knownStatuses[userID]);
        }

        private void OnWorldLeave()
        {
            _knownStatuses.Clear();
            _statusComponents.Clear();

            if (Configuration.JSONConfig.HideInPublicWorlds)
            {
                _isPublicWorld = true;
                SendStatusUpdate();
            }
        }
        
        private void OnPlayerLeave(CVRPlayerEntity obj)
        {
            _statusComponents.Remove(obj.PlayerDescriptor.ownerId);
            _knownStatuses.Remove(obj.PlayerDescriptor.ownerId);
        }
    }
}