#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using Flow.Launcher.Plugin.PortViewer.Util;

namespace Flow.Launcher.Plugin.PortViewer;

public enum QueryAddressType
{
    IpV4,
    IpV6,
    All
}

public class SocketHelper
{
    public static List<int> ExecuteTcpPort = new()
    {
        135, // RPC / DCOM / EPM 相关
        445, // SMB
    };

    public static List<int> ExecuteUdpPort = new()
    {
        53, // DNS
        67, 68, // DHCP
    };

    public static List<(SocketInfo, ProcessInfo)> GetListenerPortProcess(
        SocketType? socketType = null,
        QueryAddressType address = QueryAddressType.All)
    {
        var cpList = new List<(SocketInfo, ProcessInfo)>();

        var processDict = new Dictionary<int, ProcessInfo>();

        if (socketType is null or SocketType.Tcp)
        {
            var tcpConnections = GetTcpListenerConnections(address);
            foreach (var tcpConnection in tcpConnections)
            {
                if (ExecuteTcpPort.Contains(tcpConnection.LocalPort))
                    continue;

                var pid = tcpConnection.ProcessId;
                if (pid <= 0) continue;

                if (!processDict.TryGetValue(pid, out var processInfo))
                {
                    processInfo = ProcessHelper.GetProcessInfo(pid) ?? new ProcessInfo
                    {
                        Pid = pid
                    };
                    processDict[pid] = processInfo;
                }

                cpList.Add((tcpConnection, processInfo));
            }
        }

        if (socketType is null or SocketType.Udp)
        {
            var udpConnections = GetAllUdpConnections(address);
            foreach (var udpConnection in udpConnections)
            {
                if (ExecuteUdpPort.Contains(udpConnection.LocalPort))
                    continue;

                var pid = udpConnection.ProcessId;
                if (pid <= 0) continue;

                if (!processDict.TryGetValue(pid, out var processInfo))
                {
                    processInfo = ProcessHelper.GetProcessInfo(pid) ?? new ProcessInfo
                    {
                        Pid = pid
                    };
                    processDict[pid] = processInfo;
                }

                cpList.Add((udpConnection, processInfo));
            }
        }

        return cpList;
    }

    public static List<TcpConnectionInfo> GetAllTcpConnections(QueryAddressType address = QueryAddressType.All)
    {
        var connections = new List<TcpConnectionInfo>();
        if (address is QueryAddressType.IpV4 or QueryAddressType.All)
        {
            var list = GetTcpConnections(TcpTableClass.TcpTableOwnerPidAll);
            connections.AddRange(list);
        }

        if (address is QueryAddressType.IpV6 or QueryAddressType.All)
        {
            var list = GetTcp6Connections(TcpTableClass.TcpTableOwnerPidAll);
            connections.AddRange(list);
        }

        return connections;
    }

    public static List<TcpConnectionInfo> GetTcpListenerConnections(QueryAddressType address = QueryAddressType.All)
    {
        var connections = new List<TcpConnectionInfo>();
        if (address is QueryAddressType.IpV4 or QueryAddressType.All)
        {
            var list = GetTcpConnections(TcpTableClass.TcpTableOwnerPidListener);
            connections.AddRange(list);
        }

        if (address is QueryAddressType.IpV6 or QueryAddressType.All)
        {
            var list = GetTcp6Connections(TcpTableClass.TcpTableOwnerPidListener);
            connections.AddRange(list);
        }

        return connections;
    }

    public static List<TcpConnectionInfo> GetTcpConnections(TcpTableClass tableClass)
    {
        var result = new List<TcpConnectionInfo>();
        int buffSize = 0;

        // 第一次调用用于获取缓冲区大小
        WindowsApi.GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, WindowsApi.AF_INET, tableClass, 0);
        IntPtr tcpTablePtr = Marshal.AllocHGlobal(buffSize);

        try
        {
            uint ret = WindowsApi.GetExtendedTcpTable(tcpTablePtr, ref buffSize, true, WindowsApi.AF_INET, tableClass,
                0);
            if (ret != 0)
                throw new Win32Exception((int)ret);

            int rowSize = Marshal.SizeOf(typeof(WindowsApi.MIB_TCPROW_OWNER_PID));
            uint numEntries = (uint)Marshal.ReadInt32(tcpTablePtr);

            IntPtr rowPtr = IntPtr.Add(tcpTablePtr, 4);

            for (int i = 0; i < numEntries; i++)
            {
                WindowsApi.MIB_TCPROW_OWNER_PID row = Marshal.PtrToStructure<WindowsApi.MIB_TCPROW_OWNER_PID>(rowPtr);

                result.Add(new TcpConnectionInfo
                {
                    IpType = IpType.Ipv4,
                    ProcessId = row.owningPid,
                    LocalAddress = new IPAddress(row.localAddr),
                    LocalPort = WindowsApi.ToHostOrderPort(row.localPort),
                    RemoteAddress = new IPAddress(row.remoteAddr),
                    RemotePort = WindowsApi.ToHostOrderPort(row.remotePort),
                    State = row.state
                });

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }

        return result;
    }


