using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flow.Launcher.Plugin.PortViewer.Util;

namespace Flow.Launcher.Plugin.PortViewer
{
    public class PortViewer : IPlugin, IContextMenu
    {
        public const string IconPath = "Images\\PortViewer.png";

        private PluginInitContext _context;
        private Settings _settings;

        public void Init(PluginInitContext context)
        {
            InnerLogger.SetAsFlowLauncherLogger(context.API, LoggerLevel.DEBUG);

            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();
        }

        public List<Result> Query(Query query)
        {
            // InnerLogger.Logger.Info($"FirstSearch: {query.FirstSearch}");

            var firstSearch = query.FirstSearch;

            var filterString = "";
            SocketType? socketType = null;

            if (!string.IsNullOrEmpty(firstSearch))
            {
                if (string.Equals(nameof(SocketType.Tcp), firstSearch, StringComparison.OrdinalIgnoreCase))
                {
                    socketType = SocketType.Tcp;
                    filterString = query.SecondToEndSearch;
                }
                else if (string.Equals(nameof(SocketType.Udp), firstSearch, StringComparison.OrdinalIgnoreCase))
                {
                    socketType = SocketType.Udp;
                    filterString = query.SecondToEndSearch;
                }
                else
                    filterString = firstSearch;
            }

            // InnerLogger.Logger.Info($"socketType: {socketType}. filterString: {filterString}");

            var isResolveProcessName = _settings.ResolveProcessName;
            var queryInfoList = SocketHelper.GetListenerPortProcess(socketType);

            // queryInfoList = queryInfoList.Where(x => x.Item1.ProcessId > 10).ToList();

            var results = new List<Result>();

            foreach (var (socketInfo, processInfo) in queryInfoList)
            {
                var localAddress = socketInfo.IpType == IpType.Ipv6
                    ? $"[{socketInfo.LocalAddress}]"
                    : socketInfo.LocalAddress.ToString();

                string title;
                if (isResolveProcessName)
                {
                    title = string.IsNullOrEmpty(processInfo.Name)
                        ? $"{localAddress}:{socketInfo.LocalPort}"
                        : $"{localAddress}:{socketInfo.LocalPort} - {processInfo.Name}";
                }
                else
                    title = $"{localAddress}:{socketInfo.LocalPort}";

                if (!string.IsNullOrEmpty(filterString))
                {
                    if (!title.Contains(filterString))
                        continue;
                }

                var protocol = socketInfo is TcpConnectionInfo ? "TCP" : "UDP";

                var subTitle = $"{protocol}. PID: {processInfo.Pid}";
                if (socketInfo is TcpConnectionInfo tcpConnectionInfo)
                {
                    subTitle += $". {tcpConnectionInfo.StateString}";
                }

                if (!string.IsNullOrEmpty(processInfo.FilePath))
                {
                    subTitle += $". {processInfo.FilePath}";
                }


                var path = ProcessHelper.TryGetProcessFilename(socketInfo.ProcessId);
                if (string.IsNullOrEmpty(path))
                    path = IconPath;

                results.Add(new Result
                {
                    Title = title,
                    SubTitle = subTitle,
                    AutoCompleteText = $"{query.ActionKeyword} {title}",
                    IcoPath = path,
                    Action = _ =>
                    {
                        _context.API.ChangeQuery($"{query.ActionKeyword} {title}");
                        return false;
                    },
                    ContextData = new Tuple<SocketInfo, ProcessInfo>(socketInfo, processInfo)
                });
            }

            return results;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            InnerLogger.Logger.Info($"LoadContextMenus: {selectedResult.Title}");
            var (socketInfo, processInfo) = (Tuple<SocketInfo, ProcessInfo>)selectedResult.ContextData;
            var path = ProcessHelper.TryGetProcessFilename(socketInfo.ProcessId);
            if (string.IsNullOrEmpty(path))
                path = IconPath;

            var results = new List<Result>
            {
                new()
                {
                    IcoPath = path,
                    Title = $"Kill Process {processInfo.Pid}",
                    SubTitle = $"{processInfo.Pid}",
                    CopyText = $"{processInfo.Pid}",
                    AsyncAction = async _ =>
                    {
                        await Task.Run(() => { _killProcessByPid(processInfo.Pid); });
                        return true;
                    }
                },
                new()
                {
                    IcoPath = path,
                    Title = $"Copy PID {processInfo.Pid}",
                    SubTitle = $"{processInfo.Pid}",
                    CopyText = $"{processInfo.Pid}",
                    Action = _ =>
                    {
                        _context.API.CopyToClipboard($"{processInfo.Pid}", false, false);
                        return true;
                    }
                }
            };

            var isJava = JavaProcessorHelper.IsJavaProcess(processInfo.FilePath);
            if (isJava)
            {
                var commandLine = PsShellProcessHelper.GetCommandLineByPidPowershell(processInfo.Pid);
                if (!string.IsNullOrEmpty(commandLine))
                {
                    results.Add(new Result()
                    {
                        IcoPath = path,
                        Title = "Copy Java Command",
                        SubTitle = commandLine,
                        CopyText = commandLine,
                        Action = _ =>
                        {
                            _context.API.CopyToClipboard(commandLine, false, false);
                            return true;
                        }
                    });
                    var commandList = PsShellProcessHelper.SplitCommandList(commandLine);
                    var startClass = JavaProcessorHelper.GetJavaLauncherClassByCommand(commandList);
                    if (!string.IsNullOrEmpty(startClass))
                    {
                        results.Add(new Result()
                        {
                            IcoPath = path,
                            Title = "Copy Java Launcher Class",
                            SubTitle = startClass,
                            CopyText = startClass,
                            Action = _ =>
                            {
                                _context.API.CopyToClipboard(startClass, false, false);
                                return true;
                            }
                        });
                    }
                }
            }

            return results;
        }

        private void _killProcessByPid(int pid)
        {
            InnerLogger.Logger.Info($"KillProcessByPid: {pid}");

            var pi = ProcessHelper.GetProcessInfo(pid);
            if (pi == null) return;
            if (!ProcessHelper.KillProcessByPid(pid))
                ProcessHelper.KillByPid(pid);
        }
    }
}