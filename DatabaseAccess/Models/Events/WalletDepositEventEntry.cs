namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class WalletDepositEventEntry : WalletEventEntry
    {
        public string BlockchainTransactionId;

        public decimal DepositQty;
    }
}
