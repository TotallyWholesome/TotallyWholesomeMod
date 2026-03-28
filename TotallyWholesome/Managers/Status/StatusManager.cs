using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.UI.UILib;
using TMPro;
using TotallyWholesome.Managers.ModCompatibility.CompatbilityReflections;
using TotallyWholesome.Managers.Shockers.OpenShock.Config;
using TotallyWholesome.Managers.Shockers.PiShock.Config;
using TotallyWholesome.Managers.TWUI;
using TotallyWholesome.Network;
using TotallyWholesome.Utils;
using TWNetCommon;
using TWNetCommon.Data;
using UnityEngine;
using UnityEngine.UI;
using WholesomeLoader;
using Yggdrasil.Logging;
using Object = UnityEngine.Object;

namespace TotallyWholesome.Managers.Status
{
    public class StatusManager : ITWManager
    {
        public static StatusManager Instance;
        
        private Dictionary<string, StatusComponent> _statusComponents;
        private Dictionary<string, StatusUpdate> _knownStatuses;

        private bool _isPublicWorld;
        private bool _localUserStatusGenerated;
        private StatusComponent _localUserStatusComp;
        private StatusUpdate _localUserStatusUpdate;
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
            
            Patches.OnOverheadControllerStart += OnOverheadControllerStart;
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
            
            StatusComponent targetComponent = null;
            
            //Add or Update KnownStatuses
            if (packet.UserID != MetaPort.Instance.ownerId)
            {
                _knownStatuses[packet.UserID] = packet;
                if (!_statusComponents.TryGetValue(packet.UserID, out targetComponent)) return;
            }
            else
            {
                _localUserStatusUpdate = packet;
                if (!_localUserStatusGenerated) return;
                targetComponent = _localUserStatusComp;
            }

            if (ConfigManager.Instance.IsActive(AccessType.HideNameplateBadges)) return;

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (targetComponent == null) return;
                if (targetComponent.gameObject == null) return;


                if (!packet.EnableStatus)
                {
                    targetComponent.ResetStatus();
                    return;
                }


                targetComponent.specialMark.gameObject.SetActive(packet.DisplaySpecialRank); //Controlled by server
                targetComponent.specialMarkText.text = packet.SpecialRank;


                //Status will be shown and updated
                targetComponent.gameObject.SetActive(true);
                targetComponent.StatusEnabled = true;
                if (packet.IsLookingForGroup && !packet.PetAutoAccept && !packet.MasterAutoAccept)
                {
                    //Old client, display single colour mode
                    targetComponent.UpdateAutoAcceptGroup(false,false,true, false);
                }
                else
                {
                    //Updated client, show complete status indicator
                    targetComponent.UpdateAutoAcceptGroup(packet.PiShockDevice, packet.ButtplugDevice, packet.PetAutoAccept, packet.MasterAutoAccept);
                }

                //Enable beta icon if build is release-beta
                #if BETA
                targetComponent.backgroundImage.sprite = packet.ActiveBetaUser ? TWAssets.TWTagBetaIcon : TWAssets.TWTagNormalIcon;
                

                if (ColorUtility.TryParseHtmlString(packet.SpecialRankColour, out var colour))
                    targetComponent.specialMark.color = colour;
                if (ColorUtility.TryParseHtmlString(packet.SpecialRankTextColour, out var colour2))
                    targetComponent.specialMarkText.color = colour2;
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
        
        private void OnOverheadControllerStart(OverheadController controller, List<IOverhead> overheads)
        {
            if (controller.playerBase == null) return;
            if (controller.playerBase.gameObject == null) return;
            var player = controller.playerBase;
            var userID = player.playerDescriptor.ownerId;
            
            Con.Debug($"OverheadController start fired, creating TWStatus object for {userID} ({player.playerDescriptor.userName})");
            
            if ((_statusComponents.ContainsKey(userID) && _statusComponents[userID] != null) || player.IsLocalPlayer && _localUserStatusGenerated) return;
            
            _statusComponents.Remove(userID);

            GameObject newStatus = Object.Instantiate(TWAssets.StatusPrefab, controller.canvas.transform);
            RectTransform rectTransform = newStatus.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(1, 0);
            rectTransform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

            StatusComponent component = newStatus.AddComponent<StatusComponent>();
            component.SetupStatus(newStatus);
            component.ResetStatus();
            
            //Register as overhead
            overheads.Add(component);

            if (player.IsLocalPlayer)
            {
                Con.Debug("Setting local user status material properties");
                SetLocalUserMaterialProperties(newStatus);
                component.IsLocalUser = true;
                _localUserStatusGenerated = true;
                _localUserStatusComp = component;
                if(_localUserStatusUpdate != null)
                    OnStatusUpdate(_localUserStatusUpdate);
            }
            else
            {
                _statusComponents.Add(userID, component);
                if (!_knownStatuses.ContainsKey(userID)) return;
                OnStatusUpdate(_knownStatuses[userID]);
            }
        }

