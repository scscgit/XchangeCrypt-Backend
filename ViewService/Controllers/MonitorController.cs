using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace XchangeCrypt.Backend.ViewService.Controllers
{
    [Route("api/v1/monitor/")]
    [ApiController]
    public class MonitorController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] {"value1", "value2"};
        }
    }
}
