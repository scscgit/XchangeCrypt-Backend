using System.ComponentModel.DataAnnotations;
using IO.Swagger.Attributes;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using XchangeCrypt.Backend.ConvergenceService.Services;

namespace IO.Swagger.Controllers
{
    /// <inheritdoc />
    /// <summary>
    /// Trading panel bridge for broker data, only mappings not covered by other controllers.
    /// </summary>
    [Area("Trading")]
    [Route("api/v1/trading/")]
    public sealed class TradingPanelBrokerDataBridge : Controller
    {
        private readonly CommandService _commandService;

        /// <summary>
        /// </summary>
        public TradingPanelBrokerDataBridge(CommandService commandService)
        {
            _commandService = commandService;
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Bars request. You can find examples in the [documentation](https://github.com/tradingview/charting_library/wiki/UDF#bars).</remarks>
        /// <param name="symbol">Symbol name or ticker</param>
        /// <param name="resolution">Symbol resolution. Possible resolutions are daily (&#x60;1D&#x60;, &#x60;2D&#x60; ... ), weekly (&#x60;1W&#x60;, &#x60;2W&#x60; ...), monthly (&#x60;1M&#x60;, &#x60;2M&#x60;...) and an intra-day resolution &amp;ndash; minutes(&#x60;1&#x60;, &#x60;2&#x60; ...).</param>
        /// <param name="from">Unix timestamp (UTC) of the leftmost required bar, including &#x60;from&#x60;.</param>
        /// <param name="to">Unix timestamp (UTC) of the rightmost required bar, including &#x60;to&#x60;.</param>
        /// <param name="countback">Number of bars (higher priority than &#x60;from&#x60;) starting with &#x60;to&#x60;. If &#x60;countback&#x60; is set, &#x60;from&#x60; should be ignorred. It is used only by tradingview.com, Trading Terminal will never use it.</param>
        /// <response code="200">Response is expected to be an object with properties listed below. Each property is treated as a table column</response>
        [HttpGet]
        [Route("history")]
        [ValidateModelState]
        [SwaggerOperation("HistoryGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(BarsArrays),
            description:
            "Response is expected to be an object with properties listed below. Each property is treated as a table column")]
        public IActionResult HistoryGet([FromQuery] [Required] string symbol,
            [FromQuery] [Required] string resolution, [FromQuery] [Required] decimal? from,
            [FromQuery] [Required] decimal? to, [FromQuery] decimal? countback)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(BarsArrays));

            string exampleJson = null;
            exampleJson =
                "{\n  \"s\" : \"ok\",\n  \"c\" : [ 2.3021358869347654518833223846741020679473876953125, 2.3021358869347654518833223846741020679473876953125 ],\n  \"nb\" : 0.80082819046101150206595775671303272247314453125,\n  \"t\" : [ 6.02745618307040320615897144307382404804229736328125, 6.02745618307040320615897144307382404804229736328125 ],\n  \"v\" : [ 7.061401241503109105224211816675961017608642578125, 7.061401241503109105224211816675961017608642578125 ],\n  \"h\" : [ 5.962133916683182377482808078639209270477294921875, 5.962133916683182377482808078639209270477294921875 ],\n  \"errmsg\" : \"errmsg\",\n  \"l\" : [ 5.63737665663332876420099637471139430999755859375, 5.63737665663332876420099637471139430999755859375 ],\n  \"o\" : [ 1.46581298050294517310021547018550336360931396484375, 1.46581298050294517310021547018550336360931396484375 ]\n}";

            var example = exampleJson != null
                ? JsonConvert.DeserializeObject<BarsArrays>(exampleJson)
                : default(BarsArrays);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Stream of prices. Server constantly keeps the connection alive. If the connection is broken the server constantly tries to restore it. Transfer mode is &#39;chunked encoding&#39;. The data feed should set &#39;Transfer-Encoding: chunked&#39; and make sure that all intermediate proxies are set to use this mode. All messages are finished with &#39;\\n&#39;. Streaming data should contain real-time only. It shouldn&#39;t contain snapshots of data.</remarks>
        /// <response code="200">Data feed should provide ticks (trades, asks, bids) and daily bars.  If there is no trades the data feed should set trades to bids.  If there is only ask/bid implementation you must also set the trade (same as bid but it&#39;s size must be &gt;&#x3D; 1).  Size for trades is always &gt;&#x3D; 1 except for a correction. In case of correction size can be 0.  All times should be UNIX time UTC.  Daily bars are required if they cannot be built from ticks (has_dwm should be set to true in the symbol information).  Fields for asks, bids and trades: &#x60;id&#x60;, &#x60;p&#x60;, &#x60;s&#x60; (optional for asks and bids), &#x60;t&#x60;, &#x60;f&#x60;.  Fields for daily bars: &#x60;id&#x60;, &#x60;t&#x60;, &#x60;o&#x60;, &#x60;h&#x60;, &#x60;l&#x60;, &#x60;c&#x60;, &#x60;v&#x60;.  Messages: 1. trade &#x60;{\&quot;id\&quot;:\&quot;symbol\&quot;,\&quot;p\&quot;:price,\&quot;s\&quot;:size,\&quot;t\&quot;:time}&#x60; 2. ask &#x60;{\&quot;id\&quot;:\&quot;symbol\&quot;,\&quot;p\&quot;:price,\&quot;s\&quot;:size,\&quot;t\&quot;:time,\&quot;f\&quot;:\&quot;a\&quot;}&#x60; 3. bid &#x60;{\&quot;id\&quot;:\&quot;symbol\&quot;,\&quot;p\&quot;:price,\&quot;s\&quot;:size,\&quot;t\&quot;:time,\&quot;f\&quot;:\&quot;b\&quot;}&#x60; 4. daily bar &#x60;{\&quot;id\&quot;:\&quot;symbol\&quot;,\&quot;o\&quot;:open,\&quot;h\&quot;:high,\&quot;l\&quot;:low,\&quot;c\&quot;:close,\&quot;v\&quot;:volume,\&quot;t\&quot;:time,\&quot;f\&quot;:\&quot;d\&quot;}&#x60; </response>
        [HttpGet]
        [Route("streaming")]
        [ValidateModelState]
        [SwaggerOperation("StreamingGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(InlineResponse20014),
            description:
            "Data feed should provide ticks (trades, asks, bids) and daily bars. If there is no trades the data feed should set trades to bids. If there is only ask/bid implementation you must also set the trade (same as bid but it's size must be >= 1). Size for trades is always >= 1 except for a correction. In case of correction size can be 0. All times should be UNIX time UTC. Daily bars are required if they cannot be built from ticks (has_dwm should be set to true in the symbol information). Fields for asks, bids and trades: `id`, `p`, `s` (optional for asks and bids), `t`, `f`. Fields for daily bars: `id`, `t`, `o`, `h`, `l`, `c`, `v`. Messages: 1. trade `{\"id\":\"symbol\",\"p\":price,\"s\":size,\"t\":time}` 2. ask `{\"id\":\"symbol\",\"p\":price,\"s\":size,\"t\":time,\"f\":\"a\"}` 3. bid `{\"id\":\"symbol\",\"p\":price,\"s\":size,\"t\":time,\"f\":\"b\"}` 4. daily bar `{\"id\":\"symbol\",\"o\":open,\"h\":high,\"l\":low,\"c\":close,\"v\":volume,\"t\":time,\"f\":\"d\"}`")]
        public IActionResult StreamingGet()
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(InlineResponse20014));

