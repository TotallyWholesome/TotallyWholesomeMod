using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using cohtml;
using TotallyWholesome.Network;
using TotallyWholesome.TWUI;
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

        public string ManagerName() => "StatusManager";
        public int Priority() => 1;
        public void Setup()
        {
            Instance = this;
            
            _statusComponents = new Dictionary<string, StatusComponent>();
            _knownStatuses = new Dictionary<string, StatusUpdate>();

            ButtplugManager.Instance.ButtplugDeviceRemoved += DeviceChangeStatusUpdate;
            ButtplugManager.Instance.ButtplugDeviceAdded += DeviceChangeStatusUpdate;
            PiShockManager.Instance.PiShockDeviceUpdated += DeviceChangeStatusUpdate;
            
            Patches.OnNameplateRebuild += OnNameplateRebuild;
            Patches.OnWorldLeave += OnWorldLeave;
            Patches.UserLeave += OnPlayerLeave;
            Patches.OnWorldJoin += OnInstanceJoin;
            TWNetClient.OnTWNetAuthenticated += OnTWNetAuthenticated;
        }
        
        public void LateSetup(){}

        [UIEventHandler("enterRankKey")]
        public static void EnterRankKey()
        {
            TWUtils.OpenKeyboard(Configuration.JSONConfig.LoginKey, s =>
            {
                Configuration.JSONConfig.LoginKey = s;
                Configuration.SaveConfig();
                
                Con.Debug($"LoginKey updated to {s}");
            });    
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
        
        
        private void OnInstanceJoin(RichPresenceInstance_t richPresenceInstanceT)
        {
            var publicWorld = richPresenceInstanceT.InstancePrivacy.Equals("public", StringComparison.InvariantCultureIgnoreCase) || richPresenceInstanceT.InstancePrivacy.Equals("friendsoffriends", StringComparison.InvariantCultureIgnoreCase);

            if (publicWorld == _isPublicWorld) return;

            _isPublicWorld = publicWorld;
            
            if(Configuration.JSONConfig.HideInPublicWorlds)
                SendStatusUpdate();
        }

        private void DeviceChangeStatusUpdate()
        {
            if(Configuration.JSONConfig.ShowDeviceStatus && Configuration.JSONConfig.EnableStatus && !(_isPublicWorld && Configuration.JSONConfig.HideInPublicWorlds))
                SendStatusUpdate(true);
        }

        public void SendStatusUpdate(bool fromDevice = false)
        {
            StatusUpdate update = new StatusUpdate();
            update.EnableStatus = Configuration.JSONConfig.EnableStatus && !(_isPublicWorld && Configuration.JSONConfig.HideInPublicWorlds);

            if (update.EnableStatus)
            {
                update.IsLookingForGroup = (ConfigManager.Instance.IsActive(AccessType.AutoAcceptPetRequest) || ConfigManager.Instance.IsActive(AccessType.AutoAcceptMasterRequest)) && !ConfigManager.Instance.IsActive(AccessType.AutoAcceptFriendsOnly) && Configuration.JSONConfig.ShowAutoAccept;
                update.DisplaySpecialRank = Configuration.JSONConfig.DisplaySpecialStatus;
                update.MasterAutoAccept = ConfigManager.Instance.IsActive(AccessType.AutoAcceptMasterRequest) && !ConfigManager.Instance.IsActive(AccessType.AutoAcceptFriendsOnly) && Configuration.JSONConfig.ShowAutoAccept;
                update.PetAutoAccept = ConfigManager.Instance.IsActive(AccessType.AutoAcceptPetRequest) && !ConfigManager.Instance.IsActive(AccessType.AutoAcceptFriendsOnly) && Configuration.JSONConfig.ShowAutoAccept;
                update.PiShockDevice = Configuration.JSONConfig.PiShockShockers.Any(x => x.Enabled) && Configuration.JSONConfig.ShowDeviceStatus;
                if(ButtplugManager.Instance.buttplugClient != null)
                    update.ButtplugDevice = ButtplugManager.Instance.buttplugClient.Devices.Length > 0 && Configuration.JSONConfig.ShowDeviceStatus;
            }

            _nextUpdatePacket = update;

            if (!fromDevice)
            {
                TWNetClient.Instance.Send(_nextUpdatePacket, TWNetMessageTypes.StatusUpdate);
                return;
            }
            
            //If update is coming from a device then we limit it's speed
            if (_statusUpdateTask != null && !_statusUpdateTask.IsCompleted)
                return;

            _statusUpdateTask = Task.Run(() =>
            {
                var timeBetweenLast = DateTime.Now.Subtract(_lastStatusUpdate).Milliseconds;

                //Ensure MasterRemoteSettings waits before being sent
                if (timeBetweenLast <= 5000)
                    Thread.Sleep(50 - timeBetweenLast);

                _lastStatusUpdate = DateTime.Now;
                
                TWNetClient.Instance.Send(_nextUpdatePacket, TWNetMessageTypes.StatusUpdate);
            });
        }

        public void OnStatusUpdate(StatusUpdate packet)
        {
            if (packet.UserID == MetaPort.Instance.ownerId)
            {
                _ourLastStatusUpdate = packet;
                
                Main.Instance.MainThreadQueue.Enqueue(() => {
                    if (!TWUtils.IsQMReady()) return;
                    UpdateQuickMenuStatus();
                });

                return;
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
#endif

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

                if (ColorUtility.TryParseHtmlString(packet.SpecialRankColour, out var colour))
                    component.specialMark.color = colour;
                if (ColorUtility.TryParseHtmlString(packet.SpecialRankTextColour, out var colour2))
                    component.specialMarkText.color = colour2;
            });
        }

        public void UpdateQuickMenuStatus()
        {
            if (_ourLastStatusUpdate == null) return;
            
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twStatusUpdate", string.IsNullOrWhiteSpace(_ourLastStatusUpdate.SpecialRankColour) ? "#ffffff": _ourLastStatusUpdate.SpecialRankColour, string.IsNullOrWhiteSpace(_ourLastStatusUpdate.SpecialRankTextColour) ? "#ffffff": _ourLastStatusUpdate.SpecialRankTextColour, _ourLastStatusUpdate.SpecialRank, _ourLastStatusUpdate.DisplaySpecialRank, _ourLastStatusUpdate.PetAutoAccept, _ourLastStatusUpdate.MasterAutoAccept, _ourLastStatusUpdate.ButtplugDevice, _ourLastStatusUpdate.PiShockDevice);
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
        }
        
        private void OnPlayerLeave(CVRPlayerEntity obj)
        {
            _statusComponents.Remove(obj.PlayerDescriptor.ownerId);
            _knownStatuses.Remove(obj.PlayerDescriptor.ownerId);
        }
    }
}