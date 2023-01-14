using System;
using System.Collections;
using System.Collections.Generic;
using ABI_RC.Core.IO;
using ABI_RC.Core.UI;
using MelonLoader;
using TMPro;
using TotallyWholesome.Managers.ModCompatibility;
using UnityEngine;
using UnityEngine.UI;

namespace TotallyWholesome.Notification
{
    public class NotificationController : MonoBehaviour
    {
        public Sprite defaultSprite;
        
        //Objects
        private Animator _notificationAnimator;
        private Image _iconImage;
        private Image _backgroundImage;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _descriptionText;

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
            _notificationQueue.Clear();
            ClearNotification();
        }

        private void Start()
        {
            _notificationQueue = new Queue<NotificationObject>();
            
            _notificationAnimator = gameObject.GetComponent<Animator>();
            _backgroundImage = gameObject.transform.Find("Content/Background").GetComponent<Image>();
            _iconImage = gameObject.transform.Find("Content/Icon").gameObject.GetComponent<Image>();
            _titleText = gameObject.transform.Find("Content/Title").gameObject.GetComponent<TextMeshProUGUI>();
            _descriptionText = gameObject.transform.Find("Content/Description").gameObject.GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (_notificationQueue.Count <= 0 || _isDisplaying) return;

            _currentNotification = _notificationQueue.Dequeue();

            //Do not allow repeated messages within 30 seconds
            if (_currentNotification.Title.Equals(_titleText.text) && _currentNotification.Description.Equals(_descriptionText.text) && DateTime.Now.Subtract(_lastNotifTime).TotalSeconds < 30) return;

            if (NotificationSystem.UseCVRNotificationSystem)
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
            _titleText.text = _currentNotification.Title;
            _descriptionText.text = _currentNotification.Description;
            _iconImage.sprite = _currentNotification.Icon == null ? defaultSprite : _currentNotification.Icon;
            _iconImage.enabled = true;
            _currentNotification.BackgroundColor.a = NotificationSystem.NotificationAlpha.Value;
            _backgroundImage.color = _currentNotification.BackgroundColor;
            _titleText.faceColor = _white;
            _descriptionText.faceColor = _white;
            _titleText.color = Color.white;
            _descriptionText.color = Color.white;

            OpenNotification();
        }

        public void ClearNotification()
        {
            _currentNotification = null;
            CloseNotification();
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
            _notificationAnimator.Play("In");

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
            _notificationAnimator.Play("Out");
        }

        private IEnumerator StartTimer()
        {
            yield return new WaitForSeconds(_currentNotification.DisplayLength);
            CloseNotification();
        }
    }
}