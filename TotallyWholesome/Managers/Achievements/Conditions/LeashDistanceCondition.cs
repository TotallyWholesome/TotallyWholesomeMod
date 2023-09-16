using System;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class LeashDistanceCondition : Attribute, ICondition
    {
        private float _leashDistance;
        private DistanceCheckMode _checkMode;

        public bool CheckCondition()
        {
            switch (_checkMode)
            {
                case DistanceCheckMode.Equal:
                    return Math.Abs(LeadManager.Instance.TetherRange.SliderValue - _leashDistance) < 0.1f;
                case DistanceCheckMode.Greater:
                    return LeadManager.Instance.TetherRange.SliderValue > _leashDistance;
                case DistanceCheckMode.EqualOrGreater:
                    return LeadManager.Instance.TetherRange.SliderValue >= _leashDistance;
                case DistanceCheckMode.Less:
                    return LeadManager.Instance.TetherRange.SliderValue < _leashDistance;
                case DistanceCheckMode.LessOrEqual:
                    return LeadManager.Instance.TetherRange.SliderValue <= _leashDistance;
            }

            return false;
        }

        public LeashDistanceCondition(float leashDistance, DistanceCheckMode checkMode = DistanceCheckMode.EqualOrGreater)
        {
            _leashDistance = leashDistance;
            _checkMode = checkMode;
        }
    }

    public enum DistanceCheckMode
    {
        Equal,
        Greater,
        EqualOrGreater,
        Less,
        LessOrEqual
    }
}