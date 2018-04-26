/*
 * TradingView REST API Specification for Brokers
 *
 * No description provided (generated by Swagger Codegen https://github.com/swagger-api/swagger-codegen)
 *
 * 
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace IO.Swagger.Models
{ 
    /// <summary>
    /// Depth of market for an instrument
    /// </summary>
    [DataContract]
    public partial class Depth : IEquatable<Depth>
    { 
        /// <summary>
        /// Array of Asks
        /// </summary>
        /// <value>Array of Asks</value>
        [Required]
        [DataMember(Name="asks")]
        public List<DepthItem> Asks { get; set; }

        /// <summary>
        /// Array of Bids
        /// </summary>
        /// <value>Array of Bids</value>
        [Required]
        [DataMember(Name="bids")]
        public List<DepthItem> Bids { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Depth {\n");
            sb.Append("  Asks: ").Append(Asks).Append("\n");
            sb.Append("  Bids: ").Append(Bids).Append("\n");
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
            return obj.GetType() == GetType() && Equals((Depth)obj);
        }

        /// <summary>
        /// Returns true if Depth instances are equal
        /// </summary>
        /// <param name="other">Instance of Depth to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Depth other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return 
                (
                    Asks == other.Asks ||
                    Asks != null &&
                    Asks.SequenceEqual(other.Asks)
                ) && 
                (
                    Bids == other.Bids ||
                    Bids != null &&
                    Bids.SequenceEqual(other.Bids)
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
                    if (Asks != null)
                    hashCode = hashCode * 59 + Asks.GetHashCode();
                    if (Bids != null)
                    hashCode = hashCode * 59 + Bids.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
        #pragma warning disable 1591

        public static bool operator ==(Depth left, Depth right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Depth left, Depth right)
        {
            return !Equals(left, right);
        }

        #pragma warning restore 1591
        #endregion Operators
    }
}
