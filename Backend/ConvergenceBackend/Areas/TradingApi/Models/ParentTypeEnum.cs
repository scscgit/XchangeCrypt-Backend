using System.Runtime.Serialization;

namespace IO.Swagger.Models
{
    /// <summary>
    /// Type of order's parent. Should be set only for bracket orders.
    /// </summary>
    /// <value>Type of order's parent. Should be set only for bracket orders.</value>
    public enum ParentTypeEnum
    {
        /// <summary>
        /// Enum OrderEnum for order
        /// </summary>
        [EnumMember(Value = "order")] OrderEnum = 1,

        /// <summary>
        /// Enum PositionEnum for position
        /// </summary>
        [EnumMember(Value = "position")] PositionEnum = 2
    }
}
