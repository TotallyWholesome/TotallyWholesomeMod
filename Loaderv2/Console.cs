using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;

namespace WholesomeLoader
{
    public static class Con
    {
        public static bool DebugMode;
        
        //Create an instance of MelonLogger.Instance to adhere to 0.5.x logging changes
        private static MelonLogger.Instance _logger = new MelonLogger.Instance("TotallyWholesome", ConsoleColor.Green); 

        public static void Msg(object data) => _logger.Msg(data);
        
        public static void Msg(ConsoleColor c, object data) => _logger.Msg(c, data);

        public static void Error(object data) {
            _logger.Error(data);
        }

        public static void Debug(object data, bool isDebug = false)
        {
            if (!DebugMode) return;
            _logger.Msg(ConsoleColor.Yellow, data);
        }

        public static void Warn(object data) => _logger.Warning(data);
    }
}
