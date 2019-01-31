using MongoDB.Bson;

namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class WalletRevokeEventEntry : WalletEventEntry
    {
        public ObjectId RevokeWalletEventEntryId;
    }
}
