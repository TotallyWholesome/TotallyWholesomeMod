using System;
using System.Linq;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class PetCountCondition : Attribute, ICondition
    {
        private int _petCount;
        
        public bool CheckCondition()
        {
            return LeadManager.Instance.PetPairs.Count >= _petCount;
        }

        public PetCountCondition(int petCount)
        {
            _petCount = petCount;
        }
    }
}