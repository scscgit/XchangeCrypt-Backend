/*
 * TradingView REST API Specification for Brokers
 *
 * No description provided (generated by Swagger Codegen https://github.com/swagger-api/swagger-codegen)
 *
 *
 *
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace IO.Swagger.Models
{
    /// <summary>
    ///
    /// </summary>
    [DataContract]
    public partial class Execution : IEquatable<Execution>
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        /// <value>Unique identifier</value>
        [Required]
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Instrument id
        /// </summary>
        /// <value>Instrument id</value>
        [Required]
        [DataMember(Name = "instrument")]
        public string Instrument { get; set; }

        /// <summary>
        /// Execution price
        /// </summary>
        /// <value>Execution price</value>
        [Required]
        [DataMember(Name = "price")]
        public decimal? Price { get; set; }

        /// <summary>
        /// Execution time
        /// </summary>
        /// <value>Execution time</value>
        [Required]
        [DataMember(Name = "time")]
        public decimal? Time { get; set; }

        /// <summary>
        /// Execution quantity
        /// </summary>
        /// <value>Execution quantity</value>
        [Required]
        [DataMember(Name = "qty")]
        public decimal? Qty { get; set; }

        /// <summary>
        /// Side. Possible values &amp;ndash; \&quot;buy\&quot; and \&quot;sell\&quot;.
        /// </summary>
        /// <value>Side. Possible values &amp;ndash; \&quot;buy\&quot; and \&quot;sell\&quot;.</value>
        [Required]
        [DataMember(Name = "side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SideEnum? Side { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Execution {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Instrument: ").Append(Instrument).Append("\n");
            sb.Append("  Price: ").Append(Price).Append("\n");
            sb.Append("  Time: ").Append(Time).Append("\n");
            sb.Append("  Qty: ").Append(Qty).Append("\n");
            sb.Append("  Side: ").Append(Side).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Execution) obj);
        }

        /// <summary>
        /// Returns true if Execution instances are equal
        /// </summary>
        /// <param name="other">Instance of Execution to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Execution other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    Id == other.Id ||
                    Id != null &&
                    Id.Equals(other.Id)
                ) &&
                (
                    Instrument == other.Instrument ||
                    Instrument != null &&
                    Instrument.Equals(other.Instrument)
                ) &&
                (
                    Price == other.Price ||
                    Price != null &&
                    Price.Equals(other.Price)
                ) &&
                (
                    Time == other.Time ||
                    Time != null &&
                    Time.Equals(other.Time)
                ) &&
                (
                    Qty == other.Qty ||
                    Qty != null &&
                    Qty.Equals(other.Qty)
                ) &&
                (
                    Side == other.Side ||
                    Side != null &&
                    Side.Equals(other.Side)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                // Suitable nullity checks etc, of course :)
                if (Id != null)
                    hashCode = hashCode * 59 + Id.GetHashCode();
                if (Instrument != null)
                    hashCode = hashCode * 59 + Instrument.GetHashCode();
                if (Price != null)
                    hashCode = hashCode * 59 + Price.GetHashCode();
                if (Time != null)
                    hashCode = hashCode * 59 + Time.GetHashCode();
                if (Qty != null)
                    hashCode = hashCode * 59 + Qty.GetHashCode();
                if (Side != null)
                    hashCode = hashCode * 59 + Side.GetHashCode();
                return hashCode;
            }
        }

        #region Operators

#pragma warning disable 1591

        public static bool operator ==(Execution left, Execution right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Execution left, Execution right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591

        #endregion Operators
    }
}
