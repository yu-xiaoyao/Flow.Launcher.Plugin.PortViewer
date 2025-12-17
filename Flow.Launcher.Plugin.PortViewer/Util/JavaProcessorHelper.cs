using System;
using System.IO;

namespace Flow.Launcher.Plugin.PortViewer.Util;

public class JavaProcessorHelper
{
    public static bool IsJavaProcess(string javaPath)
    {
        if (!javaPath.EndsWith("java.exe", StringComparison.OrdinalIgnoreCase))
            return false;
        if (File.Exists(javaPath))
            return true;
        return false;
    }

    public static string GetJavaLauncherClassByCommand(string[] args)
    {
        if (args.Length == 0)
            return null;

        var index = -1;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.EndsWith("java.exe", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.Equals(arg, "-classpath"))
            {
                i++;
                continue;
            }

            if (arg.StartsWith("-")) continue;

            index = i;
        }

        return index != -1 ? args[index] : null;
    }

    public static string GetJavaInfoByJps(string javaPath, int pid)
    {
        if (!File.Exists(javaPath))
            return null;

        var dir = Path.GetDirectoryName(javaPath);
        var jpsPath = Path.Join(dir, "jps.exe");
        if (File.Exists(jpsPath))
        {
        }

        return null;
    }
}