        private void SetLocalUserMaterialProperties(GameObject newStatus)
        {
            //Get all components with things we need to touch
            var images = newStatus.GetComponentsInChildren<Image>(true);
            var tmpTexts = newStatus.GetComponentsInChildren<TMP_Text>(true);
            
            var fadeStart = Shader.PropertyToID("_FadeStartDistance");
            var fadeEnd = Shader.PropertyToID("_FadeEndDistance");
            var firstPersonLocalNameplateScaleVr = Shader.PropertyToID("_FirstPersonLocalNameplateScaleVr");
            var firstPersonLocalNameplateScaleDesktop = Shader.PropertyToID("_FirstPersonLocalNameplateScaleDesktop");
            var isLocalPlayer = Shader.PropertyToID("_IsLocalPlayer");

            newStatus.layer = 8;
            
            //Set all gameobjects to PlayerLocal layer
            var children = newStatus.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var child in children)
            {
                child.gameObject.layer = 8;
            }

            if (tmpTexts.Length > 0)
            {
                var textMeshMat = new Material(tmpTexts[0].fontSharedMaterial);
                textMeshMat.SetFloat(fadeStart, PlayerNameplate.LocalPlayerFadeStart);
                textMeshMat.SetFloat(fadeEnd, PlayerNameplate.LocalPlayerFadeEnd);
                textMeshMat.SetFloat(firstPersonLocalNameplateScaleVr, PlayerNameplate.FirstPersonLocalScaleVr);
                textMeshMat.SetFloat(firstPersonLocalNameplateScaleDesktop, PlayerNameplate.FirstPersonLocalScaleDesktop);
                textMeshMat.SetFloat(isLocalPlayer, 1f);
                
                foreach(var tmpText in tmpTexts)
                    tmpText.fontSharedMaterial = textMeshMat;
            }

            if (images.Length > 0)
            {
                var imageMat = new Material(images.FirstOrDefault(x => x.material.shader.name == "TotallyWholesome/NameplateStatusBillboard")?.material);
                var imageMaskMat = new Material(images.FirstOrDefault(x => x.material.shader.name == "TotallyWholesome/NameplateStatusBillboardMask")?.material);
                if (imageMat == null || imageMaskMat == null)
                {
                    Con.Error("There was no images with valid shader in the TWStatus prefab? How?");
                    return;
                }
                    
                imageMat.SetFloat(fadeStart, PlayerNameplate.LocalPlayerFadeStart);
                imageMat.SetFloat(fadeEnd, PlayerNameplate.LocalPlayerFadeEnd);
                imageMat.SetFloat(firstPersonLocalNameplateScaleVr, PlayerNameplate.FirstPersonLocalScaleVr);
                imageMat.SetFloat(firstPersonLocalNameplateScaleDesktop, PlayerNameplate.FirstPersonLocalScaleDesktop);
                imageMat.SetFloat(isLocalPlayer, 1f);
                
                imageMaskMat.SetFloat(fadeStart, PlayerNameplate.LocalPlayerFadeStart);
                imageMaskMat.SetFloat(fadeEnd, PlayerNameplate.LocalPlayerFadeEnd);
                imageMaskMat.SetFloat(firstPersonLocalNameplateScaleVr, PlayerNameplate.FirstPersonLocalScaleVr);
                imageMaskMat.SetFloat(firstPersonLocalNameplateScaleDesktop, PlayerNameplate.FirstPersonLocalScaleDesktop);
                imageMaskMat.SetFloat(isLocalPlayer, 1f);

                foreach (var image in images) 
                    image.material = image.material.shader.name == "TotallyWholesome/NameplateStatusBillboardMask" ? imageMaskMat : imageMat;
                
            }
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