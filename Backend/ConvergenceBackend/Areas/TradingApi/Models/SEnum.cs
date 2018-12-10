using System.Runtime.Serialization;

namespace IO.Swagger.Models
{
    /// <summary>
    /// Status code.
    /// </summary>
    /// <value>Status code.</value>
    public enum SEnum
    {
        /// <summary>
        /// Enum OkEnum for ok
        /// </summary>
        [EnumMember(Value = "ok")] OkEnum = 1,

        /// <summary>
        /// Enum ErrorEnum for error
        /// </summary>
        [EnumMember(Value = "error")] ErrorEnum = 2,

        /// <summary>
        /// Enum NoDataEnum for no_data
        /// </summary>
        [EnumMember(Value = "no_data")] NoDataEnum = 3
    }
}
