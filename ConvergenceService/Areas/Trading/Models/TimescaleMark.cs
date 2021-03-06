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
    /// Timescale marks data.
    /// </summary>
    [DataContract]
    public partial class TimescaleMark : IEquatable<TimescaleMark>
    {
        /// <summary>
        /// Unique identifier of marks
        /// </summary>
        /// <value>Unique identifier of marks</value>
        [Required]
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// bar time, unix timestamp (UTC)
        /// </summary>
        /// <value>bar time, unix timestamp (UTC)</value>
        [Required]
        [DataMember(Name = "time")]
        public decimal? Time { get; set; }

        /// <summary>
        /// Mark color
        /// </summary>
        /// <value>Mark color</value>
        public enum ColorEnum
        {
            /// <summary>
            /// Enum RedEnum for red
            /// </summary>
            [EnumMember(Value = "red")] RedEnum = 1,

            /// <summary>
            /// Enum GreenEnum for green
            /// </summary>
            [EnumMember(Value = "green")] GreenEnum = 2,

            /// <summary>
            /// Enum BlueEnum for blue
            /// </summary>
            [EnumMember(Value = "blue")] BlueEnum = 3,

            /// <summary>
            /// Enum YellowEnum for yellow
            /// </summary>
            [EnumMember(Value = "yellow")] YellowEnum = 4
        }

        /// <summary>
        /// Mark color
        /// </summary>
        /// <value>Mark color</value>
        [DataMember(Name = "color")]
        public ColorEnum? Color { get; set; }

        /// <summary>
        /// Tooltip text
        /// </summary>
        /// <value>Tooltip text</value>
        [DataMember(Name = "tooltip")]
        public string Tooltip { get; set; }

        /// <summary>
        /// A letter to be printed on a mark. Single character
        /// </summary>
        /// <value>A letter to be printed on a mark. Single character</value>
        [Required]
        [DataMember(Name = "label")]
        public string Label { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class TimescaleMark {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Time: ").Append(Time).Append("\n");
            sb.Append("  Color: ").Append(Color).Append("\n");
            sb.Append("  Tooltip: ").Append(Tooltip).Append("\n");
            sb.Append("  Label: ").Append(Label).Append("\n");
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
            return obj.GetType() == GetType() && Equals((TimescaleMark) obj);
        }

        /// <summary>
        /// Returns true if TimescaleMark instances are equal
        /// </summary>
        /// <param name="other">Instance of TimescaleMark to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(TimescaleMark other)
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
                    Time == other.Time ||
                    Time != null &&
                    Time.Equals(other.Time)
                ) &&
                (
                    Color == other.Color ||
                    Color != null &&
                    Color.Equals(other.Color)
                ) &&
                (
                    Tooltip == other.Tooltip ||
                    Tooltip != null &&
                    Tooltip.Equals(other.Tooltip)
                ) &&
                (
                    Label == other.Label ||
                    Label != null &&
                    Label.Equals(other.Label)
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
                if (Time != null)
                    hashCode = hashCode * 59 + Time.GetHashCode();
                if (Color != null)
                    hashCode = hashCode * 59 + Color.GetHashCode();
                if (Tooltip != null)
                    hashCode = hashCode * 59 + Tooltip.GetHashCode();
                if (Label != null)
                    hashCode = hashCode * 59 + Label.GetHashCode();
                return hashCode;
            }
        }

        #region Operators

#pragma warning disable 1591

        public static bool operator ==(TimescaleMark left, TimescaleMark right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TimescaleMark left, TimescaleMark right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591

        #endregion Operators
    }
}
