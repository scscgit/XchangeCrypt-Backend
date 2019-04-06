using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    [BsonKnownTypes(
        typeof(CancelOrderEventEntry),
        typeof(CreateOrderEventEntry),
        typeof(MatchOrderEventEntry),
        typeof(TransactionCommitEventEntry),
        typeof(WalletDepositEventEntry),
        //typeof(WalletEventEntry),
        typeof(WalletRevokeEventEntry),
        typeof(WalletConsolidationTransferEventEntry),
        typeof(WalletWithdrawalEventEntry))]
    public abstract class EventEntry
    {
        public ObjectId Id;

        /// <summary>
        /// Always ascending transaction number within event sourcing.
        /// </summary>
        public long VersionNumber;

        public DateTime EntryTime;

        // TODO: event discriminator via constructor?
    }
}
