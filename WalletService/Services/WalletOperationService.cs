using MongoDB.Driver;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.WalletService.Services
{
    public class WalletOperationService
    {
        private IMongoCollection<HotWallet> HotWallets { get; }

        public WalletOperationService(WalletRepository walletRepository)
        {
            HotWallets = walletRepository.HotWallets();
        }

        public string GetPublicKey(string user, string accountId, string coinSymbol)
        {
            return HotWallets.Find(hotWallet =>
                    hotWallet.User.Equals(user)
                    && hotWallet.AccountId.Equals(accountId)
                    && hotWallet.CoinSymbol.Equals(coinSymbol))
                .Single().PublicKey;
        }

        public void StoreHdWallet(string hdSeed, string publicKey, string user, string accountId, string coinSymbol)
        {
            HotWallets.InsertOne(new HotWallet
            {
                HdSeed = hdSeed,
                PublicKey = publicKey,
                User = user,
                AccountId = accountId,
                CoinSymbol = coinSymbol
            });
        }
    }
}
