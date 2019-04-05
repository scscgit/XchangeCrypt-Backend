using MongoDB.Bson.Serialization.Attributes;

namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    [BsonKnownTypes(
        typeof(WalletDepositEventEntry),
        typeof(WalletGenerateEventEntry),
        typeof(WalletRevokeEventEntry),
        typeof(WalletWithdrawalEventEntry))]
    public abstract class WalletEventEntry : EventEntry
    {
        public string User;

        public string AccountId;

        public string CoinSymbol;

        // Represents a most recent public key of the user
        public string LastWalletPublicKey;

        public decimal NewSourcePublicKeyBalance;
    }
}
