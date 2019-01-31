using MongoDB.Bson;

namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class CancelOrderEventEntry : EventEntry
    {
        public ObjectId CancelOrderId;
    }
}
