using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;

namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class MatchOrderEventEntry : EventEntry
    {
        // ActionOrderId is not available directly, but there is always a (first) CreateOrderEventEntry
        // with the same VersionNumber, which will be used!
        //public ObjectId ActionOrderId;

        // Redundant, contained in ActionOrderId
        public string ActionUser;

        // Redundant, contained in ActionOrderId
        public string ActionAccountId;

        // Correction: we will use version number instead
        //public ObjectId TargetOrderId;

        public long TargetOrderOnVersionNumber;

        // Redundant, contained in TargetOrderId
        public string TargetUser;

        // Redundant, contained in TargetOrderId
        public string TargetAccountId;

        // Redundant, contained in TargetOrderId
        public string Instrument;

        public decimal Qty;

        [JsonConverter(typeof(StringEnumConverter))] [BsonRepresentation(BsonType.String)]
        public OrderSide ActionSide;

        public decimal Price;

        public decimal ActionBaseNewBalance;

        public decimal ActionQuoteNewBalance;

        public decimal TargetBaseNewBalance;

        public decimal TargetQuoteNewBalance;

        // Integrity validation
        public decimal ActionOrderQtyRemaining;

        // Integrity validation
        public decimal TargetOrderQtyRemaining;
    }
}
