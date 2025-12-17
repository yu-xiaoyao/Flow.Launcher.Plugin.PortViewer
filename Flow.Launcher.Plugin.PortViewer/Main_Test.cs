using System;
using Flow.Launcher.Plugin.PortViewer.Util;

namespace Flow.Launcher.Plugin.PortViewer;

public class Main_Test
{
    public static void Main()
    {
        InnerLogger.SetAsConsoleLogger(LoggerLevel.DEBUG);

        // test_get_tcp_listener();
        // test_get_udp_listener();

        test_get_process_info();
    }


    private static void test_get_tcp_listener()
    {
        var connections = SocketHelper.GetTcpListenerConnections();
        foreach (var conn in connections)
        {
            Console.WriteLine(
                $"ProcessId: {conn.ProcessId} .LocalAddress: {conn.LocalAddress}:{conn.LocalPort}, RemotePort: {conn.RemotePort}. State: {conn.State}");
            var pi = ProcessHelper.GetProcessInfo(conn.ProcessId);
            if (pi != null)
            {
                Console.WriteLine(
                    $"ProcessInfo. Name: {pi.Name}. CommandLine: {pi.CommandLine}");
            }
        }
    }

    private static void test_get_udp_listener()
    {
        var connections = SocketHelper.GetAllUdpConnections();
        foreach (var conn in connections)
        {
            Console.WriteLine(
                $"ProcessId: \t{conn.ProcessId}\t LocalAddress: {conn.LocalAddress}:{conn.LocalPort}");
        }
    }


    private static void test_get_process_info()
    {
        var pid = 26872;
        var result = PsShellProcessHelper.GetCommandLineByPidPowershell(pid);

        Console.WriteLine(result);
        Console.WriteLine();


        if (result != null)
        {
            var args = PsShellProcessHelper.SplitCommandList(result);

            var startClass = JavaProcessorHelper.GetJavaLauncherClassByCommand(args);

            Console.WriteLine($"startClass: {startClass}");

            foreach (string arg in args)
            {
                Console.WriteLine(arg);
            }
        }
    }
}