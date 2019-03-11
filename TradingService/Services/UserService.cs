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

        public void AddWallet(string user, string accountId, string coinSymbol, string publicKey)
        {
            // TODO: create user account if does not exist yet
            // After a wallet generation event, wallet service already contains private key,
            // so a public key representation must be created, always storing only the most recent one
            // This also initializes user account if none exists yet
            var userAccount = Accounts
                .Find(account =>
                    account.User.Equals(user)
                    && account.AccountId.Equals(accountId))
                .Single();
            userAccount.CoinWallets.Add(new CoinWallet
            {
                CoinSymbol = coinSymbol,
                PublicKey = publicKey,
                Balance = 0,
            });
            // TODO: make sure no hazard occurs, as this can overwrite other update!
            Accounts.UpdateOne(
                account => account.Id.Equals(userAccount.Id),
                Builders<AccountEntry>.Update.Set(e => e.CoinWallets, userAccount.CoinWallets));
        }
    }
}
