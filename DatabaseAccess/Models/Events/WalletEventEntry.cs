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
        public string CoinSymbol;

        public string WalletPublicKey;

        public decimal NewBalance;
    }
}
