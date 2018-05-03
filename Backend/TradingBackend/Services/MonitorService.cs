using System.Collections.Generic;

namespace TradingBackend.Services
{
    public class MonitorService
    {
        public IList<string> Errors = new List<string>();
        public string LastMessage = null;
    }
}
