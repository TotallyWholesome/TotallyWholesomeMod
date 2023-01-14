using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using MelonLoader;
using TotallyWholesome.Managers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using WholesomeLoader;
using Object = UnityEngine.Object;

namespace TotallyWholesome.Notification
{
    public class NotificationSystem
    {
        public static Sprite DefaultIcon;
        public static Color DefaultColour = new Color(0.1764f, 0.2549f, .3333f, 1f);
        public static MelonPreferences_Entry<float> NotificationAlpha;
        public static MelonPreferences_Entry<string> NotificationAlignment;
        public static MelonPreferences_Entry<bool> NotificationCoordinateAlignment;
        public static MelonPreferences_Entry<float> NotificationX;
        public static MelonPreferences_Entry<float> NotificationY;
        public static bool UseCVRNotificationSystem;

        private static GameObject _hudContent;
        private static GameObject _notificationGO;
        private static RectTransform _notificationRect;
        private static NotificationController _controllerInstance;

        public static void SetupNotifications()
        {
            if (!XRDevice.isPresent)
                _hudContent = PlayerSetup.Instance.desktopCamera.GetComponentInChildren<Canvas>().gameObject;
            else
                _hudContent = PlayerSetup.Instance.vrCamera.GetComponentInChildren<Canvas>().gameObject;

            Con.Debug("Got hud canvas");

            var notificationTransform = _hudContent.transform.Find("Notification(Clone)");
            if (notificationTransform != null)
            {
                //Notification system already initialized
                _notificationGO = notificationTransform.gameObject;
                _controllerInstance = _notificationGO.GetComponent<NotificationController>();
                
                return;
            }

            MelonPreferences.CreateCategory("TotallyWholesome", "Totally Wholesome");
            NotificationAlpha = MelonPreferences.CreateEntry("TotallyWholesome", "NotificationAlpha", .7f, "Notification Alpha", "Controls transparency of the notification system.");
            NotificationAlignment = MelonPreferences.CreateEntry("TotallyWholesome", "NotificationAlignment", "centerMiddle", "Notification Alignment");
            NotificationCoordinateAlignment = MelonPreferences.CreateEntry("TotallyWholesome", "NotificationCoordinateAlignment", false, "Use Coordinate Alignment");
            NotificationX = MelonPreferences.CreateEntry("TotallyWholesome", "NotificationX", .5f, "Notification X", "Controls the X position of the notification system.");
            NotificationY = MelonPreferences.CreateEntry("TotallyWholesome", "NotificationY", .5f, "Notification Y", "Controls the Y position of the notification system.");

            NotificationAlignment.OnEntryValueChanged.Subscribe(UpdateNotificationAlignment);
            NotificationCoordinateAlignment.OnEntryValueChanged.Subscribe((_, _) => UpdateNotificationAlignment(null, null));
            NotificationX.OnEntryValueChanged.Subscribe((_, _) => UpdateNotificationAlignment(null, null));
            NotificationY.OnEntryValueChanged.Subscribe((_, _) => UpdateNotificationAlignment(null, null));
            
            //Create UIX settings enum
            //RegSettingsEnum("TotallyWholesome", "NotificationAlignment", new[] {("centerMiddle", "Middle Centered"), ("topCenter", "Top Centered"), ("topLeft", "Top Left"), ("topRight", "Top Right"), ("bottomCenter", "Bottom Centered"), ("bottomLeft", "Bottom Left"), ("bottomRight", "Bottom Right")});

            if (TWAssets.NotificationPrefab == null)
                throw new Exception("NotificationSystem failed to load, prefab missing!");

            //Instantiate prefab and let NotificationController setup!
            _notificationGO = Object.Instantiate(TWAssets.NotificationPrefab, _hudContent.transform);
            _controllerInstance = _notificationGO.AddComponent<NotificationController>();
            //Get the RectTransform for us to set the alignment
            _notificationRect = _notificationGO.GetComponent<RectTransform>();
            
            _notificationRect.localPosition = XRDevice.isPresent ? new Vector3(-3, 0, 0) : Vector3.zero;
            
            UpdateNotificationAlignment(null, null);

            _controllerInstance.defaultSprite = DefaultIcon;
        }

        public static void VRModeSwitched()
        {
            if(_controllerInstance == null)
            {
                SetupNotifications();
                return;
            }
            
            if(MetaPort.Instance.isUsingVr)
                _hudContent = PlayerSetup.Instance.vrCamera.GetComponentInChildren<Canvas>().gameObject;
            else
                _hudContent = PlayerSetup.Instance.desktopCamera.GetComponentInChildren<Canvas>().gameObject;

            _notificationGO.transform.parent = _hudContent.transform;
            
            _notificationRect.localPosition = MetaPort.Instance.isUsingVr ? new Vector3(-3, 0, 0) : Vector3.zero;
        }

