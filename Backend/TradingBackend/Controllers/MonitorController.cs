using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using TradingBackend.Services;

namespace TradingBackend.Controllers
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
            return _monitorService.Errors;
        }

        [HttpGet("last_message")]
        public string LastMessage()
        {
            return _monitorService.LastMessage;
        }
    }
}
