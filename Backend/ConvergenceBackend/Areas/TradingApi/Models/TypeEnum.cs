using System.Runtime.Serialization;

namespace IO.Swagger.Models
{
    /// <summary>
    /// Type. Possible values &ndash; \"market\", \"stop\", \"limit\", \"stoplimit\".
    /// </summary>
    /// <value>Type. Possible values &ndash; \"market\", \"stop\", \"limit\", \"stoplimit\".</value>
    public enum TypeEnum
    {
        /// <summary>
        /// Enum MarketEnum for market
        /// </summary>
        [EnumMember(Value = "market")]
        MarketEnum = 1,

        /// <summary>
        /// Enum StopEnum for stop
        /// </summary>
        [EnumMember(Value = "stop")]
        StopEnum = 2,

        /// <summary>
        /// Enum LimitEnum for limit
        /// </summary>
        [EnumMember(Value = "limit")]
        LimitEnum = 3,

        /// <summary>
        /// Enum StoplimitEnum for stoplimit
        /// </summary>
        [EnumMember(Value = "stoplimit")]
        StoplimitEnum = 4
    }
}
