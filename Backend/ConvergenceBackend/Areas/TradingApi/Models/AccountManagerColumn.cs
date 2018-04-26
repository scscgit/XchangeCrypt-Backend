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
    /// 
    /// </summary>
    [DataContract]
    public partial class AccountManagerColumn : IEquatable<AccountManagerColumn>
    { 
        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [Required]
        [DataMember(Name="id")]
        public string Id { get; set; }

        /// <summary>
        /// Localized title of a column
        /// </summary>
        /// <value>Localized title of a column</value>
        [Required]
        [DataMember(Name="title")]
        public string Title { get; set; }

        /// <summary>
        /// Tooltip that is shown on mouse hover
        /// </summary>
        /// <value>Tooltip that is shown on mouse hover</value>
        [DataMember(Name="tooltip")]
        public string Tooltip { get; set; }

        /// <summary>
        /// Set it to true if data length is frequently changed
        /// </summary>
        /// <value>Set it to true if data length is frequently changed</value>
        [DataMember(Name="fixedWidth")]
        public bool? FixedWidth { get; set; }

        /// <summary>
        /// Set it to false if this columns data should not be sortable
        /// </summary>
        /// <value>Set it to false if this columns data should not be sortable</value>
        [DataMember(Name="sortable")]
        public bool? Sortable { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AccountManagerColumn {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Title: ").Append(Title).Append("\n");
            sb.Append("  Tooltip: ").Append(Tooltip).Append("\n");
            sb.Append("  FixedWidth: ").Append(FixedWidth).Append("\n");
            sb.Append("  Sortable: ").Append(Sortable).Append("\n");
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
            return obj.GetType() == GetType() && Equals((AccountManagerColumn)obj);
        }

        /// <summary>
        /// Returns true if AccountManagerColumn instances are equal
        /// </summary>
        /// <param name="other">Instance of AccountManagerColumn to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(AccountManagerColumn other)
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
                    Tooltip == other.Tooltip ||
                    Tooltip != null &&
                    Tooltip.Equals(other.Tooltip)
                ) && 
                (
                    FixedWidth == other.FixedWidth ||
                    FixedWidth != null &&
                    FixedWidth.Equals(other.FixedWidth)
                ) && 
                (
                    Sortable == other.Sortable ||
                    Sortable != null &&
                    Sortable.Equals(other.Sortable)
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
                    if (Tooltip != null)
                    hashCode = hashCode * 59 + Tooltip.GetHashCode();
                    if (FixedWidth != null)
                    hashCode = hashCode * 59 + FixedWidth.GetHashCode();
                    if (Sortable != null)
                    hashCode = hashCode * 59 + Sortable.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
        #pragma warning disable 1591

        public static bool operator ==(AccountManagerColumn left, AccountManagerColumn right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AccountManagerColumn left, AccountManagerColumn right)
        {
            return !Equals(left, right);
        }

        #pragma warning restore 1591
        #endregion Operators
    }
}
