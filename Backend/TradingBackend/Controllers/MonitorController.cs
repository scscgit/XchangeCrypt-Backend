using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using XchangeCrypt.Backend.TradingBackend.Services;

namespace XchangeCrypt.Backend.TradingBackend.Controllers
{
    [Route("api/v1/monitorapi/")]
    public class MonitorController : Controller
    {
        private readonly MonitorService _monitorService;

        public MonitorController(MonitorService monitorService)
        {
            _monitorService = monitorService;
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
