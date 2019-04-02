namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class WalletWithdrawalEventEntry : WalletEventEntry
    {
        public string BlockchainTransactionId;

        public string WithdrawalTargetPublicKey;

        public decimal WithdrawalQty;

        public bool OverdrawnAndCanceledOrders;
    }
}
