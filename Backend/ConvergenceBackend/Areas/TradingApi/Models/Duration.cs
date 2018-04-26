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
    /// Single duration option
    /// </summary>
    [DataContract]
    public partial class Duration : IEquatable<Duration>
    {
        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [Required]
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Localized title
        /// </summary>
        /// <value>Localized title</value>
        [Required]
        [DataMember(Name = "title")]
        public string Title { get; set; }

        /// <summary>
        /// Display date control in Order Ticket for this duration type
        /// </summary>
        /// <value>Display date control in Order Ticket for this duration type</value>
        [DataMember(Name = "hasDatePicker")]
        public bool? HasDatePicker { get; set; }

        /// <summary>
        /// Display time control in Order Ticket for this duration type
        /// </summary>
        /// <value>Display time control in Order Ticket for this duration type</value>
        [DataMember(Name = "hasTimePicker")]
        public bool? HasTimePicker { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Duration {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Title: ").Append(Title).Append("\n");
            sb.Append("  HasDatePicker: ").Append(HasDatePicker).Append("\n");
            sb.Append("  HasTimePicker: ").Append(HasTimePicker).Append("\n");
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
            return obj.GetType() == GetType() && Equals((Duration)obj);
        }

        /// <summary>
        /// Returns true if Duration instances are equal
        /// </summary>
        /// <param name="other">Instance of Duration to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Duration other)
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
                    Title == other.Title ||
                    Title != null &&
                    Title.Equals(other.Title)
                ) &&
                (
                    HasDatePicker == other.HasDatePicker ||
                    HasDatePicker != null &&
                    HasDatePicker.Equals(other.HasDatePicker)
                ) &&
                (
                    HasTimePicker == other.HasTimePicker ||
                    HasTimePicker != null &&
                    HasTimePicker.Equals(other.HasTimePicker)
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
                if (Title != null)
                    hashCode = hashCode * 59 + Title.GetHashCode();
                if (HasDatePicker != null)
                    hashCode = hashCode * 59 + HasDatePicker.GetHashCode();
                if (HasTimePicker != null)
                    hashCode = hashCode * 59 + HasTimePicker.GetHashCode();
                return hashCode;
            }
        }

        #region Operators

#pragma warning disable 1591

        public static bool operator ==(Duration left, Duration right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Duration left, Duration right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591

        #endregion Operators
    }
}
