using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using XchangeCrypt.Backend.ConvergenceService.Areas.User.Models;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.ViewService.Controllers
{
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
            return AccountRepository.Accounts()
                .Find(account =>
                    account.User.Equals(user)
                    && account.AccountId.Equals(accountId))
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
