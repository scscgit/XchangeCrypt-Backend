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
            // TODO: re-work so that this happens implicitly! not in view service!
            var userAccount = AccountRepository.Accounts()
                .Find(account =>
                    account.User.Equals(user)
                    && account.AccountId.Equals(accountId));
            if (userAccount.CountDocuments() == 0)
            {
                AccountRepository.Accounts().InsertOne(
                    new AccountEntry
                    {
                        User = user,
                        AccountId = accountId,
                        CoinWallets = new List<CoinWallet>(),
                    });
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
