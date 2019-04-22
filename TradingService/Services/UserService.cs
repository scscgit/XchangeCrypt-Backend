using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.TradingService.Services
{
    public class UserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly EventHistoryRepository _eventHistoryRepository;
        private IMongoCollection<AccountEntry> Accounts { get; }

        /// <summary>
        /// </summary>
        public UserService(
            AccountRepository accountRepository,
            EventHistoryRepository eventHistoryRepository,
            ILogger<UserService> logger)
        {
            _logger = logger;
            _eventHistoryRepository = eventHistoryRepository;
            Accounts = accountRepository.Accounts();
        }

        public (decimal, decimal) GetBalanceAndReservedBalance(string user, string accountId, string coinSymbol)
        {
            var userAccount = GetAccountQuery(user, accountId).Single();
            var userWallet = userAccount.CoinWallets.Single(
                userAccountCoinWallet => userAccountCoinWallet.CoinSymbol.Equals(coinSymbol)
            );
            return (userWallet.Balance, userWallet.ReservedBalance);
        }

        public async Task ModifyBalance(string user, string accountId, string coinSymbol, decimal relativeValue)
        {
            AccountEntry result;
            do
            {
                var userAccount = GetAccountQuery(user, accountId).Single();
                var userWallet = userAccount.CoinWallets.Single(
                    userAccountCoinWallet => userAccountCoinWallet.CoinSymbol.Equals(coinSymbol)
                );
                result = Accounts.FindOneAndUpdate(
                    account =>
                        account.Id.Equals(userAccount.Id)
                        && account.CoinWallets.Any(coinWallet =>
                            coinWallet.CoinSymbol.Equals(coinSymbol)
                            // We modify the balance compared to the previous one, and simply retry when it was changed
                            && coinWallet.Balance == userWallet.Balance
                        ),
                    // Parameter -1 of CoinWallets list means first matching
                    Builders<AccountEntry>.Update.Set(
                        account => account.CoinWallets[-1].Balance,
                        userWallet.Balance + relativeValue
                    )
                );
                _logger.LogInformation(
                    $"Modified {coinSymbol} balance of {user} by {relativeValue} to {userWallet.Balance + relativeValue}");
                if (result == null)
                {
                    _logger.LogError(
                        $"Attempted to modify balance of user {user} accountId {accountId} coinSymbol {coinSymbol} from {userWallet.Balance} by {relativeValue}, but his Balance has changed meanwhile, so the atomic modify operation will be retried");
                }
            }
            while (result == null);
        }

        public async Task ModifyReservedBalance(string user, string accountId, string coinSymbol, decimal relativeValue)
        {
            AccountEntry result;
            do
            {
                var userAccount = GetAccountQuery(user, accountId).Single();
                var userWallet = userAccount.CoinWallets.Single(
                    userAccountCoinWallet => userAccountCoinWallet.CoinSymbol.Equals(coinSymbol)
                );
                result = Accounts.FindOneAndUpdate(
                    account =>
                        account.Id.Equals(userAccount.Id)
                        && account.CoinWallets.Any(coinWallet =>
                            coinWallet.CoinSymbol.Equals(coinSymbol)
                            // We modify the balance compared to the previous one, and simply retry when it was changed
                            && coinWallet.ReservedBalance == userWallet.ReservedBalance
                        ),
                    // Parameter -1 of CoinWallets list means first matching
                    Builders<AccountEntry>.Update.Set(
                        account => account.CoinWallets[-1].ReservedBalance,
                        userWallet.ReservedBalance + relativeValue
                    )
                );
                _logger.LogInformation(
                    $"Modified {coinSymbol} reserved balance of {user} by {relativeValue} to {userWallet.ReservedBalance + relativeValue} out of balance {userWallet.Balance}");
                if (result == null)
                {
                    _logger.LogError(
                        $"Attempted to modify reserved balance of user {user} accountId {accountId} coinSymbol {coinSymbol} from {userWallet.Balance} by {relativeValue}, but his Balance has changed meanwhile, so the atomic modify operation will be retried");
                }
            }
            while (result == null);
        }

        /// <summary>
        /// After a wallet generation event, wallet service already contains private key,
        /// so a public key representation must be created, always storing only the most recent one.
        /// This also initializes user account if none exists yet.
        /// </summary>
        /// <param name="user">User ID</param>
        /// <param name="accountId">User's account ID</param>
        /// <param name="coinSymbol">Coin symbol of the wallet</param>
        /// <param name="publicKey">Public key of the specified coin to be registered as address visible to user</param>
        public void AddWallet(string user, string accountId, string coinSymbol, string publicKey)
        {
            var userAccountQuery = GetAccountQuery(user, accountId);

            // Get the user account
            var userAccount = userAccountQuery.Single();

            var coinWalletList =
                from wallet in userAccount.CoinWallets
                where wallet.CoinSymbol.Equals(coinSymbol)
                select wallet;

            // Insert a new coin wallet, or replace the existing public key
            if (!coinWalletList.Any())
            {
                Accounts.UpdateOne(
                    account => account.Id.Equals(userAccount.Id),
                    Builders<AccountEntry>.Update.AddToSet(e => e.CoinWallets, new CoinWallet
                    {
                        CoinSymbol = coinSymbol,
                        PublicKey = publicKey,
                        Balance = 0,
                        ReservedBalance = 0,
                    })
                );
            }
            else
            {
                // We need to make sure wallet with the coinSymbol is unique. We simply assert it here
                coinWalletList.Single();
                // This is an atomical operation to make sure we DON'T MODIFY THE BALANCE!
                Accounts.FindOneAndUpdate(
                    account =>
                        account.Id.Equals(userAccount.Id)
                        && account.CoinWallets.Any(coinWallet => coinWallet.CoinSymbol.Equals(coinSymbol)),
                    // Parameter -1 of CoinWallets list means first matching
                    Builders<AccountEntry>.Update.Set(account => account.CoinWallets[-1].PublicKey, publicKey)
                );
            }
        }

        protected IFindFluent<AccountEntry, AccountEntry> GetAccountQuery(string user, string accountId)
        {
            var userAccountQuery = Accounts
                .Find(account =>
                    account.User.Equals(user)
                    && account.AccountId.Equals(accountId));

            // Create user account if one does not exist yet
            if (userAccountQuery.CountDocuments() == 0)
            {
                Accounts.InsertOne(
                    new AccountEntry
                    {
                        User = user,
                        AccountId = accountId,
                        CoinWallets = new List<CoinWallet>()
                    }
                );
            }

            return userAccountQuery;
        }
    }
}
