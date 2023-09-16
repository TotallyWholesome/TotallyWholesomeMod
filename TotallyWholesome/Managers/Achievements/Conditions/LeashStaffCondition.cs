using System;
using System.Linq;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class LeashStaffCondition : Attribute, ICondition
    {
        private string[] _staffIDs = { "5301af21-eb8d-7b36-3ef4-b623fa51c2c6", "adf06e8a-813e-d7b0-95f9-3d8e8a0d8132", "9205dba1-0493-2257-a878-920abb85f073", "047b30bd-089d-887c-8734-b0032df5d176", "2a1af47c-ce18-6c5a-b75a-b2360555b05a", "47970916-5a98-e7fb-bf39-2e7d5c93aa2b", "f4419fb2-0f71-4072-7bbb-e3c756682f22", "877f0749-00ed-4a96-5396-ac978eb275b4", "3ae66710-5307-022f-2c31-5bcb1f468af2" };
        
        public bool CheckCondition()
        {
            return LeadManager.Instance.PetPairs.Any(x => _staffIDs.Contains(x.PetID));
        }
    }
}