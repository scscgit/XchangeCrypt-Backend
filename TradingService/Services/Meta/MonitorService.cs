using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XchangeCrypt.Backend.TradingService.Services.Meta
{
    public class MonitorService
    {
        private const int MaxErrors = 1000;

        private IList<string> _errors = new List<string>();

        public bool Enabled = true;
        public string LastMessage = null;

        public IEnumerable<string> GetErrors()
        {
            return new ReadOnlyCollection<string>(_errors);
        }

        public void ReportError(string error)
        {
            if (!Enabled)
            {
                return;
            }

            _errors.Add(error);
            if (_errors.Count > MaxErrors)
            {
                _errors.RemoveAt(0);
            }
        }
    }
}
