using System;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class VibrationDurationCondition : Attribute, ICondition
    {
        private int _duration;
        private float _vibrationPercentage;
        private DateTime _vibrationStarted;
        private bool _vibrating;
        
        public bool CheckCondition()
        {
            if (ButtplugManager.Instance.ActiveVibrationStrength >= _vibrationPercentage)
            {
                if(!_vibrating)
                {
                    _vibrationStarted = DateTime.Now;
                    _vibrating = true;
                }

                var compare = DateTime.Now.Subtract(_vibrationStarted);

                return compare.TotalSeconds >= _duration;
            }

            _vibrating = false;
            return false;
        }

        public VibrationDurationCondition(int duration, float vibrationPercentage)
        {
            _duration = duration;
            _vibrationPercentage = vibrationPercentage;
        }
    }
}