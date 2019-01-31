using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;

namespace XchangeCrypt.Backend.DatabaseAccess.Models
{
    public class TransactionHistoryEntry
    {
        public ObjectId Id { get; set; }

        public DateTime ExecutionTime { get; set; }

        public string User { get; set; }

        public string AccountId { get; set; }

        public string Instrument { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public OrderSide Side { get; set; }

        /// <summary>
        /// Optional.
        /// </summary>
        public ObjectId OrderId { get; set; }

        public decimal FilledQty { get; set; }

        public decimal Price { get; set; }
    }
}
