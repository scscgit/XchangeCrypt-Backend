using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Mvc;
using XchangeCrypt.Backend.ViewService.Services;

namespace XchangeCrypt.Backend.ViewService.Controllers
{
    [Route("api/v1/view/")]
    [ApiController]
    public class OrderViewController : ControllerBase
    {
        private readonly OrderCaching _orderCaching;

        public OrderViewController(OrderCaching orderCaching)
        {
            _orderCaching = orderCaching;
        }

        [HttpGet]
        [Route("executions")]
        public List<Execution> AccountsAccountIdExecutionsGet(
            [FromQuery] [Required] string user,
            [FromQuery] [Required] string accountId,
            [FromQuery] [Required] string instrument,
            [FromQuery] int? maxCount)
        {
            return _orderCaching.GetExecutions(user, accountId, instrument, maxCount);
        }

        [HttpGet]
        [Route("orders")]
        public List<Order> AccountsAccountIdExecutionsGet(
            [FromQuery] [Required] string user,
            [FromQuery] [Required] string accountId)
        {
            return _orderCaching.GetOrders(user, accountId);
        }

        [HttpGet]
        [Route("order")]
        public Order AccountsAccountIdExecutionsGet(
            [FromQuery] [Required] string user,
            [FromQuery] [Required] string accountId,
            [FromQuery] [Required] string orderId)
        {
            return _orderCaching.GetOrder(user, accountId, orderId);
        }
    }
}
