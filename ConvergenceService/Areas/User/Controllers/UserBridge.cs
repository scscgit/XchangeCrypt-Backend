using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.ConstantsLibrary;
using XchangeCrypt.Backend.ConvergenceService.Areas.User.Models;
using XchangeCrypt.Backend.ConvergenceService.Extensions.Authentication;
using XchangeCrypt.Backend.ConvergenceService.Services;

namespace XchangeCrypt.Backend.ConvergenceService.Areas.User.Controllers
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
        private readonly ILogger<UserBridge> _logger;
        public CommandService CommandService { get; }
        public ViewProxyService ViewProxyService { get; }

        public UserBridge(CommandService commandService, ViewProxyService viewProxyService, ILogger<UserBridge> logger)
        {
            _logger = logger;
            CommandService = commandService;
            ViewProxyService = viewProxyService;
        }

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
        /// <param name="accountId">The account identifier.</param>
        /// </summary>
        [HttpGet("accounts/{accountId}/wallets")]
        public IEnumerable<WalletDetails> Wallets(
            [FromRoute] [Required] string accountId)
        {
            IList<WalletDetails> wallets = ViewProxyService.GetWallets(User.GetIdentifier(), "0").ToList();
            // Generate missing empty wallets
            var generateTasks = new Dictionary<string, Task<string>>();
            foreach (var expectedCoin in GlobalConfiguration.Currencies)
            {
                if (!wallets.Any(wallet => wallet.CoinSymbol.Equals(expectedCoin)))
                {
                    _logger.LogInformation($"Generating {expectedCoin} wallet for user {User.GetIdentifier()}");
                    generateTasks.Add(expectedCoin, WalletGenerate(accountId, expectedCoin));
                }
            }

            // Process the answers asynchronously
            foreach (var expectedCoin in generateTasks.Keys)
            {
                var error = generateTasks[expectedCoin].Result;
                _logger.LogInformation($"Generating {expectedCoin} wallet for user {User.GetIdentifier()}");
                if (error != null)
                {
                    _logger.LogError("Error during wallet generation: " + error);
                    wallets.Add(new WalletDetails
                    {
                        CoinSymbol = expectedCoin,
                        WalletPublicKey = null,
                        Balance = 0
                    });
                }
                else
                {
                    // Retry getting the wallet after it's supposed to be generated
                    wallets = ViewProxyService.GetWallets(User.GetIdentifier(), "0").ToList();
                    if (!wallets.Any(wallet => wallet.CoinSymbol.Equals(expectedCoin)))
                    {
                        _logger.LogError("Wallet generation did not cause a wallet to be generated");
                    }
                }
            }

            return wallets;
        }

        /// <summary>
        /// Receives details of a single specific wallet of the authorized user.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="coinSymbol">A unique symbol identification of a coin.</param>
        [HttpGet("accounts/{accountId}/wallets/{coinSymbol}")]
        public WalletDetails Wallet(
            [FromRoute] [Required] string accountId,
            [FromRoute] [Required] string coinSymbol)
        {
            return Wallets(accountId).Single(wallet => wallet.CoinSymbol.Equals(coinSymbol.ToUpperInvariant()));
        }

        /// <summary>
        /// Requests a coin withdrawal from a specific wallet of the authorized user.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="coinSymbol">A unique symbol identification of a coin.</param>
        /// <param name="recipientPublicKey">Recipient address of a wallet for coins to be sent to</param>
        /// <param name="withdrawalAmount">Amount of balance to withdraw, represented in multiplies of the lowest tradable amount, which is specified by the wallet</param>
        /// <returns>Error if any</returns>
        [HttpPost("accounts/{accountId}/wallets/{coinSymbol}/withdraw")]
        public string WalletWithdraw(
            [FromRoute] [Required] string accountId,
            [FromRoute] [Required] string coinSymbol,
            [FromForm] [Required] string recipientPublicKey,
            [FromForm] [Required] decimal withdrawalAmount)
        {
            return CommandService.WalletWithdraw(
                User.GetIdentifier(),
                accountId,
                coinSymbol.ToUpperInvariant(),
                recipientPublicKey,
                withdrawalAmount,
                ""
            ).Result;
        }

        // TODO: withdrawal history with state, cancel pending

        /// <summary>
        /// Requests a coin withdrawal from a specific wallet of the authorized user.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="coinSymbol">A unique symbol identification of a coin.</param>
        /// <returns>Error if any</returns>
        [HttpPut("accounts/{accountId}/wallets/{coinSymbol}/generate")]
        public async Task<string> WalletGenerate(
            [FromRoute] [Required] string accountId,
            [FromRoute] [Required] string coinSymbol)
        {
            return await CommandService.GenerateWallet(
                User.GetIdentifier(),
                accountId,
                coinSymbol.ToUpperInvariant(),
                CommandService.RandomRequestId()
            );
        }
    }
}
