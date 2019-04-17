using MongoDB.Bson;

namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class CancelOrderEventEntry : EventEntry
    {
        // Redundant, serves only for error handling during audit
        public string User;

        // Redundant, serves only for error handling during audit
        public string AccountId;

        // Redundant, serves only for error handling during audit
        public string Instrument;

        public long CancelOrderCreatedOnVersionNumber;
    }
}
