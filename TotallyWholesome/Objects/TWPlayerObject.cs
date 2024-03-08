using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using UnityEngine;

namespace TotallyWholesome.Objects
{
    public class TWPlayerObject
    {
        private CVRPlayerEntity _playerEntity;
        private bool _isRemotePlayer;

        public TWPlayerObject(CVRPlayerEntity playerEntity)
        {
            _playerEntity = playerEntity;
            
            if (ReferenceEquals(playerEntity, null)) return;
            
            _isRemotePlayer = true;
        }

        public CVRPlayerEntity CVRPlayer => _isRemotePlayer ? _playerEntity : null;

        public GameObject AvatarObject
        {
            get
            {
                if (!_isRemotePlayer)
                    return PlayerSetup.Instance._avatar;

                return _playerEntity.PuppetMaster == null ? null : _playerEntity.PuppetMaster.avatarObject;
            }
        }

        public string Uuid
        {
            get
            {
                if (!_isRemotePlayer)
                    return MetaPort.Instance.ownerId;
                return ReferenceEquals(_playerEntity, null) ? null : _playerEntity.Uuid;
            }
        }

        public string Username
        {
            get
            {
                if (!_isRemotePlayer)
                    return TWUtils.GetSelfUsername();
                return ReferenceEquals(_playerEntity, null) ? null : _playerEntity.Username;
            }
        }

        public Animator AvatarAnimator
        {
            get
            {
                if (!_isRemotePlayer)
                    return PlayerSetup.Instance._animator;
                return ReferenceEquals(_playerEntity, null) ? null : TWUtils.GetAvatarAnimator(_playerEntity.PuppetMaster);
            }
        }

        public GameObject PlayerGameObject
        {
            get
            {
                if (!_isRemotePlayer)
                    return PlayerSetup.Instance.gameObject;
                return ReferenceEquals(_playerEntity, null) ? null : _playerEntity.PlayerObject;
            }
        }

        public Vector3 PlayerPosition
        {
            get
            {
                if (!_isRemotePlayer)
                    return PlayerSetup.Instance.GetPlayerPosition();
                // remote players avatar root is stuck at their playspace center, game bug :)
                return ReferenceEquals(_playerEntity, null)
                    ? Vector3.zero
                    : _playerEntity.PuppetMaster.GetViewWorldPosition() with
                    {
                        y = _playerEntity.PuppetMaster.transform.position.y
                    };
            }
        }

        public string AvatarID
        {
            get
            {
                if (!_isRemotePlayer)
                    return MetaPort.Instance.currentAvatarGuid;
                return ReferenceEquals(_playerEntity, null) ? null : _playerEntity.AvatarId;
            }
        }

        public string PlayerIconURL
        {
            get
            {
                if (!_isRemotePlayer)
                    return "";
                return ReferenceEquals(_playerEntity, null) ? "" : _playerEntity.ApiProfileImageUrl;
            }
        }

        public override string ToString()
        {
            return $"TWPlayerObject - [Uuid: {Uuid}, Username: {Username}]";
        }

        public override bool Equals(object obj)
        {
            if(obj is TWPlayerObject playerObject)
                return Uuid == playerObject.Uuid;
            return false;
        }
    }
}