namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class WalletWithdrawalEventEntry : WalletEventEntry
    {
        public string BlockchainTransactionId;

        public decimal WithdrawalQty;
    }
}
