using System;
using System.Linq;
using System.Reflection;
using MelonLoader;
using WholesomeLoader;

namespace TotallyWholesome.Managers.ModCompatibility.CompatbilityReflections
{
    public class NotificationAPIAdapter
    {
        private static bool? _notificationAPIAvailable;
        private static MethodInfo _notifyMethod;
        private static ConstructorInfo _notificationCtor;
        private static bool _methodsGetRan;
        
        public static bool IsNotifAPIAvailable()
        {
            _notificationAPIAvailable ??= MelonMod.RegisteredMelons.Any(x => x.Info.Name.Equals("NotificationAPI", StringComparison.OrdinalIgnoreCase));
            return _notificationAPIAvailable.Value;
        }

        public static bool GetNotifAPIMethods()
        {
            if (!IsNotifAPIAvailable()) return false;
            if (_methodsGetRan) return true;

            Type notification = Type.GetType("Notification, NotificationAPI");
            Type notificationAPI = Type.GetType("NotificationAPI.NotificationAPI, NotificationAPI");

            if (notification == null || notificationAPI == null)
            {
                Con.Error($"Unable to properly retrieve NotificationAPI types! NotificationAPI support will not be available! Notification - {notification != null} | NotificationAPI - {notificationAPI != null}");
                _notificationAPIAvailable = false;
                return false;
            }

            try
            {
                _notifyMethod = notificationAPI.GetMethods().FirstOrDefault(x => x.GetParameters().Length == 1 && x.GetParameters().All(p => p.ParameterType != typeof(string)));
                _notificationCtor = notification.GetConstructor(new Type[] { typeof(string), typeof(int), typeof(int), typeof(string), typeof(string) });
            }
            catch (Exception)
            {
                Con.Error("Unable to properly retrieve NotificationAPI methods! NotificationAPI support will not be available!");
                _notificationAPIAvailable = false;
                return false;
            }

            if (_notifyMethod == null)
            {
                Con.Error("Unable to properly retrieve NotificationAPI methods! NotificationAPI support will not be available!");
                _notificationAPIAvailable = false;
                return false;
            }

            Con.Debug("Successfully retrieved NotificationAPI methods via reflection!");
            _methodsGetRan = true;

            return true;
        }

        public static void Notify(string msg, int iterations = 3, int duration = 8, string color = "#ffef00", string theme = "")
        {
            if (!GetNotifAPIMethods()) return;

            var notification = _notificationCtor.Invoke(new object[] { msg, iterations, duration, color, theme });

            if (notification != null)
                _notifyMethod.Invoke(null, new object[] { notification });
        }
    }
}