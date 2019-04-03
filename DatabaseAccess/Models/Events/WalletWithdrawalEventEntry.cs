using System;

namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class WalletWithdrawalEventEntry : WalletEventEntry
    {
        public string BlockchainTransactionId;

        public string WithdrawalTargetPublicKey;

        public decimal WithdrawalQty;

        public bool OverdrawnAndCanceledOrders;

        // Saga operation modifying null to true or false by Trading Service
        public bool? Validated;
    }
}
