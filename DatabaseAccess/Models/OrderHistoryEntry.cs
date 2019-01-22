using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;

namespace XchangeCrypt.Backend.DatabaseAccess.Models
{
    public class OrderHistoryEntry
    {
        public ObjectId Id { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime CloseTime { get; set; }

        public string User { get; set; }

        public string AccountId { get; set; }

        public string Instrument { get; set; }

        public decimal Qty { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public OrderSide Side { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public OrderType Type { get; set; }

        public decimal FilledQty { get; set; }

        //public decimal? AvgPrice { get; set; }

        public decimal? LimitPrice { get; set; }

        public decimal? StopPrice { get; set; }

        /// <summary>
        /// Many to many reverse side.
        /// </summary>
        public string[] ChildrenIds { get; set; }

        public string DurationType { get; set; }

        public decimal? Duration { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public OrderStatus Status { get; set; }
    }
}
