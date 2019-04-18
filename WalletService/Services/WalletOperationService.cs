using System.Collections.Generic;
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

        public string GetLastPublicKey(string user, string accountId, string coinSymbol)
        {
            var hotWallet = HotWallets.Find(hotWalletEntry =>
                    hotWalletEntry.User.Equals(user)
                    && hotWalletEntry.AccountId.Equals(accountId)
                    && hotWalletEntry.CoinSymbol.Equals(coinSymbol))
                .SortByDescending(e => e.CreatedOnVersionNumber)
                .First();
            return hotWallet?.PublicKey;
        }

        public string GetLastPublicKey(string otherPublicKey)
        {
            var hotWallet = HotWallets.Find(hotWalletEntry =>
                    hotWalletEntry.PublicKey.Equals(otherPublicKey))
                .SortByDescending(e => e.CreatedOnVersionNumber)
                .First();
            return hotWallet?.PublicKey;
        }

        public IList<HotWallet> GetAllHotWallets(string user, string accountId, string coinSymbol)
        {
            return HotWallets.Find(hotWalletEntry =>
                    hotWalletEntry.User.Equals(user)
                    && hotWalletEntry.AccountId.Equals(accountId)
                    && hotWalletEntry.CoinSymbol.Equals(coinSymbol))
                .ToList();
        }

//        public List<HotWallet> GetAllHotWallets(string otherPublicKey, string coinSymbol)
//        {
//            var otherHotWallet = HotWallets.Find(hotWalletEntry =>
//                    hotWalletEntry.PublicKey.Equals(otherPublicKey))
//                .SortByDescending(e => e.CreatedOnVersionNumber)
//                .First();
//
//            return HotWallets.Find(hotWalletEntry =>
//                    hotWalletEntry.User.Equals(otherHotWallet.User)
//                    && hotWalletEntry.AccountId.Equals(otherHotWallet.AccountId)
//                    // Coin symbol could be extracted from the public key too, this is just a failsafe double-check
//                    && hotWalletEntry.CoinSymbol.Equals(coinSymbol))
//                .ToList();
//        }

        public HotWallet GetHotWallet(string publicKey, string coinSymbol)
        {
            return HotWallets.Find(hotWalletEntry =>
                    hotWalletEntry.PublicKey.Equals(publicKey)
                    // Coin symbol could be extracted from the public key too, this is just a failsafe double-check
                    && hotWalletEntry.CoinSymbol.Equals(coinSymbol))
                .First();
        }

        public void StoreHdWallet(
            string hdSeed, string publicKey, string user, string accountId, string coinSymbol, long versionNumber)
        {
            if (HotWallets.UpdateMany(
                    Builders<HotWallet>.Filter.Eq(e => e.HdSeed, hdSeed),
                    Builders<HotWallet>.Update.Set(e => e.CreatedOnVersionNumber, versionNumber)
                ).MatchedCount == 0)
            {
                HotWallets.InsertOne(new HotWallet
                {
                    HdSeed = hdSeed,
                    PublicKey = publicKey,
                    User = user,
                    AccountId = accountId,
                    CoinSymbol = coinSymbol,
                    CreatedOnVersionNumber = versionNumber,
                });
            }
        }
    }
}
