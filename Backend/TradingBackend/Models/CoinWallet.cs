namespace XchangeCrypt.Backend.TradingBackend.Models
{
    public class CoinWallet
    {
        // TODO maybe unique ID
        public string CoinSymbol;

        /// <summary>
        /// Generated from WalletBackend, copy.
        /// </summary>
        public string PublicKey;

        public decimal Balance;
    }
}
