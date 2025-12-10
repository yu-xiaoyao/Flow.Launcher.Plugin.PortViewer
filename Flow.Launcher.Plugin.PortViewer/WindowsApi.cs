using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Flow.Launcher.Plugin.PortViewer.Util;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.PortViewer;

public enum TcpTableClass
{
    TcpTableBasicListener,
    TcpTableBasicConnections,
    TcpTableBasicAll,
    TcpTableOwnerPidListener,
    TcpTableOwnerPidConnections,
    TcpTableOwnerPidAll // 获取所有包含 PID 的 TCP 连接
}

public enum UdpTableClass
{
    UdpTableBasic,
    UdpTableOwnerPid
}

/// <summary>
/// 定义 TCP 状态
/// </summary>
public enum TcpState
{
    Closed = 1,
    Listen = 2,
    SynSent = 3,
    SynRcvd = 4,
    Established = 5,
    FinWait1 = 6,
    FinWait2 = 7,
    CloseWait = 8,
    Closing = 9,
    LastAck = 10,
    TimeWait = 11,
    DeleteTcb = 12
}

public class WindowsApi
{
    public const int AF_INET = 2; // IPv4
    public const int AF_INET6 = 23; // IPv6

    // IPv4
    // MIB_TCPROW_OWNER_PID 结构体
    // 用于存储单个 TCP 连接的详细信息，包括 PID。
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public TcpState state;
        public uint localAddr;
        public uint localPort; // 端口高低位是反转的
        public uint remoteAddr;
        public uint remotePort; // 端口高低位是反转的

        public int owningPid;
    }

    // IPv6
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCP6ROW_OWNER_PID
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] LocalAddr;

        public uint LocalScopeId;
        public uint LocalPort;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] RemoteAddr;

        public uint RemoteScopeId;
        public uint RemotePort;

        public TcpState State;
        public int OwningPid;
    }


    // MIB_TCPTABLE_OWNER_PID 结构体
    // 包含连接数量和连接数组
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        private MIB_TCPROW_OWNER_PID table;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPROW_OWNER_PID
    {
        public uint localAddr;
        public uint localPort;
        public int owningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDP6ROW_OWNER_PID
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] LocalAddr;

        public uint LocalScopeId;
        public uint LocalPort;

        public int OwningPid;
    }


    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        TcpTableClass tblClass,
        int reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedUdpTable(
        IntPtr pUdpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        UdpTableClass tblClass,
        int reserved);


    public static int ToHostOrderPort(uint port)
    {
        // return IPAddress.NetworkToHostOrder((short)port);
        ushort newPort = (ushort)IPAddress.NetworkToHostOrder((short)(port & 0xFFFF));
        return newPort;
    }

    [CanBeNull]
    public static ProcessInfo GetProcessInfo(int pid)
    {
        try
        {
            var p = Process.GetProcessById(pid);

            return new ProcessInfo
            {
                Pid = pid,
                Name = p.ProcessName,
                FilePath = p.MainModule?.FileName,
                StartTime = p.StartTime,
                MemoryUsage = p.WorkingSet64,
                CpuUsage = p.TotalProcessorTime,
                SessionId = p.SessionId
            };
        }
        catch (Win32Exception win32Ex)
        {
            InnerLogger.Logger.Debug($"FailedWin32GetProcessInfo. PID={pid}. {win32Ex.Message}");
        }
        catch (Exception ex)
        {
            InnerLogger.Logger.Error($"FailedGetProcessInfo. PID={pid}. {ex.Message}");
        }

        return null;
    }
}