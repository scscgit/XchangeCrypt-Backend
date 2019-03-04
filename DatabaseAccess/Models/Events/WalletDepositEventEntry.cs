namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class WalletDepositEventEntry : WalletEventEntry
    {
        public string BlockchainTransactionId;

        //public string WalletPublicKey;

        public decimal DepositQty;
    }
}
