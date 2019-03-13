using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.TradingService.Services
{
    public class UserService
    {
        private readonly EventHistoryRepository _eventHistoryRepository;
        private IMongoCollection<AccountEntry> Accounts { get; }

        /// <summary>
        /// </summary>
        public UserService(AccountRepository accountRepository, EventHistoryRepository eventHistoryRepository)
        {
            _eventHistoryRepository = eventHistoryRepository;
            Accounts = accountRepository.Accounts();
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
                        Balance = 0
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
    }
}
