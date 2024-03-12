using System;
using System.Collections;
using System.Collections.Generic;
using ABI_RC.Core.IO;
using ABI_RC.Core.UI;
using MelonLoader;
using TMPro;
using TotallyWholesome.Managers.ModCompatibility;
using TotallyWholesome.Managers.ModCompatibility.CompatbilityReflections;
using UnityEngine;
using UnityEngine.UI;
using WholesomeLoader;

namespace TotallyWholesome.Notification
{
    public class NotificationController : MonoBehaviour
    {
        public Sprite defaultSprite;
        
        //Objects
        private Animator _notificationAnimator;
        private Animator _achievementAnimator;
        private Image _iconImage;
        private Image _iconAchievement;
        private Image _backgroundImage;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _descriptionText;
        private TextMeshProUGUI _descriptionTextAchievement;
        private AudioSource _achievementJingle;
        
        private Queue<NotificationObject> _notificationQueue;
        private bool _isDisplaying;
        private object _timerToken;
        private DateTime _lastNotifTime = DateTime.Now;
        private Color32 _white = new(255, 255, 255, 255);

        //Current NotificationObject details
        private NotificationObject _currentNotification;

        public void EnqueueNotification(NotificationObject notif)
        {
            _notificationQueue.Enqueue(notif);
        }

        public void ClearNotifications()
        {
            try
            {
                _notificationQueue.Clear();
                ClearNotification();
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }

        private void Awake()
        {
            _notificationQueue = new Queue<NotificationObject>();
            
            _notificationAnimator = gameObject.transform.Find("Notification").GetComponent<Animator>();
            _backgroundImage = gameObject.transform.Find("Notification/Content/Background").GetComponent<Image>();
            _iconImage = gameObject.transform.Find("Notification/Content/Icon").gameObject.GetComponent<Image>();
            _titleText = gameObject.transform.Find("Notification/Content/Title").gameObject.GetComponent<TextMeshProUGUI>();
            _descriptionText = gameObject.transform.Find("Notification/Content/Description").gameObject.GetComponent<TextMeshProUGUI>();
            _iconAchievement = gameObject.transform.Find("Achievement/Content/Icon").gameObject.GetComponent<Image>();
            _descriptionTextAchievement = gameObject.transform.Find("Achievement/Content/Description").gameObject.GetComponent<TextMeshProUGUI>();
            _achievementAnimator = gameObject.transform.Find("Achievement").GetComponent<Animator>();
            _achievementJingle = gameObject.transform.Find("AchievementJingle").GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (_notificationQueue.Count <= 0 || _isDisplaying) return;

            _currentNotification = _notificationQueue.Dequeue();

            //Do not allow repeated messages within 30 seconds
            if (_currentNotification.Title.Equals(_titleText.text) && _currentNotification.Description.Equals(_descriptionText.text) && DateTime.Now.Subtract(_lastNotifTime).TotalSeconds < 30) return;

            if (NotificationSystem.UseCVRNotificationSystem && !_currentNotification.UseAchievementPopup)
            {
                //Using CVR HUD Messages
                if(CohtmlHud.Instance != null && !NotificationAPIAdapter.IsNotifAPIAvailable())
                    CohtmlHud.Instance.ViewDropTextImmediate("Totally Wholesome", _currentNotification.Title, _currentNotification.Description);
                if(NotificationAPIAdapter.IsNotifAPIAvailable())
                    NotificationAPIAdapter.Notify($"[{_currentNotification.Title}] {_currentNotification.Description}", 2);
                return;
            }
            
            _lastNotifTime = DateTime.Now;

            //Update UI
            if (!_currentNotification.UseAchievementPopup)
            {
                _titleText.text = _currentNotification.Title;
                _descriptionText.text = _currentNotification.Description;
                _iconImage.sprite = _currentNotification.Icon == null ? defaultSprite : _currentNotification.Icon;
                _iconImage.enabled = true;
                _currentNotification.BackgroundColor.a = Configuration.JSONConfig.NotificationAlpha;
                _backgroundImage.color = _currentNotification.BackgroundColor;
                _titleText.faceColor = _white;
                _descriptionText.faceColor = _white;
                _titleText.color = Color.white;
                _descriptionText.color = Color.white;
            }
            else
            {
                _iconAchievement.sprite = _currentNotification.Icon == null ? defaultSprite : _currentNotification.Icon;
                _descriptionTextAchievement.text = _currentNotification.Description;
                _achievementJingle.Play();
            }

            OpenNotification();
        }

        public void ClearNotification()
        {
            CloseNotification();
            _currentNotification = null;
        }

        private void OpenNotification()
        {
            _isDisplaying = true;
            if (_timerToken != null)
            {
                MelonCoroutines.Stop(_timerToken);
                _timerToken = null;
            }
                
            //Play slide in animation
            if(!_currentNotification.UseAchievementPopup)
                _notificationAnimator.Play("In");
            else
                _achievementAnimator.Play("In");

            //Start notification timer
            _timerToken = MelonCoroutines.Start(StartTimer());
        }

        private void CloseNotification()
        {
            if (!_isDisplaying) return;
            
            if (_timerToken != null)
            {
                MelonCoroutines.Stop(_timerToken);
                _timerToken = null;
            }

            _isDisplaying = false;
            //Play slide out
            if(!_currentNotification.UseAchievementPopup)
                _notificationAnimator.Play("Out");
            else
                _achievementAnimator.Play("Out");
        }

        private IEnumerator StartTimer()
        {
            yield return new WaitForSeconds(_currentNotification.DisplayLength);
            CloseNotification();
        }
    }
}