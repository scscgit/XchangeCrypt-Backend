using System;

namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class WalletWithdrawalEventEntry : WalletEventEntry
    {
        public string BlockchainTransactionId;

        public string WithdrawalTargetPublicKey;

        public decimal WithdrawalQty;

        public bool OverdrawnAndCanceledOrders;

        // During the first processing run, this is guaranteed to be false, as the withdrawal occurs asynchronously,
        // and that's when this flag is set. In case of event re-processing, if it's true, then the balance can be
        // unlocked to be available for a deposit detection
        public bool Executed;

        // Saga operation modifying null to true or false by Trading Service
        public bool? Validated;
    }
}