    public static List<TcpConnectionInfo> GetTcp6Connections(TcpTableClass tableClass)
    {
        var result = new List<TcpConnectionInfo>();
        int buffSize = 0;

        // 第一次调用用于获取缓冲区大小
        WindowsApi.GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, WindowsApi.AF_INET6, tableClass, 0);
        IntPtr tcpTablePtr = Marshal.AllocHGlobal(buffSize);

        try
        {
            uint ret = WindowsApi.GetExtendedTcpTable(tcpTablePtr, ref buffSize, true, WindowsApi.AF_INET6, tableClass,
                0);
            if (ret != 0)
                throw new Win32Exception((int)ret);

            int rowSize = Marshal.SizeOf(typeof(WindowsApi.MIB_TCP6ROW_OWNER_PID));
            uint numEntries = (uint)Marshal.ReadInt32(tcpTablePtr);

            IntPtr rowPtr = IntPtr.Add(tcpTablePtr, 4);

            for (var i = 0; i < numEntries; i++)
            {
                var row = Marshal.PtrToStructure<WindowsApi.MIB_TCP6ROW_OWNER_PID>(rowPtr);

                result.Add(new TcpConnectionInfo
                {
                    IpType = IpType.Ipv6,
                    ProcessId = row.OwningPid,
                    LocalAddress = new IPAddress(row.LocalAddr),
                    LocalPort = WindowsApi.ToHostOrderPort(row.LocalPort),
                    RemoteAddress = new IPAddress(row.RemoteAddr),
                    RemotePort = WindowsApi.ToHostOrderPort(row.RemotePort),
                    State = row.State
                });

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }

        return result;
    }

    public static List<SocketInfo> GetAllUdpConnections(QueryAddressType address = QueryAddressType.All)
    {
        var connections = new List<SocketInfo>();
        if (address is QueryAddressType.IpV4 or QueryAddressType.All)
        {
            var list = GetUdpConnections();
            connections.AddRange(list);
        }

        if (address is QueryAddressType.IpV6 or QueryAddressType.All)
        {
            var list = GetUdp6Connections();
            connections.AddRange(list);
        }

        return connections;
    }

    public static List<SocketInfo> GetUdpConnections()
    {
        var result = new List<SocketInfo>();
        int buffSize = 0;

        WindowsApi.GetExtendedUdpTable(IntPtr.Zero, ref buffSize, true, WindowsApi.AF_INET,
            UdpTableClass.UdpTableOwnerPid, 0);
        IntPtr udpTablePtr = Marshal.AllocHGlobal(buffSize);

        try
        {
            uint ret = WindowsApi.GetExtendedUdpTable(udpTablePtr, ref buffSize, true, WindowsApi.AF_INET,
                UdpTableClass.UdpTableOwnerPid, 0);
            if (ret != 0)
                throw new Win32Exception((int)ret);

            int rowSize = Marshal.SizeOf(typeof(WindowsApi.MIB_UDPROW_OWNER_PID));
            uint numEntries = (uint)Marshal.ReadInt32(udpTablePtr);

            IntPtr rowPtr = IntPtr.Add(udpTablePtr, 4);

            for (int i = 0; i < numEntries; i++)
            {
                WindowsApi.MIB_UDPROW_OWNER_PID row = Marshal.PtrToStructure<WindowsApi.MIB_UDPROW_OWNER_PID>(rowPtr);
                result.Add(new SocketInfo
                {
                    IpType = IpType.Ipv4,
                    ProcessId = row.owningPid,
                    LocalAddress = new IPAddress(row.localAddr),
                    LocalPort = WindowsApi.ToHostOrderPort(row.localPort),
                });

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(udpTablePtr);
        }

        return result;
    }

    public static List<SocketInfo> GetUdp6Connections()
    {
        var result = new List<SocketInfo>();
        int buffSize = 0;

        WindowsApi.GetExtendedUdpTable(IntPtr.Zero, ref buffSize, true, WindowsApi.AF_INET6,
            UdpTableClass.UdpTableOwnerPid, 0);
        IntPtr udpTablePtr = Marshal.AllocHGlobal(buffSize);

        try
        {
            uint ret = WindowsApi.GetExtendedUdpTable(udpTablePtr, ref buffSize, true, WindowsApi.AF_INET6,
                UdpTableClass.UdpTableOwnerPid, 0);
            if (ret != 0)
                throw new Win32Exception((int)ret);

            int rowSize = Marshal.SizeOf(typeof(WindowsApi.MIB_UDPROW_OWNER_PID));
            uint numEntries = (uint)Marshal.ReadInt32(udpTablePtr);

            IntPtr rowPtr = IntPtr.Add(udpTablePtr, 4);

            for (int i = 0; i < numEntries; i++)
            {
                var row = Marshal.PtrToStructure<WindowsApi.MIB_UDP6ROW_OWNER_PID>(rowPtr);
                result.Add(new SocketInfo
                {
                    IpType = IpType.Ipv6,
                    ProcessId = row.OwningPid,
                    LocalAddress = new IPAddress(row.LocalAddr),
                    LocalPort = WindowsApi.ToHostOrderPort(row.LocalPort),
                });

                rowPtr = IntPtr.Add(rowPtr, rowSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(udpTablePtr);
        }

        return result;
    }
}