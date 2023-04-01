using System;
using System.Linq;
using ABI_RC.Core.Player;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class UserExistsCondition : Attribute, ICondition
    {
        private string _userID;
        
        public bool CheckCondition()
        {
            return CVRPlayerManager.Instance.NetworkPlayers.Any(x => x.Uuid == _userID);
        }

        public UserExistsCondition(string userID)
        {
            _userID = userID;
        }
    }
}