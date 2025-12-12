using System;
using Flow.Launcher.Plugin.PortViewer.Util;

namespace Flow.Launcher.Plugin.PortViewer;

public class Main_Test
{
    public static void Main()
    {
        InnerLogger.SetAsConsoleLogger(LoggerLevel.DEBUG);

        test_get_tcp_listener();
        // test_get_udp_listener();
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
}