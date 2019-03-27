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

        [HttpGet]
        [Route("ordersHistory")]
        public List<Order> AccountsAccountIdOrdersHistoryGet(
            [FromQuery] [Required] string user,
            [FromQuery] [Required] string accountId,
            [FromQuery] int? maxCount)
        {
            return _orderCaching.GetOrdersHistory(user, accountId, maxCount);
        }

        [HttpGet]
        [Route("depth")]
        public Depth DepthGet(
            [FromQuery] [Required] string instrument)
        {
            return _orderCaching.GetDepth(instrument);
        }
    }
}