        /// <summary>
        /// Enqueue a new notification
        /// </summary>
        /// <param name="title">Title shown in the top of the notification</param>
        /// <param name="description">Main description, scales based on size</param>
        /// <param name="displayLength">How long in seconds you want it shown</param>
        /// <param name="icon">Optional icon sprite, defaults to Megaphone</param>
        public static void EnqueueNotification(string title, string description, float displayLength = 5f, Sprite icon = null)
        {
            EnqueueNotification(title, description, DefaultColour, displayLength, icon);
        }

        /// <summary>
        /// Enqueue a new notification
        /// </summary>
        /// <param name="title">Title shown in the top of the notification</param>
        /// <param name="description">Main description, scales based on size</param>
        /// <param name="backgroundColour">Background colour of the notification</param>
        /// <param name="displayLength">How long in seconds you want it shown</param>
        /// <param name="icon">Optional icon sprite, defaults to Megaphone</param>
        public static void EnqueueNotification(string title, string description, Color backgroundColour, float displayLength = 5f, Sprite icon = null)
        {
            var notif = new NotificationObject(title, description, icon, displayLength, backgroundColour);
            
            if(_controllerInstance == null)
                SetupNotifications();
            
            _controllerInstance.EnqueueNotification(notif);
        }

        public static void ClearNotification()
        {
            if(_controllerInstance == null)
                SetupNotifications();
            
            _controllerInstance.ClearNotifications();
        }

        public static void CloseNotification()
        {
            if (_controllerInstance == null)
                SetupNotifications();

            _controllerInstance.ClearNotification();
        }

        private static void UpdateNotificationAlignment(string sender, string args)
        {
            if (_notificationRect == null) return;
            
            if (NotificationCoordinateAlignment.Value)
            {
                _notificationRect.anchorMin = new Vector2(NotificationX.Value, NotificationY.Value);
                _notificationRect.anchorMax = new Vector2(NotificationX.Value, NotificationY.Value);
                _notificationRect.pivot = new Vector2(NotificationX.Value, NotificationY.Value);
                return;
            }

            switch (NotificationAlignment.Value)
            {
                case "centerMiddle":
                    _notificationRect.anchorMin = new Vector2(0.5f, 0.5f);
                    _notificationRect.anchorMax = new Vector2(0.5f, 0.5f);
                    _notificationRect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "topCenter":
                    _notificationRect.anchorMin = new Vector2(0.5f, 1f);
                    _notificationRect.anchorMax = new Vector2(0.5f, 1f);
                    _notificationRect.pivot = new Vector2(0.5f, 1f);
                    break;
                case "topLeft":
                    _notificationRect.anchorMin = new Vector2(0f, 1f);
                    _notificationRect.anchorMax = new Vector2(0f, 1f);
                    _notificationRect.pivot = new Vector2(0f, 1f);
                    break;
                case "topRight":
                    _notificationRect.anchorMin = new Vector2(1f, 1f);
                    _notificationRect.anchorMax = new Vector2(1f, 1f);
                    _notificationRect.pivot = new Vector2(1f, 1f);
                    break;
                case "bottomCenter":
                    _notificationRect.anchorMin = new Vector2(0.5f, 0f);
                    _notificationRect.anchorMax = new Vector2(0.5f, 0f);
                    _notificationRect.pivot = new Vector2(0.5f, 0f);
                    break;
                case "bottomLeft":
                    _notificationRect.anchorMin = new Vector2(0f, 0f);
                    _notificationRect.anchorMax = new Vector2(0f, 0f);
                    _notificationRect.pivot = new Vector2(0f, 0f);
                    break;
                case "bottomRight":
                    _notificationRect.anchorMin = new Vector2(1f, 0f);
                    _notificationRect.anchorMax = new Vector2(1f, 0f);
                    _notificationRect.pivot = new Vector2(1f, 0f);
                    break;
            }
        }

        #region UIXAdapter
        
        private static bool? _uixAvailable;
        private static MethodInfo _regSettingEnum;
        private static bool _methodsGetRan;

        private static bool IsUIXAvailable()
        {
            _uixAvailable ??= MelonMod.RegisteredMelons.Any(x => x.Info.Name.Equals("UI Expansion Kit"));
            return _uixAvailable.Value;
        }

        private static bool GetUIXMethods()
        {
            if (_methodsGetRan) return true;

            Type expandedMenu = Type.GetType("UIExpansionKit.API.ExpansionKitApi, UIExpansionKit");

            if (expandedMenu == null) return false;
            
            _regSettingEnum = expandedMenu.GetMethod("RegisterSettingAsStringEnum", BindingFlags.Public | BindingFlags.Static);
            
            _methodsGetRan = true;

            return true;
        }

        private static bool RegSettingsEnum(string settingsCat, string settingsName, IList<(string value, string desc)> values)
        {
            if (!IsUIXAvailable()) return false;
            if (!GetUIXMethods()) return false;

            _regSettingEnum.Invoke(null, new object[] { settingsCat, settingsName, values });

            return true;
        }
        
        #endregion
    }
}
