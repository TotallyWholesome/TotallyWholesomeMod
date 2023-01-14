using System;
using System.Collections;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Savior;
using TotallyWholesome.Notification;
using UnityEngine;

namespace TotallyWholesome
{
    public class TWRaycaster : MonoBehaviour
    {
        public static TWRaycaster Instance;
    
        public LineRenderer targetLine;
        public MeshRenderer targetMark;
        public float maxDistance;
        public Action<Vector3> OnSelect;
        
        private Vector3 _lastRaycastTarget;
        private bool _noticeCooldown;
        private int _layerMask;

        private void Start()
        {
            Instance = this;
            _layerMask = LayerMask.GetMask("Default", "CVRPickup", "CVRInteractable");
        }

        // Update is called once per frame
        void Update()
        {
            RaycastHit hit;
            var o = gameObject.transform;
            var targetTransform = targetMark.transform;
            var colour = Color.red;
        
            targetLine.SetPosition(0, o.position);
            if (Physics.Raycast(o.position, o.forward, out hit, maxDistance, _layerMask))
            {
                targetLine.SetPosition(1, hit.point);
                targetTransform.position = hit.point;
                targetTransform.up = hit.normal;
                colour = Color.green;
                _lastRaycastTarget = hit.point;
            }
            else
            {
                var newPos = o.position + o.forward * maxDistance;
                targetLine.SetPosition(1, newPos);
                targetTransform.position = newPos;
                targetTransform.up = o.forward;
                _lastRaycastTarget = Vector3.zero;
            }
        
            targetLine.endColor = colour;
            targetLine.startColor = colour;
            targetMark.material.color = colour;
        }

        public void StartRaycaster(Action<Vector3> onSelect)
        {
            OnSelect = onSelect;
            gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if(ViewManager.Instance.isGameMenuOpen()) return;

            if (CVRInputManager.Instance.interactRightDown && _lastRaycastTarget != Vector3.zero)
            {
                gameObject.SetActive(false);
                OnSelect?.Invoke(_lastRaycastTarget);
                return;
            }

            if (CVRInputManager.Instance.interactRightDown && _lastRaycastTarget == Vector3.zero)
            {
                if(!_noticeCooldown)
                    StartCoroutine(WaitTillNoticeClear());
            }

            if (CVRInputManager.Instance.gripRightDown)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _noticeCooldown = false;
        }

        private IEnumerator WaitTillNoticeClear()
        {
            _noticeCooldown = true;
            NotificationSystem.EnqueueNotification("Totally Wholesome", "You can only pin the leash to a collider! The selector must be green!");
            yield return new WaitForSeconds(10f);
            _noticeCooldown = false;
        }
    }
}