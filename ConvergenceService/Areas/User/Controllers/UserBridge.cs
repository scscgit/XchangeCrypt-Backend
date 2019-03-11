using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private static readonly string[] AllCoins =
        {
            "ETH",
            "BTC",
            "LTC",
        };

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
        /// Obsolete api.
        /// Receives details of all wallets of the authorized user.
        /// </summary>
        [Obsolete]
        [HttpGet("wallets")]
        public IEnumerable<WalletDetails> Wallets()
        {
            return Wallets("0");
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
            foreach (var expectedCoin in AllCoins)
            {
                if (!wallets.Any(wallet => wallet.CoinSymbol.Equals(expectedCoin)))
                {
                    var error = WalletGenerate(accountId, expectedCoin);
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
            }

            return wallets;


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
        /// <param name="accountId">The account identifier.</param>
        /// <param name="coinSymbol">A unique symbol identification of a coin.</param>
        [HttpGet("accounts/{accountId}/wallets/{coinSymbol}")]
        public WalletDetails Wallet(
            [FromRoute] [Required] string accountId,
            [FromRoute] [Required] string coinSymbol)
        {
            return Wallets(accountId).Single(wallet => wallet.CoinSymbol.Equals(coinSymbol));
        }

        /// <summary>
        /// Requests a coin withdrawal from a specific wallet of the authorized user.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="coinSymbol">A unique symbol identification of a coin.</param>
        /// <param name="recipientPublicKey">Recipient address of a wallet for coins to be sent to</param>
        /// <param name="withdrawalAmount">Amount of balance to withdraw, represented in multiplies of the lowest tradable amount, which is specified by the wallet</param>
        [HttpPost("accounts/{accountId}/wallets/{coinSymbol}/withdraw")]
        public IDictionary<string, string> WalletWithdraw(
            [FromRoute] [Required] string accountId,
            [FromRoute] [Required] string coinSymbol,
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


        /// <summary>
        /// Requests a coin withdrawal from a specific wallet of the authorized user.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="coinSymbol">A unique symbol identification of a coin.</param>
        /// <returns>Error if any</returns>
        [HttpPut("accounts/{accountId}/wallets/{coinSymbol}/generate")]
        public string WalletGenerate(
            [FromRoute] [Required] string accountId,
            [FromRoute] [Required] string coinSymbol)
        {
            return CommandService.GenerateWallet(User.GetIdentifier(), accountId, coinSymbol, "").Result;
        }
    }
}
