using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.PortViewer
{
    public class PortViewer : IPlugin
    {
        private PluginInitContext _context;

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            if (string.IsNullOrEmpty(query.Search))
            {
                var sps = SocketHelper.GetListenerPortProcess();
                return _buildResults(sps);
            }

            var firstSearch = query.FirstSearch;

            return new List<Result>();
        }


        private List<Result> _buildResults(List<(SocketInfo, ProcessInfo)> sps)
        {
            var socketInfos = sps.Select(f => f.Item1).ToList();
            return new List<Result>();
        }
    }
}