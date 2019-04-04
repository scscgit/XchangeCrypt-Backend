namespace XchangeCrypt.Backend.DatabaseAccess.Models.Events
{
    public class WalletConsolidationTransferEventEntry : WalletEventEntry
    {
        public string BlockchainTransactionId;

        public string TransferSourcePublicKey;

        public string TransferTargetPublicKey;

        public decimal NewTargetPublicKeyBalance;

        public decimal TransferQty;

        // This modifying field won't be the only deciding factor, the Wallet Service must also verify expected balance
        public bool Executed;

        // Trading Service sets this flag while Wallet Service actively waits for it
        public bool? Valid;
    }
}
