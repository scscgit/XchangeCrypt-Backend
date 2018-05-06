using System;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingBackend.Models;
using XchangeCrypt.Backend.TradingBackend.Models.Enums;
using XchangeCrypt.Backend.TradingBackend.Repositories;

namespace XchangeCrypt.Backend.TradingBackend.Services
{
    public class ActivityHistoryService
    {
        public ActivityHistoryRepository ActivityHistoryRepository { get; }

        public ActivityHistoryService(ActivityHistoryRepository activityHistoryRepository)
        {
            ActivityHistoryRepository = activityHistoryRepository;
        }

        public Task<ActivityHistoryOrderEntry> PersistLimitOrder(string user, string accountId, string instrument, decimal? quantity, OrderSide orderSide, decimal? limitPrice, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit)
        {
            return PersistOrder(user, accountId, instrument, quantity, orderSide, OrderType.Market, limitPrice, null, durationType, duration, stopLoss, takeProfit);
        }

        public Task<ActivityHistoryOrderEntry> PersistStopOrder(string user, string accountId, string instrument, decimal? quantity, OrderSide orderSide, decimal? stopPrice, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit)
        {
            return PersistOrder(user, accountId, instrument, quantity, orderSide, OrderType.Market, null, stopPrice, durationType, duration, stopLoss, takeProfit);
        }

        public Task<ActivityHistoryOrderEntry> PersistMarketOrder(string user, string accountId, string instrument, decimal? quantity, OrderSide orderSide, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit)
        {
            return PersistOrder(user, accountId, instrument, quantity, orderSide, OrderType.Market, null, null, durationType, duration, stopLoss, takeProfit);
        }

        public async Task<ActivityHistoryWalletOperationEntry> PersistWalletOperation(string user, string accountId, string coinSymbol, string depositType, string withdrawalType, decimal amount)
        {
            var entry = new ActivityHistoryWalletOperationEntry
            {
                EntryTime = DateTime.Now,
                User = user,
                AccountId = accountId,
                CoinSymbol = coinSymbol,
                DepositType = depositType,
                WithdrawalType = withdrawalType,
                Amount = amount,
            };
            await ActivityHistoryRepository.WalletOperations().InsertOneAsync(entry);
            return entry;
        }

        private async Task<ActivityHistoryOrderEntry> PersistOrder(string user, string accountId, string instrument, decimal? quantity, OrderSide orderSide, OrderType orderType, decimal? limitPrice, decimal? stopPrice, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit)
        {
            var entry = new ActivityHistoryOrderEntry
            {
                EntryTime = DateTime.Now,
                User = user,
                AccountId = accountId,
                Instrument = instrument,
                Qty = quantity,
                Side = orderSide,
                Type = orderType,
                LimitPrice = limitPrice,
                StopPrice = stopPrice,
                DurationType = durationType,
                Duration = duration,
                StopLoss = stopLoss,
                TakeProfit = takeProfit,
            };
            await ActivityHistoryRepository.Orders().InsertOneAsync(entry);
            return entry;
        }
    }
}
