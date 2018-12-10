using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using XchangeCrypt.Backend.ConvergenceBackend.Areas.AccountApi.Models;
using XchangeCrypt.Backend.ConvergenceBackend.Extensions.Authentication;

namespace XchangeCrypt.Backend.ConvergenceBackend.Areas.AccountApi.Controllers
{
    /// <summary>
    /// Accessor for management operations over a user's private account.
    /// </summary>
    [Produces("application/json")]
    [Area("AccountApi")]
    [Route("api/v1/accountapi/")]
    [Authorize]
    public class AccountApi : Controller
    {
        /// <summary>
        /// Receives all profile details related to an account of the authorized user.
        /// </summary>
        [HttpGet("profile")]
        public ProfileDetails Profile()
        {
            var emailClaim = User.FindFirst(ClaimTypes.Email);
            var realNameClaim = User.FindFirst(ClaimTypes.GivenName);
            return new ProfileDetails
            {
                Id = User.GetIdentifier(),
                Login = "",
                EmailAddress = emailClaim != null ? emailClaim.Value : "no-email",
                RealName = realNameClaim != null ? realNameClaim.Value : "no-real-name",
            };
        }

        /// <summary>
        /// Receives details of all wallets of the authorized user.
        /// </summary>
        [HttpGet("wallets")]
        public IEnumerable<WalletDetails> Wallets()
        {
            return new List<WalletDetails>()
            {
                new WalletDetails
                {
                    CoinSymbol = "BTC",
                    WalletPublicKey = "B65983299",
                    Balance = 0.0013185m,
                },
                new WalletDetails
                {
                    CoinSymbol = "LTC",
                    WalletPublicKey = "L88183299",
                    Balance = 103350.23358m,
                },
                new WalletDetails
                {
                    CoinSymbol = "QBC",
                    WalletPublicKey = "Q4018",
                    Balance = 800_059_900_000,
                },
            };
        }

        /// <summary>
        /// Receives details of a specific wallet of the authorized user.
        /// </summary>
        /// <param name="coinSymbol">Unique symbol identification of a coin</param>
        [HttpGet("wallets/{coinSymbol}")]
        public WalletDetails Wallet(
            [FromRoute] [Required] string coinSymbol)
        {
            foreach (var wallet in Wallets())
            {
                if (wallet.CoinSymbol.Equals(coinSymbol))
                {
                    return wallet;
                }
            }

            return null;
        }

        /// <summary>
        /// Requests a coin withdrawal from a specific wallet of the authorized user.
        /// </summary>
        /// <param name="coinSymbol">Unique symbol identification of a coin</param>
        /// <param name="recipientPublicKey">Recipient address of a wallet for coins to be sent to</param>
        /// <param name="withdrawalAmount">Amount of balance to withdraw, represented in multiplies of the lowest tradable amount, which is specified by the wallet</param>
        [HttpPost("wallets/{coinSymbol}/withdraw")]
        public IDictionary<string, string> Wallet(
            [FromRoute] [Required] string coinSymbol,
            [FromBody] [Required] string recipientPublicKey,
            [FromBody] [Required] long withdrawalAmount)
        {
            return new Dictionary<string, string>()
            {
                {"response", "error"},
                {"message", "Balance insufficient for the withdrawal"},
            };
        }
    }
}
