using IO.Swagger.Attributes;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using XchangeCrypt.Backend.ConvergenceBackend.Extensions.Authentication;
using XchangeCrypt.Backend.ConvergenceBackend.Services;
using static XchangeCrypt.Backend.ConstantsLibrary.MessagingConstants;

namespace IO.Swagger.Controllers
{
    /// <summary>
    /// Trading panel bridge for orders.
    /// </summary>
    [Area("TradingApi")]
    [Route("api/v1/tradingapi/")]
    public class TradingPanelBridgeOrdersApi : Controller
    {
        private readonly OrderService _orderService;
        private readonly UserService _userService;

        /// <summary>
        /// </summary>
        public TradingPanelBridgeOrdersApi(UserService userService, OrderService orderService)
        {
            _orderService = orderService;
            _userService = userService;
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get a list of executions (i.e. fills or trades) for an account and an instrument. Executions are displayed on a chart</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <param name="instrument">Broker instrument name</param>
        /// <param name="maxCount">Maximum count of executions to return</param>
        /// <response code="200">List of executions</response>
        [HttpGet]
        [Route("accounts/{accountId}/executions")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdExecutionsGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse20010), description: "List of executions")]
        public virtual IActionResult AccountsAccountIdExecutionsGet([FromRoute][Required]string accountId, [FromQuery][Required()]string instrument, [FromQuery]decimal? maxCount)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse20010));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : [ {\n    \"side\" : \"buy\",\n    \"price\" : 0.80082819046101150206595775671303272247314453125,\n    \"qty\" : 1.46581298050294517310021547018550336360931396484375,\n    \"instrument\" : \"instrument\",\n    \"id\" : \"id\",\n    \"time\" : 6.02745618307040320615897144307382404804229736328125\n  }, {\n    \"side\" : \"buy\",\n    \"price\" : 0.80082819046101150206595775671303272247314453125,\n    \"qty\" : 1.46581298050294517310021547018550336360931396484375,\n    \"instrument\" : \"instrument\",\n    \"id\" : \"id\",\n    \"time\" : 6.02745618307040320615897144307382404804229736328125\n  } ],\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse20010>(exampleJson)
            : default(InlineResponse20010);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get a list of tradeable instruments that are available for trading with the account specified</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <response code="200">List of instruments</response>
        [HttpGet]
        [Route("accounts/{accountId}/instruments")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdInstrumentsGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse20011), description: "List of instruments")]
        public virtual IActionResult AccountsAccountIdInstrumentsGet([FromRoute][Required]string accountId)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse20011));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : [ {\n    \"qtyStep\" : 1.46581298050294517310021547018550336360931396484375,\n    \"pipSize\" : 5.962133916683182377482808078639209270477294921875,\n    \"pipValue\" : 5.63737665663332876420099637471139430999755859375,\n    \"minTick\" : 2.3021358869347654518833223846741020679473876953125,\n    \"lotSize\" : 7.061401241503109105224211816675961017608642578125,\n    \"name\" : \"name\",\n    \"description\" : \"description\",\n    \"maxQty\" : 6.02745618307040320615897144307382404804229736328125,\n    \"minQty\" : 0.80082819046101150206595775671303272247314453125\n  }, {\n    \"qtyStep\" : 1.46581298050294517310021547018550336360931396484375,\n    \"pipSize\" : 5.962133916683182377482808078639209270477294921875,\n    \"pipValue\" : 5.63737665663332876420099637471139430999755859375,\n    \"minTick\" : 2.3021358869347654518833223846741020679473876953125,\n    \"lotSize\" : 7.061401241503109105224211816675961017608642578125,\n    \"name\" : \"name\",\n    \"description\" : \"description\",\n    \"maxQty\" : 6.02745618307040320615897144307382404804229736328125,\n    \"minQty\" : 0.80082819046101150206595775671303272247314453125\n  } ],\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse20011>(exampleJson)
            : default(InlineResponse20011);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get pending orders for an account.</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <response code="200">List of pending orders. It is also expected that broker returns orders filled/cancelled/rejected during current session.</response>
        [HttpGet]
        [Route("accounts/{accountId}/orders")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdOrdersGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2004), description: "List of pending orders. It is also expected that broker returns orders filled/cancelled/rejected during current session.")]
        public virtual IActionResult AccountsAccountIdOrdersGet([FromRoute][Required]string accountId)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2004));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : [ {\n    \"side\" : \"buy\",\n    \"limitPrice\" : 5.962133916683182377482808078639209270477294921875,\n    \"avgPrice\" : 1.46581298050294517310021547018550336360931396484375,\n    \"instrument\" : \"instrument\",\n    \"type\" : \"market\",\n    \"parentId\" : \"parentId\",\n    \"parentType\" : \"order\",\n    \"duration\" : {\n      \"datetime\" : 2.3021358869347654518833223846741020679473876953125,\n      \"type\" : \"type\"\n    },\n    \"stopPrice\" : 5.63737665663332876420099637471139430999755859375,\n    \"qty\" : 0.80082819046101150206595775671303272247314453125,\n    \"id\" : \"id\",\n    \"filledQty\" : 6.02745618307040320615897144307382404804229736328125,\n    \"status\" : \"placing\"\n  }, {\n    \"side\" : \"buy\",\n    \"limitPrice\" : 5.962133916683182377482808078639209270477294921875,\n    \"avgPrice\" : 1.46581298050294517310021547018550336360931396484375,\n    \"instrument\" : \"instrument\",\n    \"type\" : \"market\",\n    \"parentId\" : \"parentId\",\n    \"parentType\" : \"order\",\n    \"duration\" : {\n      \"datetime\" : 2.3021358869347654518833223846741020679473876953125,\n      \"type\" : \"type\"\n    },\n    \"stopPrice\" : 5.63737665663332876420099637471139430999755859375,\n    \"qty\" : 0.80082819046101150206595775671303272247314453125,\n    \"id\" : \"id\",\n    \"filledQty\" : 6.02745618307040320615897144307382404804229736328125,\n    \"status\" : \"placing\"\n  } ],\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2004>(exampleJson)
            : default(InlineResponse2004);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get order history for an account. It is expected that returned orders will have a final status (rejected, filled, cancelled). This request is optional. If you don&#39;t support history of orders set &#x60;AccountFlags::supportOrdersHistory&#x60; to &#x60;false&#x60;.</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <param name="maxCount">Maximum amount of orders to return</param>
        /// <response code="200">List of orders</response>
        [HttpGet]
        [Route("accounts/{accountId}/ordersHistory")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdOrdersHistoryGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2004), description: "List of orders")]
        public virtual IActionResult AccountsAccountIdOrdersHistoryGet([FromRoute][Required]string accountId, [FromQuery]decimal? maxCount)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2004));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : [ {\n    \"side\" : \"buy\",\n    \"limitPrice\" : 5.962133916683182377482808078639209270477294921875,\n    \"avgPrice\" : 1.46581298050294517310021547018550336360931396484375,\n    \"instrument\" : \"instrument\",\n    \"type\" : \"market\",\n    \"parentId\" : \"parentId\",\n    \"parentType\" : \"order\",\n    \"duration\" : {\n      \"datetime\" : 2.3021358869347654518833223846741020679473876953125,\n      \"type\" : \"type\"\n    },\n    \"stopPrice\" : 5.63737665663332876420099637471139430999755859375,\n    \"qty\" : 0.80082819046101150206595775671303272247314453125,\n    \"id\" : \"id\",\n    \"filledQty\" : 6.02745618307040320615897144307382404804229736328125,\n    \"status\" : \"placing\"\n  }, {\n    \"side\" : \"buy\",\n    \"limitPrice\" : 5.962133916683182377482808078639209270477294921875,\n    \"avgPrice\" : 1.46581298050294517310021547018550336360931396484375,\n    \"instrument\" : \"instrument\",\n    \"type\" : \"market\",\n    \"parentId\" : \"parentId\",\n    \"parentType\" : \"order\",\n    \"duration\" : {\n      \"datetime\" : 2.3021358869347654518833223846741020679473876953125,\n      \"type\" : \"type\"\n    },\n    \"stopPrice\" : 5.63737665663332876420099637471139430999755859375,\n    \"qty\" : 0.80082819046101150206595775671303272247314453125,\n    \"id\" : \"id\",\n    \"filledQty\" : 6.02745618307040320615897144307382404804229736328125,\n    \"status\" : \"placing\"\n  } ],\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2004>(exampleJson)
            : default(InlineResponse2004);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Cancel an existing order</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <param name="orderId">Order ID</param>
        /// <response code="200">OK</response>
        [HttpDelete]
        [Route("accounts/{accountId}/orders/{orderId}")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdOrdersOrderIdDelete")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2007), description: "OK")]
        public virtual IActionResult AccountsAccountIdOrdersOrderIdDelete([FromRoute][Required]string accountId, [FromRoute][Required]string orderId)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2007));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2007>(exampleJson)
            : default(InlineResponse2007);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get an order for an account. It can be an active or historical order.</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <param name="orderId">Order ID</param>
        /// <response code="200">Order</response>
        [HttpGet]
        [Route("accounts/{accountId}/orders/{orderId}")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdOrdersOrderIdGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2006), description: "Order")]
        public virtual IActionResult AccountsAccountIdOrdersOrderIdGet([FromRoute][Required]string accountId, [FromRoute][Required]string orderId)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2006));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : {\n    \"side\" : \"buy\",\n    \"limitPrice\" : 5.962133916683182377482808078639209270477294921875,\n    \"avgPrice\" : 1.46581298050294517310021547018550336360931396484375,\n    \"instrument\" : \"instrument\",\n    \"type\" : \"market\",\n    \"parentId\" : \"parentId\",\n    \"parentType\" : \"order\",\n    \"duration\" : {\n      \"datetime\" : 2.3021358869347654518833223846741020679473876953125,\n      \"type\" : \"type\"\n    },\n    \"stopPrice\" : 5.63737665663332876420099637471139430999755859375,\n    \"qty\" : 0.80082819046101150206595775671303272247314453125,\n    \"id\" : \"id\",\n    \"filledQty\" : 6.02745618307040320615897144307382404804229736328125,\n    \"status\" : \"placing\"\n  },\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2006>(exampleJson)
            : default(InlineResponse2006);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Modify an existing order</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <param name="orderId">Order ID</param>
        /// <param name="qty">Number of units</param>
        /// <param name="limitPrice">Limit Price for Limit or StopLimit order</param>
        /// <param name="stopPrice">Stop Price for Stop or StopLimit order</param>
        /// <param name="stopLoss">StopLoss price (if supported)</param>
        /// <param name="takeProfit">TakeProfit price (if supported)</param>
        /// <param name="digitalSignature">Digital signature (if supported)</param>
        /// <response code="200">OK</response>
        [HttpPut]
        [Route("accounts/{accountId}/orders/{orderId}")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdOrdersOrderIdPut")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2007), description: "OK")]
        public virtual IActionResult AccountsAccountIdOrdersOrderIdPut([FromRoute][Required]string accountId, [FromRoute][Required]string orderId, [FromForm][Required()]decimal? qty, [FromForm]decimal? limitPrice, [FromForm]decimal? stopPrice, [FromForm]decimal? stopLoss, [FromForm]decimal? takeProfit, [FromForm]string digitalSignature)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2007));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2007>(exampleJson)
            : default(InlineResponse2007);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Create a new order</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <param name="instrument">Instrument to open the order on</param>
        /// <param name="qty">The number of units to open order for</param>
        /// <param name="side">Side. Possible values &amp;ndash; &#x60;buy&#x60; and &#x60;sell&#x60;.</param>
        /// <param name="type">Type. Possible values &amp;ndash; &#x60;market&#x60;, &#x60;stop&#x60;, &#x60;limit&#x60;, &#x60;stoplimit&#x60;.</param>
        /// <param name="limitPrice">Limit Price for Limit or StopLimit order</param>
        /// <param name="stopPrice">Stop Price for Stop or StopLimit order</param>
        /// <param name="durationType">Duration ID (if supported)</param>
        /// <param name="durationDateTime">Expiration datetime UNIX timestamp (if supported by duration type)</param>
        /// <param name="stopLoss">StopLoss price (if supported)</param>
        /// <param name="takeProfit">TakeProfit price (if supported)</param>
        /// <param name="digitalSignature">Digital signature (if supported)</param>
        /// <param name="requestId">Unique identifier for a request</param>
        /// <response code="200">Status. &#x60;message&#x60; should be filled if erroneous. &#x60;orderId&#x60; should present if successful.</response>
        [HttpPost]
        [Route("accounts/{accountId}/orders")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdOrdersPost")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2005), description: "Status. &#x60;message&#x60; should be filled if erroneous. &#x60;orderId&#x60; should present if successful.")]
        [Authorize]
        public virtual IActionResult AccountsAccountIdOrdersPost(
            [FromRoute][Required]string accountId,
            [FromForm][Required]string instrument,
            [FromForm][Required]decimal? qty,
            [FromForm][Required]string side,
            [FromForm][Required]string type,
            [FromForm]decimal? limitPrice,
            [FromForm]decimal? stopPrice,
            [FromForm]string durationType,
            [FromForm]decimal? durationDateTime,
            [FromForm]decimal? stopLoss,
            [FromForm]decimal? takeProfit,
            [FromForm]string digitalSignature,
            [FromQuery]string requestId)
        {
            Task orderTask;
            try
            {
                var user = User.GetIdentifier();
                switch (type)
                {
                    case OrderTypes.MarketOrder:
                        orderTask = _orderService.CreateMarketOrder(
                            user, accountId, instrument, qty, side, durationType, durationDateTime, stopLoss, takeProfit, requestId);
                        break;

                    case OrderTypes.StopOrder:
                        orderTask = _orderService.CreateStopOrder(
                            user, accountId, instrument, qty, side, stopPrice, durationType, durationDateTime, stopLoss, takeProfit, requestId);
                        break;

                    case OrderTypes.LimitOrder:
                        orderTask = _orderService.CreateLimitOrder(
                            user, accountId, instrument, qty, side, limitPrice, durationType, durationDateTime, stopLoss, takeProfit, requestId);
                        break;

                    case "stoplimit":
                        return StatusCode(
                            400,
                            new InlineResponse2005()
                            {
                                S = Status.ErrorEnum,
                                Errmsg = "Invalid type parameter: stoplimit not supported yet"
                            }
                        );

                    default:
                        return StatusCode(
                            400,
                            new InlineResponse2005()
                            {
                                S = Status.ErrorEnum,
                                Errmsg = "Invalid type parameter"
                            }
                        );
                }
                // Finish sending in order to catch exceptions
                orderTask.Wait();
            }
            catch (Exception e)
            {
                return StatusCode(
                    500,
                    new InlineResponse2005()
                    {
                        S = Status.ErrorEnum,
                        Errmsg = $"Internal error occurred: {e.Message}\n{e.StackTrace}"
                    }
                );
            }
            return StatusCode(
                200,
                new InlineResponse2005()
                {
                    S = Status.OkEnum,
                    D = new InlineResponse2005D()
                    {
                        // Currently the OrderId is not supported, as the request is asynchronous
                        OrderId = requestId
                    }
                }
            );
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get positions for an account</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <response code="200">Array of positions</response>
        [HttpGet]
        [Route("accounts/{accountId}/positions")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdPositionsGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2008), description: "Array of positions")]
        public virtual IActionResult AccountsAccountIdPositionsGet([FromRoute][Required]string accountId)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2008));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : [ {\n    \"side\" : \"buy\",\n    \"unrealizedPl\" : 1.46581298050294517310021547018550336360931396484375,\n    \"avgPrice\" : 6.02745618307040320615897144307382404804229736328125,\n    \"qty\" : 0.80082819046101150206595775671303272247314453125,\n    \"instrument\" : \"instrument\",\n    \"id\" : \"id\"\n  }, {\n    \"side\" : \"buy\",\n    \"unrealizedPl\" : 1.46581298050294517310021547018550336360931396484375,\n    \"avgPrice\" : 6.02745618307040320615897144307382404804229736328125,\n    \"qty\" : 0.80082819046101150206595775671303272247314453125,\n    \"instrument\" : \"instrument\",\n    \"id\" : \"id\"\n  } ],\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2008>(exampleJson)
            : default(InlineResponse2008);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Close an existing position</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <param name="positionId">Position ID</param>
        /// <response code="200">OK</response>
        [HttpDelete]
        [Route("accounts/{accountId}/positions/{positionId}")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdPositionsPositionIdDelete")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2007), description: "OK")]
        public virtual IActionResult AccountsAccountIdPositionsPositionIdDelete([FromRoute][Required]string accountId, [FromRoute][Required]string positionId)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2007));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2007>(exampleJson)
            : default(InlineResponse2007);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get a position for an account</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <param name="positionId">Position ID</param>
        /// <response code="200">Position object</response>
        [HttpGet]
        [Route("accounts/{accountId}/positions/{positionId}")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdPositionsPositionIdGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2009), description: "Position object")]
        public virtual IActionResult AccountsAccountIdPositionsPositionIdGet([FromRoute][Required]string accountId, [FromRoute][Required]string positionId)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2009));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : {\n    \"side\" : \"buy\",\n    \"unrealizedPl\" : 1.46581298050294517310021547018550336360931396484375,\n    \"avgPrice\" : 6.02745618307040320615897144307382404804229736328125,\n    \"qty\" : 0.80082819046101150206595775671303272247314453125,\n    \"instrument\" : \"instrument\",\n    \"id\" : \"id\"\n  },\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2009>(exampleJson)
            : default(InlineResponse2009);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Modify an existing position stop loss or take profit or both</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <param name="positionId">Position ID</param>
        /// <param name="stopLoss">StopLoss price</param>
        /// <param name="takeProfit">TakeProfit price</param>
        /// <response code="200">OK</response>
        [HttpPut]
        [Route("accounts/{accountId}/positions/{positionId}")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdPositionsPositionIdPut")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2007), description: "OK")]
        public virtual IActionResult AccountsAccountIdPositionsPositionIdPut([FromRoute][Required]string accountId, [FromRoute][Required]string positionId, [FromForm]decimal? stopLoss, [FromForm]decimal? takeProfit)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2007));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2007>(exampleJson)
            : default(InlineResponse2007);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get account information.</remarks>
        /// <param name="accountId">The account identifier</param>
        /// <param name="locale">Locale (language) id</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Route("accounts/{accountId}/state")]
        [ValidateModelState]
        [SwaggerOperation("AccountsAccountIdStateGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2003), description: "OK")]
        public virtual IActionResult AccountsAccountIdStateGet([FromRoute][Required]string accountId, [FromQuery][Required()]string locale)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2003));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : {\n    \"amData\" : [ [ [ \"amData\", \"amData\" ], [ \"amData\", \"amData\" ] ], [ [ \"amData\", \"amData\" ], [ \"amData\", \"amData\" ] ] ],\n    \"unrealizedPl\" : 6.02745618307040320615897144307382404804229736328125,\n    \"balance\" : 0.80082819046101150206595775671303272247314453125,\n    \"equity\" : 1.46581298050294517310021547018550336360931396484375\n  },\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2003>(exampleJson)
            : default(InlineResponse2003);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get a list of accounts owned by the user</remarks>
        /// <response code="200">Accounts list</response>
        [HttpGet]
        [Route("accounts")]
        [ValidateModelState]
        [SwaggerOperation("AccountsGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2002), description: "Accounts list")]
        public virtual IActionResult AccountsGet()
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2002));
            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : [ {\n    \"currencySign\" : \"currencySign\",\n    \"name\" : \"name\",\n    \"currency\" : \"currency\",\n    \"id\" : \"id\",\n    \"config\" : {\n      \"supportLevel2Data\" : true,\n      \"supportBrackets\" : true,\n      \"supportPLUpdate\" : true,\n      \"supportClosePosition\" : true,\n      \"supportExecutions\" : true,\n      \"supportEditAmount\" : true,\n      \"supportPositionBrackets\" : true,\n      \"supportReducePosition\" : true,\n      \"showQuantityInsteadOfAmount\" : true,\n      \"supportOrderBrackets\" : true,\n      \"supportDigitalSignature\" : true,\n      \"supportStopLimitOrders\" : true,\n      \"supportMultiposition\" : true,\n      \"supportDOM\" : true,\n      \"supportOrdersHistory\" : true\n    }\n  }, {\n    \"currencySign\" : \"currencySign\",\n    \"name\" : \"name\",\n    \"currency\" : \"currency\",\n    \"id\" : \"id\",\n    \"config\" : {\n      \"supportLevel2Data\" : true,\n      \"supportBrackets\" : true,\n      \"supportPLUpdate\" : true,\n      \"supportClosePosition\" : true,\n      \"supportExecutions\" : true,\n      \"supportEditAmount\" : true,\n      \"supportPositionBrackets\" : true,\n      \"supportReducePosition\" : true,\n      \"showQuantityInsteadOfAmount\" : true,\n      \"supportOrderBrackets\" : true,\n      \"supportDigitalSignature\" : true,\n      \"supportStopLimitOrders\" : true,\n      \"supportMultiposition\" : true,\n      \"supportDOM\" : true,\n      \"supportOrdersHistory\" : true\n    }\n  } ],\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2002>(exampleJson)
            : default(InlineResponse2002);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Oauth2 Password authorization</remarks>
        /// <param name="login">User Login</param>
        /// <param name="password">User Password</param>
        /// <response code="200">Access Token. TradingView will set Authorization header to &#39;Bearer &#39; + access_token for all requests with authorization.</response>
        [HttpPost]
        [Route("authorize")]
        [ValidateModelState]
        [SwaggerOperation("AuthorizePost")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse200), description: "Access Token. TradingView will set Authorization header to &#39;Bearer &#39; + access_token for all requests with authorization.")]
        public virtual IActionResult AuthorizePost([FromForm][Required()]string login, [FromForm][Required()]string password)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse200));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : {\n    \"access_token\" : \"access_token\",\n    \"expiration\" : 0.80082819046101150206595775671303272247314453125\n  },\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse200>(exampleJson)
            : default(InlineResponse200);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get localized configuration</remarks>
        /// <param name="locale">Locale (language) id</param>
        /// <response code="200">Configuration</response>
        [HttpGet]
        [Route("config")]
        [ValidateModelState]
        [SwaggerOperation("ConfigGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse2001), description: "Configuration")]
        public virtual IActionResult ConfigGet([FromQuery][Required()]string locale)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse2001));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : {\n    \"pullingInterval\" : {\n      \"accountManager\" : 5.63737665663332876420099637471139430999755859375,\n      \"orders\" : 1.46581298050294517310021547018550336360931396484375,\n      \"positions\" : 5.962133916683182377482808078639209270477294921875,\n      \"history\" : 0.80082819046101150206595775671303272247314453125,\n      \"quotes\" : 6.02745618307040320615897144307382404804229736328125\n    },\n    \"accountManager\" : [ {\n      \"columns\" : [ {\n        \"fixedWidth\" : true,\n        \"tooltip\" : \"tooltip\",\n        \"id\" : \"id\",\n        \"sortable\" : true,\n        \"title\" : \"title\"\n      }, {\n        \"fixedWidth\" : true,\n        \"tooltip\" : \"tooltip\",\n        \"id\" : \"id\",\n        \"sortable\" : true,\n        \"title\" : \"title\"\n      } ],\n      \"id\" : \"id\",\n      \"title\" : \"title\"\n    }, {\n      \"columns\" : [ {\n        \"fixedWidth\" : true,\n        \"tooltip\" : \"tooltip\",\n        \"id\" : \"id\",\n        \"sortable\" : true,\n        \"title\" : \"title\"\n      }, {\n        \"fixedWidth\" : true,\n        \"tooltip\" : \"tooltip\",\n        \"id\" : \"id\",\n        \"sortable\" : true,\n        \"title\" : \"title\"\n      } ],\n      \"id\" : \"id\",\n      \"title\" : \"title\"\n    } ],\n    \"durations\" : [ {\n      \"hasTimePicker\" : true,\n      \"hasDatePicker\" : true,\n      \"id\" : \"id\",\n      \"title\" : \"title\"\n    }, {\n      \"hasTimePicker\" : true,\n      \"hasDatePicker\" : true,\n      \"id\" : \"id\",\n      \"title\" : \"title\"\n    } ]\n  },\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse2001>(exampleJson)
            : default(InlineResponse2001);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get current depth of market for the instrument. Optional.</remarks>
        /// <param name="symbol">instrument name</param>
        /// <response code="200">Depth of market</response>
        [HttpGet]
        [Route("depth")]
        [ValidateModelState]
        [SwaggerOperation("DepthGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse20013), description: "Depth of market")]
        public virtual IActionResult DepthGet([FromQuery][Required()]string symbol)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse20013));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : {\n    \"asks\" : [ \"\", \"\" ],\n    \"bids\" : [ \"\", \"\" ]\n  },\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse20013>(exampleJson)
            : default(InlineResponse20013);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Return all broker instruments with corresponding TradingView instruments. It is required to add a Broker to TradingView.com. It is not required for Trading Terminal integration. This request works without authorization!</remarks>
        /// <response code="200">Broker &amp;ndash; TradingView instruments map</response>
        [HttpGet]
        [Route("mapping")]
        [ValidateModelState]
        [SwaggerOperation("MappingGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(SymbolMapping), description: "Broker &amp;ndash; TradingView instruments map")]
        public virtual IActionResult MappingGet()
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(SymbolMapping));

            string exampleJson = null;
            exampleJson = "{\n  \"fields\" : [ \"brokerSymbol\" ],\n  \"symbols\" : [ {\n    \"s\" : \"s\",\n    \"f\" : [ \"f\", \"f\" ]\n  }, {\n    \"s\" : \"s\",\n    \"f\" : [ \"f\", \"f\" ]\n  } ]\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<SymbolMapping>(exampleJson)
            : default(SymbolMapping);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get current prices of the instrument. You can see an example of this response [here](https://demo_feed.tradingview.com/quotes?symbols&#x3D;AAPL%2CMSFT%2CIBM%2CNasdaqNM%3AAAPL).</remarks>
        /// <param name="symbols">comma separated symbols</param>
        /// <response code="200">Current prices</response>
        [HttpGet]
        [Route("quotes")]
        [ValidateModelState]
        [SwaggerOperation("QuotesGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse20012), description: "Current prices")]
        public virtual IActionResult QuotesGet([FromQuery][Required()]string symbols)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse20012));

            string exampleJson = null;
            exampleJson = "{\n  \"s\" : \"ok\",\n  \"d\" : [ {\n    \"s\" : null,\n    \"v\" : {\n      \"volume\" : 2.027123023002321833274663731572218239307403564453125,\n      \"lp\" : 1.46581298050294517310021547018550336360931396484375,\n      \"ch\" : 0.80082819046101150206595775671303272247314453125,\n      \"ask\" : 5.962133916683182377482808078639209270477294921875,\n      \"high_price\" : 7.061401241503109105224211816675961017608642578125,\n      \"chp\" : 6.02745618307040320615897144307382404804229736328125,\n      \"bid\" : 5.63737665663332876420099637471139430999755859375,\n      \"open_price\" : 2.3021358869347654518833223846741020679473876953125,\n      \"low_price\" : 9.301444243932575517419536481611430644989013671875,\n      \"prev_close_price\" : 3.61607674925191080461672754609026014804840087890625\n    },\n    \"n\" : \"n\"\n  }, {\n    \"s\" : null,\n    \"v\" : {\n      \"volume\" : 2.027123023002321833274663731572218239307403564453125,\n      \"lp\" : 1.46581298050294517310021547018550336360931396484375,\n      \"ch\" : 0.80082819046101150206595775671303272247314453125,\n      \"ask\" : 5.962133916683182377482808078639209270477294921875,\n      \"high_price\" : 7.061401241503109105224211816675961017608642578125,\n      \"chp\" : 6.02745618307040320615897144307382404804229736328125,\n      \"bid\" : 5.63737665663332876420099637471139430999755859375,\n      \"open_price\" : 2.3021358869347654518833223846741020679473876953125,\n      \"low_price\" : 9.301444243932575517419536481611430644989013671875,\n      \"prev_close_price\" : 3.61607674925191080461672754609026014804840087890625\n    },\n    \"n\" : \"n\"\n  } ],\n  \"errmsg\" : \"errmsg\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<InlineResponse20012>(exampleJson)
            : default(InlineResponse20012);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }
    }
}