using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using IO.Swagger.Attributes;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace IO.Swagger.Controllers
{
    /// <inheritdoc />
    /// <summary>
    /// Trading terminal integration, only mappings not covered by other controllers.
    /// </summary>
    [Area("Trading")]
    [Route("api/v1/trading/")]
    public sealed class TradingTerminalIntegration : Controller
    {
        /// <summary>
        ///
        /// </summary>
        /// <remarks>Request for bar marks (circles on top of bars). You can display custom marks only in the Trading Terminal</remarks>
        /// <param name="symbol">Symbol name or ticker</param>
        /// <param name="resolution">Symbol resolution. Possible resolutions are daily (&#x60;1D&#x60;, &#x60;2D&#x60; ... ), weekly (&#x60;1W&#x60;, &#x60;2W&#x60; ...), monthly (&#x60;1M&#x60;, &#x60;2M&#x60;...) and an intra-day resolution &amp;ndash; minutes(&#x60;1&#x60;, &#x60;2&#x60; ...).</param>
        /// <param name="from">Unix timestamp (UTC) of the leftmost required bar, including &#x60;from&#x60;.</param>
        /// <param name="to">Unix timestamp (UTC) of the rightmost required bar, including &#x60;to&#x60;.</param>
        /// <response code="200">Response is expected to be an object with properties listed below. Each property is an array</response>
        [HttpGet]
        [Route("marks")]
        [ValidateModelState]
        [SwaggerOperation("MarksGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(MarksArrays),
            description:
            "Response is expected to be an object with properties listed below. Each property is an array")]
        public IActionResult MarksGet([FromQuery] [Required] string symbol,
            [FromQuery] [Required] string resolution, [FromQuery] [Required] decimal? from,
            [FromQuery] [Required] decimal? to)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(MarksArrays));

            string exampleJson = null;
            exampleJson =
                "{\n  \"color\" : [ \"color\", \"color\" ],\n  \"labelFontColor\" : [ \"labelFontColor\", \"labelFontColor\" ],\n  \"minSize\" : [ 1.46581298050294517310021547018550336360931396484375, 1.46581298050294517310021547018550336360931396484375 ],\n  \"id\" : [ 0.80082819046101150206595775671303272247314453125, 0.80082819046101150206595775671303272247314453125 ],\n  \"time\" : [ 6.02745618307040320615897144307382404804229736328125, 6.02745618307040320615897144307382404804229736328125 ],\n  \"text\" : [ \"text\", \"text\" ],\n  \"label\" : [ \"label\", \"label\" ]\n}";

            var example = exampleJson != null
                ? JsonConvert.DeserializeObject<MarksArrays>(exampleJson)
                : default(MarksArrays);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        ///
        /// </summary>
        /// <remarks>Request for timescale marks (circles on the timescale). You can display custom marks only in the Trading Terminal</remarks>
        /// <param name="symbol">Symbol name or ticker</param>
        /// <param name="resolution">Symbol resolution. Possible resolutions are daily (&#x60;1D&#x60;, &#x60;2D&#x60; ... ), weekly (&#x60;1W&#x60;, &#x60;2W&#x60; ...), monthly (&#x60;1M&#x60;, &#x60;2M&#x60;...) and an intra-day resolution &amp;ndash; minutes(&#x60;1&#x60;, &#x60;2&#x60; ...).</param>
        /// <param name="from">Unix timestamp (UTC) of the leftmost required bar, including &#x60;from&#x60;.</param>
        /// <param name="to">Unix timestamp (UTC) of the rightmost required bar, including &#x60;to&#x60;.</param>
        /// <response code="200">Response is expected to be an array.</response>
        [HttpGet]
        [Route("timescale_marks")]
        [ValidateModelState]
        [SwaggerOperation("TimescaleMarksGet")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<TimescaleMark>),
            description: "Response is expected to be an array.")]
        public IActionResult TimescaleMarksGet([FromQuery] [Required] string symbol,
            [FromQuery] [Required] string resolution, [FromQuery] [Required] decimal? from,
            [FromQuery] [Required] decimal? to)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(List<TimescaleMark>));

            string exampleJson = null;
            exampleJson =
                "[ {\n  \"color\" : \"red\",\n  \"tooltip\" : \"tooltip\",\n  \"id\" : \"id\",\n  \"time\" : 0.80082819046101150206595775671303272247314453125,\n  \"label\" : \"label\"\n}, {\n  \"color\" : \"red\",\n  \"tooltip\" : \"tooltip\",\n  \"id\" : \"id\",\n  \"time\" : 0.80082819046101150206595775671303272247314453125,\n  \"label\" : \"label\"\n} ]";

            var example = exampleJson != null
                ? JsonConvert.DeserializeObject<List<TimescaleMark>>(exampleJson)
                : default(List<TimescaleMark>);
            //TODO: Change the data returned
            return new ObjectResult(example);
        }
    }
}
