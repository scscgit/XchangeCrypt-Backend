using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;

namespace XchangeCrypt.Backend.DatabaseAccess.Models
{
    public class HiddenOrderEntry
    {
        public ObjectId Id { get; set; }

        public DateTime EntryTime { get; set; }

        public long CreatedOnVersionId { get; set; }

        public string User { get; set; }

        public string AccountId { get; set; }

        public string Instrument { get; set; }

        public decimal Qty { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public OrderSide Side { get; set; }

        public decimal StopPrice { get; set; }

        public string ParentId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public ParentOrderType? ParentType { get; set; }

        /// <summary>
        /// Many to many reverse side.
        /// </summary>
        public IList<string> ChildrenIds { get; set; } = new List<string>();

        public string DurationType { get; set; }

        public decimal? Duration { get; set; }
    }
}
