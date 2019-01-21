namespace XchangeCrypt.Backend.ConvergenceService.Areas.User.Models
{
    /// <summary>
    /// Details of a single wallet representing a balance of a single coin of one user.
    /// </summary>
    public class WalletDetails
    {
        /// <summary>
        /// Short and unique coin name, used as a part of instrument trading pair name. Also used as account identifier.
        /// </summary>
        public string CoinSymbol;

        //public string CoinFullName;

        /// <summary>
        /// Address to be used for receiving coins into the wallet.
        /// </summary>
        public string WalletPublicKey;

        /// <summary>
        /// User balance of the wallet represented in multiplies of the lowest tradable amount, which is specified by the wallet.
        /// </summary>
        public decimal Balance;
    }
}
