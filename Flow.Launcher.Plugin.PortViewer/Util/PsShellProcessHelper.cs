using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Flow.Launcher.Plugin.PortViewer.Util;

public class PsShellProcessHelper
{
    public static string GetCommandLineByPidPowershell(int pid)
    {
        // Get-CimInstance 查询 WMI 上的 Win32_Process 类，该类稳定包含 CommandLine 属性。
        // -Filter "ProcessId = {pid}" 确保只查询目标 PID。
        // $"Get-CimInstance Win32_Process -Filter \"ProcessId = {pid}\" | Select-Object -ExpandProperty CommandLine";
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                ArgumentList =
                {
                    "Get-CimInstance", "Win32_Process", "-Filter", $"\"ProcessId = {pid}\"", "|",
                    "Select-Object", "-ExpandProperty", "CommandLine"
                },
                // UseShellExecute = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.Default // 确保中文等字符正确读取
            };

            using var process = Process.Start(startInfo);
            if (process == null) return null;
            process.WaitForExit();

            var output = process.StandardOutput.ReadToEnd().Trim();

            // 检查退出码和输出，如果找到结果，输出就是命令行
            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // 某些情况下，输出可能包含额外的换行符，再次清理
                return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }

            // 进程不存在或查询失败
            return null;
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"Error executing PowerShell for PID {pid}: {ex.Message}");
            return null;
        }
    }


    [DllImport("shell32.dll", SetLastError = true)]
    private static extern IntPtr CommandLineToArgvW(
        [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine,
        out int pNumArgs);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    public static string[] SplitCommandList(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return Array.Empty<string>();

        IntPtr argv = CommandLineToArgvW(commandLine, out int argc);
        if (argv == IntPtr.Zero)
            throw new Win32Exception();

        try
        {
            var args = new string[argc];
            for (int i = 0; i < argc; i++)
            {
                IntPtr p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                args[i] = Marshal.PtrToStringUni(p);
            }

            return args;
        }
        finally
        {
            LocalFree(argv);
        }
    }
}