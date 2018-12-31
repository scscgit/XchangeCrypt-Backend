using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XchangeCrypt.Backend.ConvergenceBackend.Areas.User.Models;
using XchangeCrypt.Backend.ConvergenceBackend.Extensions.Authentication;

namespace XchangeCrypt.Backend.ConvergenceBackend.Areas.User.Controllers
{
    /// <inheritdoc />
    /// <summary>
    /// Accessor for management operations over a user's private account.
    /// </summary>
    [Produces("application/json")]
    [Area("User")]
    [Route("api/v1/user/")]
    [Authorize]
    public class UserBridge : Controller
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
                RealName = realNameClaim != null ? realNameClaim.Value : "no-real-name"
            };
        }

        /// <summary>
        /// Receives details of all wallets of the authorized user.
        /// </summary>
        [HttpGet("wallets")]
        public IEnumerable<WalletDetails> Wallets()
        {
            return new List<WalletDetails>
            {
                new WalletDetails
                {
                    CoinSymbol = "BTC",
                    WalletPublicKey = "B65983299",
                    Balance = 0.0013185m
                },
                new WalletDetails
                {
                    CoinSymbol = "LTC",
                    WalletPublicKey = "L88183299",
                    Balance = 103350.23358m
                },
                new WalletDetails
                {
                    CoinSymbol = "QBC",
                    WalletPublicKey = "Q4018",
                    Balance = 800_059_900_000
                }
            };
        }

        /// <summary>
        /// Receives details of a single specific wallet of the authorized user.
        /// </summary>
        /// <param name="accountId">The account identifier. A unique symbol identification of a coin</param>
        [HttpGet("accounts/{accountId}/wallet")]
        public WalletDetails Wallet(
            [FromRoute] [Required] string accountId)
        {
            return Wallets().Single(wallet => wallet.CoinSymbol.Equals(accountId));
        }

        /// <summary>
        /// Requests a coin withdrawal from a specific wallet of the authorized user.
        /// </summary>
        /// <param name="accountId">The account identifier. A unique symbol identification of a coin</param>
        /// <param name="recipientPublicKey">Recipient address of a wallet for coins to be sent to</param>
        /// <param name="withdrawalAmount">Amount of balance to withdraw, represented in multiplies of the lowest tradable amount, which is specified by the wallet</param>
        [HttpPost("accounts/{accountId}/withdraw")]
        public IDictionary<string, string> Wallet(
            [FromRoute] [Required] string accountId,
            [FromBody] [Required] string recipientPublicKey,
            [FromBody] [Required] decimal withdrawalAmount)
        {
            return new Dictionary<string, string>
            {
                {"response", "error"},
                {"message", "Balance insufficient for the withdrawal"}
            };
        }

        // TODO: withdrawal history with state, cancel pending
    }
}
