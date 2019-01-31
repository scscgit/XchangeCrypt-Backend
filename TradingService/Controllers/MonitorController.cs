using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.TradingService.Services.Meta;

namespace XchangeCrypt.Backend.TradingService.Controllers
{
    [Route("api/v1/monitor/")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class MonitorController : Controller
    {
        private readonly MonitorService _monitorService;
        private readonly ILogger<MonitorController> _logger;

        public MonitorController(MonitorService monitorService, ILogger<MonitorController> logger)
        {
            _monitorService = monitorService;
            _logger = logger;
        }

        [HttpGet("errors")]
        public IEnumerable<string> Errors()
        {
            return _monitorService.GetErrors();
        }

        [HttpGet("last_message")]
        public string LastMessage()
        {
            return _monitorService.LastMessage;
        }
    }
}
