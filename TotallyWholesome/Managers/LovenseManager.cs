using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lovense;

namespace TotallyWholesome.Managers
{
    public class LovenseManager
    {
        public static Remote remote;
        public static List<Toy> connectedToys;
        public static async Task Initialize()
        {
            while (connectedToys == null || connectedToys.Count == 0)
            {
                while (RoomManager.field_Internal_Static_ApiWorld_0 != null)
                    await Task.Delay(1000);
                remote = new Remote();
                try
                {
                    connectedToys = await remote.Discover();
                }
                catch (Exception ex)
                {
                    WholesomeLoader.Con.Error("Exception occured: " + ex.ToString());
                }
                await Task.Delay(1000);
            }
        }
        public static async Task Rediscover()
        {
            connectedToys = await remote.Discover();
        }
        public static async Task VibrateAll(int vibration)
        {
            foreach (Toy toy in connectedToys)
            {
                if (toy == null) return;
                await toy.Vibrate(Toy.Vibrator.AllVibrator, vibration);
            }
        }
        public static IEnumerator Loop()
        {
            while (true)
            {
                yield return new UnityEngine.WaitForEndOfFrame();

            }
        }
    }
}
