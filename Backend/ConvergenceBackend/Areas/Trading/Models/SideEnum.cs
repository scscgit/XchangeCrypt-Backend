using System.Runtime.Serialization;

namespace IO.Swagger.Models
{
    /// <summary>
    /// Side. Possible values &ndash; \"buy\" and \"sell\".
    /// </summary>
    /// <value>Side. Possible values &ndash; \"buy\" and \"sell\".</value>
    public enum SideEnum
    {
        /// <summary>
        /// Enum BuyEnum for buy
        /// </summary>
        [EnumMember(Value = "buy")] BuyEnum = 1,

        /// <summary>
        /// Enum SellEnum for sell
        /// </summary>
        [EnumMember(Value = "sell")] SellEnum = 2
    }
}
