#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace Flow.Launcher.Plugin.PortViewer;

public enum QueryAddressType
{
    IpV4,
    IpV6,
    All
}

public class SocketHelper
{
    public static List<(SocketInfo, ProcessInfo)> GetListenerPortProcess()
    {
        var processDict = new Dictionary<int, ProcessInfo?>();

        var tcpConnections = GetTcpListenerConnections();

        var cpList = new List<(SocketInfo, ProcessInfo)>();

        foreach (var tcpConnection in tcpConnections)
        {
            var pid = tcpConnection.ProcessId;
            if (pid <= 0) continue;

            if (!processDict.TryGetValue(pid, out var processInfo))
            {
                processInfo = WindowsApi.GetProcessInfo(pid) ?? new ProcessInfo
                {
                    Pid = pid
                };
                processDict[pid] = processInfo;
            }

            cpList.Add((tcpConnection, processInfo));
        }

        var udpConnections = GetAllUdpConnections();


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