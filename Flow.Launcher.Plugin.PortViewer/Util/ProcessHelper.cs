using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Flow.Launcher.Plugin.PortViewer.Util;

public class ProcessHelper
{
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const uint PROCESS_TERMINATE = 0x0001;


    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(
        uint dwDesiredAccess,
        bool bInheritHandle,
        int dwProcessId
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(
        IntPtr hProcess,
        int dwFlags,
        char[] lpExeName,
        ref uint lpdwSize
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

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

    public static string TryGetProcessFilename(int pid)
    {
        IntPtr handle = IntPtr.Zero;

        try
        {
            handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (handle == IntPtr.Zero)
                return string.Empty;

            uint capacity = 2000;
            var buffer = new char[capacity];

            if (!QueryFullProcessImageName(handle, 0, buffer, ref capacity))
                return string.Empty;

            return new string(buffer, 0, (int)capacity);
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            if (handle != IntPtr.Zero)
                CloseHandle(handle);
        }
    }

    public static bool KillProcessByPid(int pid)
    {
        try
        {
            var p = Process.GetProcessById(pid);
            p.Kill(); // 强制结束
            p.WaitForExit(); // 等待完全退出（可选）
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool KillByPid(int pid)
    {
        var handle = OpenProcess(PROCESS_TERMINATE, false, pid);
        if (handle == IntPtr.Zero)
            return false;
        try
        {
            return TerminateProcess(handle, 1);
        }
        finally
        {
            CloseHandle(handle);
        }
    }
}