            string exampleJson = null;
            exampleJson =
                "{\n  \"p\" : 0.80082819046101150206595775671303272247314453125,\n  \"s\" : 6.02745618307040320615897144307382404804229736328125,\n  \"c\" : 7.061401241503109105224211816675961017608642578125,\n  \"t\" : 1.46581298050294517310021547018550336360931396484375,\n  \"f\" : \"a\",\n  \"v\" : 9.301444243932575517419536481611430644989013671875,\n  \"h\" : 5.63737665663332876420099637471139430999755859375,\n  \"id\" : \"id\",\n  \"l\" : 2.3021358869347654518833223846741020679473876953125,\n  \"o\" : 5.962133916683182377482808078639209270477294921875\n}";

            var example = exampleJson != null
                ? JsonConvert.DeserializeObject<InlineResponse20014>(exampleJson)
                : default(InlineResponse20014);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Get a list of all instruments</remarks>
        /// <response code="200">List of instruments</response>
        [HttpGet]
        [Route("symbol_info")]
        [ValidateModelState]
        [SwaggerOperation("SymbolInfoGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(SymbolInfoArrays), description: "List of instruments")]
        public IActionResult SymbolInfoGet()
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(SymbolInfoArrays));

            string exampleJson = null;
            exampleJson =
                "{\n  \"symbol\" : [ \"symbol\", \"symbol\" ],\n  \"supported-resolutions\" : [ [ \"supported-resolutions\", \"supported-resolutions\" ], [ \"supported-resolutions\", \"supported-resolutions\" ] ],\n  \"ticker\" : [ \"ticker\", \"ticker\" ],\n  \"has-daily\" : [ true, true ],\n  \"minmov2\" : [ 6.02745618307040320615897144307382404804229736328125, 6.02745618307040320615897144307382404804229736328125 ],\n  \"has-weekly-and-monthly\" : [ true, true ],\n  \"timezone\" : [ \"timezone\", \"timezone\" ],\n  \"fractional\" : [ true, true ],\n  \"description\" : [ \"description\", \"description\" ],\n  \"intraday-multipliers\" : [ [ \"intraday-multipliers\", \"intraday-multipliers\" ], [ \"intraday-multipliers\", \"intraday-multipliers\" ] ],\n  \"type\" : [ \"type\", \"type\" ],\n  \"has-no-volume\" : [ true, true ],\n  \"exchange-listed\" : [ \"exchange-listed\", \"exchange-listed\" ],\n  \"has-intraday\" : [ true, true ],\n  \"exchange-traded\" : [ \"exchange-traded\", \"exchange-traded\" ],\n  \"minmovement\" : [ 0.80082819046101150206595775671303272247314453125, 0.80082819046101150206595775671303272247314453125 ],\n  \"pricescale\" : [ 1.46581298050294517310021547018550336360931396484375, 1.46581298050294517310021547018550336360931396484375 ],\n  \"session-regular\" : [ \"session-regular\", \"session-regular\" ]\n}";

            var example = exampleJson != null
                ? JsonConvert.DeserializeObject<SymbolInfoArrays>(exampleJson)
                : default(SymbolInfoArrays);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }
    }
}
