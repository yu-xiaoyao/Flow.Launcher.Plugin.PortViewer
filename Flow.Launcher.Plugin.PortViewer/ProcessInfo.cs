using System;

namespace Flow.Launcher.Plugin.PortViewer;

public class ProcessInfo
{
    public int Pid { get; set; }
    public string Name { get; set; }
    public string FilePath { get; set; }
    public string CommandLine { get; set; }
    public string User { get; set; }
    public DateTime? StartTime { get; set; }
    public long MemoryUsage { get; set; } // Bytes
    public TimeSpan? CpuUsage { get; set; }

    public int SessionId { get; set; }
    
}