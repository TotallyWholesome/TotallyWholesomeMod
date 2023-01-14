using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using TWNetCommon.Data;

namespace TotallyWholesome.Managers
{
    public class MovementManager : ITWManager
    {
        public int Priority() => 0;
        public string ManagerName() => nameof(MovementManager);

        public void Setup()
        {
            TWNetListener.MasterRemoteControlEvent += MasterRemoteControlEvent;
            TWNetListener.LeadAcceptEvent += LeadAcceptEvent;
            LeadManager.OnFollowerPairDestroyed += OnFollowerPairDestroyed;
            Patches.Patches.OnWorldLeave += OnWorldLeave;
        }

        private void OnFollowerPairDestroyed(LeadPair obj)
        {
            ChangeMovementOptions(false, false, true);
        }

        private void OnWorldLeave()
        {
            ChangeMovementOptions(false, false, true);
        }

        private void LeadAcceptEvent(LeadAccept obj)
        {
            if (!obj.FollowerID.Equals(MetaPort.Instance.ownerId)) return;
            if (!ConfigManager.Instance.IsActive(AccessType.AllowMovementControls, obj.MasterID)) return;
            
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                ChangeMovementOptions(obj.DisableFlight, obj.DisableSeats); 
            });
        }

        private void MasterRemoteControlEvent(MasterRemoteControl obj)
        {
            if (LeadManager.Instance.FollowerPair == null) return;
            if (!LeadManager.Instance.FollowerPair.Key.Equals(obj.Key)) return;
            if (!ConfigManager.Instance.IsActive(AccessType.AllowMovementControls, LeadManager.Instance.FollowerPair.MasterID)) return;

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                ChangeMovementOptions(obj.DisableFlight, obj.DisableSeats); 
            });
        }

        private void ChangeMovementOptions(bool disableFlight, bool disableSeats, bool silentSwitch = false)
        {
            if (disableFlight && !Patches.Patches.IsFlightLocked)
            {
                Patches.Patches.IsFlightLocked = true;
                MovementSystem.Instance.ChangeFlight(false);
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has disabled flight!", 3f, TWAssets.Handcuffs);
            }

            if (!disableFlight && Patches.Patches.IsFlightLocked)
            {
                Patches.Patches.IsFlightLocked = false;
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has allowed flight!", 3f, TWAssets.Checkmark);
            }

            if (disableSeats && !Patches.Patches.AreSeatsLocked)
            {
                Patches.Patches.AreSeatsLocked = true;
                if(MovementSystem.Instance.lastSeat != null)
                    MovementSystem.Instance.lastSeat.ExitSeat();
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has disabled seat usage!", 3f, TWAssets.Handcuffs);
            }

            if (!disableSeats && Patches.Patches.AreSeatsLocked)
            {
                Patches.Patches.AreSeatsLocked = false;
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has allowed seat usage!", 3f, TWAssets.Checkmark);
            }
        }

        public void LateSetup()
        {
            
        }
    }
}