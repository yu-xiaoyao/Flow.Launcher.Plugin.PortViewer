using System.Net;

namespace Flow.Launcher.Plugin.PortViewer;

public class SocketInfo
{
    public int ProcessId { get; set; }

    public IPAddress LocalAddress { get; set; }
    public int LocalPort { get; set; }

    public IPAddress RemoteAddress { get; set; }
    public int RemotePort { get; set; }
}

public class TcpConnectionInfo : SocketInfo
{
    public TcpState State { get; set; }

    public string StateString => State.ToString();
}