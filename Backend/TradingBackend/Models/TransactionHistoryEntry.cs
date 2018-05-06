using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using XchangeCrypt.Backend.TradingBackend.Models.Enums;

namespace XchangeCrypt.Backend.TradingBackend.Models
{
    public class TransactionHistoryEntry
    {
        public ObjectId Id { get; set; }

        public DateTime EntryTime { get; set; }

        public string User { get; set; }

        public string AccountId { get; set; }

        public string Instrument { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public OrderSide Side { get; set; }

        /// <summary>
        /// Optional.
        /// </summary>
        public string OrderId { get; set; }

        public decimal FilledQty { get; set; }

        public decimal Price { get; set; }
    }
}
