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

namespace TotallyWholesome.Notification;

public class NotificationSystem : ITWManager
{
    public static NotificationSystem Instance;

    public static Sprite DefaultIcon;
    public static Color DefaultColour = new Color(0.1764f, 0.2549f, .3333f, 1f);
    public static bool UseCVRNotificationSystem;

    private static GameObject _hudContent;
    private static GameObject _notificationGO;
    private static RectTransform _notificationRect;
    private static NotificationController _controllerInstance;

    public int Priority => 0;

    public void Setup()
    {
        Instance = this;
    }

    public void LateSetup()
    {
        _hudContent = !MetaPort.Instance.isUsingVr ? PlayerSetup.Instance.desktopCamera.GetComponentInChildren<Canvas>().gameObject : PlayerSetup.Instance.vrCamera.GetComponentInChildren<Canvas>().gameObject;

        Con.Debug("Got hud canvas");

        var notificationTransform = _hudContent.transform.Find("Notification(Clone)");
        if (notificationTransform != null)
        {
            //Notification system already initialized
            _notificationGO = notificationTransform.gameObject;
            _controllerInstance = _notificationGO.GetComponent<NotificationController>();

            return;
        }

        //Create UIX settings enum
        //RegSettingsEnum("TotallyWholesome", "NotificationAlignment", new[] {("centerMiddle", "Middle Centered"), ("topCenter", "Top Centered"), ("topLeft", "Top Left"), ("topRight", "Top Right"), ("bottomCenter", "Bottom Centered"), ("bottomLeft", "Bottom Left"), ("bottomRight", "Bottom Right")});

        if (TWAssets.NotificationPrefab == null)
            throw new Exception("NotificationSystem failed to load, prefab missing!");

        //Instantiate prefab and let NotificationController setup!
        _notificationGO = Object.Instantiate(TWAssets.NotificationPrefab, _hudContent.transform);
        _controllerInstance = _notificationGO.AddComponent<NotificationController>();
        //Get the RectTransform for us to set the alignment
        _notificationRect = _notificationGO.GetComponent<RectTransform>();

        _notificationRect.localPosition = MetaPort.Instance.isUsingVr ? new Vector3(-3, 0, 0) : Vector3.zero;

        UpdateNotificationAlignment();

        _controllerInstance.defaultSprite = DefaultIcon;
    }

    public void VRModeSwitched()
    {
        if(_controllerInstance == null)
        {
            return;
        }

        if(MetaPort.Instance.isUsingVr)
            _hudContent = PlayerSetup.Instance.vrCamera.GetComponentInChildren<Canvas>().gameObject;
        else
            _hudContent = PlayerSetup.Instance.desktopCamera.GetComponentInChildren<Canvas>().gameObject;

        _notificationGO.transform.parent = _hudContent.transform;

        _notificationRect.localPosition = MetaPort.Instance.isUsingVr ? new Vector3(-3, 0, 0) : Vector3.zero;
        _notificationRect.localRotation = Quaternion.identity;
    }

    public static void EnqueueAchievement(string description, Sprite icon = null)
    {
        var notif = new NotificationObject("Achievement Unlocked!", description, icon, 5f, Color.blue, true);

        if(_controllerInstance == null)
            return;

        _controllerInstance.EnqueueNotification(notif);
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

        if (_controllerInstance == null)
            return;

        _controllerInstance.EnqueueNotification(notif);
    }

    public static void ClearNotification()
    {
        if(_controllerInstance == null)
            return;

        _controllerInstance.ClearNotifications();
    }

    public static void CloseNotification()
    {
        if (_controllerInstance == null)
            return;

        _controllerInstance.ClearNotification();
    }

    public static void UpdateNotificationAlignment()
    {
        if (_notificationRect == null) return;

        if (Configuration.JSONConfig.NotificationCustomPlacement)
        {
            _notificationRect.localPosition = new Vector3(Configuration.JSONConfig.NotificationX, Configuration.JSONConfig.NotificationY);
            return;
        }

        switch (Configuration.JSONConfig.NotificationAlignment)
        {
            case NotificationAlignment.CenterMiddle:
                _notificationRect.anchorMin = new Vector2(0.5f, 0.5f);
                _notificationRect.anchorMax = new Vector2(0.5f, 0.5f);
                _notificationRect.pivot = new Vector2(0.5f, 0.5f);
                break;
            case NotificationAlignment.TopCenter:
                _notificationRect.anchorMin = new Vector2(0.5f, 1f);
                _notificationRect.anchorMax = new Vector2(0.5f, 1f);
                _notificationRect.pivot = new Vector2(0.5f, 1f);
                break;
            case NotificationAlignment.TopLeft:
                _notificationRect.anchorMin = new Vector2(0f, 1f);
                _notificationRect.anchorMax = new Vector2(0f, 1f);
                _notificationRect.pivot = new Vector2(0f, 1f);
                break;
            case NotificationAlignment.TopRight:
                _notificationRect.anchorMin = new Vector2(1f, 1f);
                _notificationRect.anchorMax = new Vector2(1f, 1f);
                _notificationRect.pivot = new Vector2(1f, 1f);
                break;
            case NotificationAlignment.BottomCenter:
                _notificationRect.anchorMin = new Vector2(0.5f, 0f);
                _notificationRect.anchorMax = new Vector2(0.5f, 0f);
                _notificationRect.pivot = new Vector2(0.5f, 0f);
                break;
            case NotificationAlignment.BottomLeft:
                _notificationRect.anchorMin = new Vector2(0f, 0f);
                _notificationRect.anchorMax = new Vector2(0f, 0f);
                _notificationRect.pivot = new Vector2(0f, 0f);
                break;
            case NotificationAlignment.BottomRight:
                _notificationRect.anchorMin = new Vector2(1f, 0f);
                _notificationRect.anchorMax = new Vector2(1f, 0f);
                _notificationRect.pivot = new Vector2(1f, 0f);
                break;
        }
    }
}

public enum NotificationAlignment
{
    CenterMiddle,
    TopCenter,
    TopLeft,
    TopRight,
    BottomCenter,
    BottomLeft,
    BottomRight
}