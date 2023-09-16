using System;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class DateCondition : Attribute, ICondition
    {
        private DateTime _startTime;
        private DateTime _endTime;
        
        public bool CheckCondition()
        {
            var now = DateTime.Now;
            return now > _startTime && now < _endTime;
        }

        public DateCondition(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay)
        {
            _startTime = new DateTime(startYear, startMonth, startDay);
            _endTime = new DateTime(endYear, endMonth, endDay);
        }
    }
}