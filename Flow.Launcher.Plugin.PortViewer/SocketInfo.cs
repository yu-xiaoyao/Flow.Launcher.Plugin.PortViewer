using System.Net;

namespace Flow.Launcher.Plugin.PortViewer;

public enum SocketType
{
    Tcp,
    Udp
}

public enum IpType
{
    Ipv4,
    Ipv6
}

public class SocketInfo
{
    public IpType IpType { get; set; }
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