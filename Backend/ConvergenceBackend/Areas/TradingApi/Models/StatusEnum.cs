using System.Runtime.Serialization;

namespace IO.Swagger.Models
{
    /// <summary>
    /// String constants to describe an order status.  `Status`  | Description - -- -- -- -- -|- -- -- -- -- -- -- placing   | order is not created on a broker's side yet inactive  | bracket order is created but waiting for a base order to be filled working   | order is created but not executed yet rejected  | order is rejected for some reason filled    | order is fully executed cancelled  | order is cancelled
    /// </summary>
    /// <value>String constants to describe an order status.  `Status`  | Description - -- -- -- -- -|- -- -- -- -- -- -- placing   | order is not created on a broker's side yet inactive  | bracket order is created but waiting for a base order to be filled working   | order is created but not executed yet rejected  | order is rejected for some reason filled    | order is fully executed cancelled  | order is cancelled </value>
    public enum StatusEnum
    {
        /// <summary>
        /// Enum PlacingEnum for placing
        /// </summary>
        [EnumMember(Value = "placing")]
        PlacingEnum = 1,

        /// <summary>
        /// Enum InactiveEnum for inactive
        /// </summary>
        [EnumMember(Value = "inactive")]
        InactiveEnum = 2,

        /// <summary>
        /// Enum WorkingEnum for working
        /// </summary>
        [EnumMember(Value = "working")]
        WorkingEnum = 3,

        /// <summary>
        /// Enum RejectedEnum for rejected
        /// </summary>
        [EnumMember(Value = "rejected")]
        RejectedEnum = 4,

        /// <summary>
        /// Enum FilledEnum for filled
        /// </summary>
        [EnumMember(Value = "filled")]
        FilledEnum = 5,

        /// <summary>
        /// Enum CancelledEnum for cancelled
        /// </summary>
        [EnumMember(Value = "cancelled")]
        CancelledEnum = 6
    }
}
