using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;

namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class CreateOrderEventEntry : EventEntry
    {
        public string User;

        public string AccountId;

        public string Instrument;

        public decimal Qty;

        [JsonConverter(typeof(StringEnumConverter))] [BsonRepresentation(BsonType.String)]
        public OrderSide Side;

        [JsonConverter(typeof(StringEnumConverter))] [BsonRepresentation(BsonType.String)]
        public OrderType Type;

        public decimal? LimitPrice;

        public decimal? StopPrice;

        public string DurationType;

        public decimal? Duration;

        public decimal? StopLoss;

        public decimal? TakeProfit;
    }
}
