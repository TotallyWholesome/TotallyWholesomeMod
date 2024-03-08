using System;
using System.Drawing;
using MelonLoader;

namespace WholesomeLoader;

public static class Con
{
    public static bool DebugMode;

    //Create an instance of MelonLogger.Instance to adhere to 0.5.x logging changes
    private static readonly MelonLogger.Instance Logger = new MelonLogger.Instance("TotallyWholesome", Color.Green);

    public static void Msg(object data) => Logger.Msg(data);

    public static void Msg(ConsoleColor c, object data) => Logger.Msg(c, data);

    public static void Error(object data)
    {
        Logger.Error(data);
    }

    public static void Error(string txt, Exception e)
    {
        Logger.Error(txt, e);
    }

    public static void Debug(object data, bool isDebug = false)
    {
        if (!DebugMode) return;
        Logger.Msg(ConsoleColor.Yellow, data);
    }

    public static void Warn(object data) => Logger.Warning(data);
}