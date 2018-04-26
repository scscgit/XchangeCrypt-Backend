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
    public partial class InlineResponse2005 : IEquatable<InlineResponse2005>
    {
        /// <summary>
        /// Gets or Sets S
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// Enum OkEnum for ok
            /// </summary>
            [EnumMember(Value = "ok")]
            OkEnum = 1,

            /// <summary>
            /// Enum ErrorEnum for error
            /// </summary>
            [EnumMember(Value = "error")]
            ErrorEnum = 2
        }

        /// <summary>
        /// Gets or Sets S
        /// </summary>
        [Required]
        [DataMember(Name = "s")]
        public Status? S { get; set; }

        /// <summary>
        /// Gets or Sets Errmsg
        /// </summary>
        [DataMember(Name = "errmsg")]
        public string Errmsg { get; set; }

        /// <summary>
        /// Gets or Sets D
        /// </summary>
        [DataMember(Name = "d")]
        public InlineResponse2005D D { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class InlineResponse2005 {\n");
            sb.Append("  S: ").Append(S).Append("\n");
            sb.Append("  Errmsg: ").Append(Errmsg).Append("\n");
            sb.Append("  D: ").Append(D).Append("\n");
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
            return obj.GetType() == GetType() && Equals((InlineResponse2005)obj);
        }

        /// <summary>
        /// Returns true if InlineResponse2005 instances are equal
        /// </summary>
        /// <param name="other">Instance of InlineResponse2005 to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(InlineResponse2005 other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    S == other.S ||
                    S != null &&
                    S.Equals(other.S)
                ) &&
                (
                    Errmsg == other.Errmsg ||
                    Errmsg != null &&
                    Errmsg.Equals(other.Errmsg)
                ) &&
                (
                    D == other.D ||
                    D != null &&
                    D.Equals(other.D)
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
                if (S != null)
                    hashCode = hashCode * 59 + S.GetHashCode();
                if (Errmsg != null)
                    hashCode = hashCode * 59 + Errmsg.GetHashCode();
                if (D != null)
                    hashCode = hashCode * 59 + D.GetHashCode();
                return hashCode;
            }
        }

        #region Operators

#pragma warning disable 1591

        public static bool operator ==(InlineResponse2005 left, InlineResponse2005 right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(InlineResponse2005 left, InlineResponse2005 right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591

        #endregion Operators
    }
}
