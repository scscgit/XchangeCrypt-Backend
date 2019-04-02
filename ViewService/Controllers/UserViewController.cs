using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using XchangeCrypt.Backend.ConvergenceService.Areas.User.Models;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.ViewService.Controllers
{
    [Route("api/v1/view/")]
    [ApiController]
    public class UserViewController
    {
        public AccountRepository AccountRepository { get; }

        public UserViewController(AccountRepository accountRepository)
        {
            AccountRepository = accountRepository;
        }

        [HttpGet]
        [Route("wallets")]
        public List<WalletDetails> Wallets(
            [FromQuery] [Required] string user,
            [FromQuery] [Required] string accountId)
        {
            var userAccount = AccountRepository.Accounts()
                .Find(account =>
                    account.User.Equals(user)
                    && account.AccountId.Equals(accountId));
            if (userAccount.CountDocuments() == 0)
            {
                // Convergence service will initiate the wallet generation anyway, and it includes account creation
                return new List<WalletDetails>();
            }

            return userAccount
                .Single()
                .CoinWallets
                .Select(coinWallet =>
                    new WalletDetails
                    {
                        Balance = coinWallet.Balance,
                        CoinSymbol = coinWallet.CoinSymbol,
                        WalletPublicKey = coinWallet.PublicKey,
                    }
                )
                .ToList();
        }
    }
}
