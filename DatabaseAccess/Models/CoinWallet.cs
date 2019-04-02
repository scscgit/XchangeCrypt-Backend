namespace XchangeCrypt.Backend.DatabaseAccess.Models
{
    public class CoinWallet
    {
        // TODO maybe unique ID
        public string CoinSymbol;

        /// <summary>
        /// Generated from WalletService, copy.
        /// </summary>
        public string PublicKey;

        public decimal Balance;

        public decimal ReservedBalance;
    }
}
