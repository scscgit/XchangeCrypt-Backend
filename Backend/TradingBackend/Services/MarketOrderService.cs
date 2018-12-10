using System;
using System.Threading.Tasks;
using XchangeCrypt.Backend.TradingBackend.Repositories;

namespace XchangeCrypt.Backend.TradingBackend.Services
{
    // TODO: one instance per instrument?
    public class MarketOrderService : AbstractTradingOrderService
    {
        public TradingRepository TradingRepository { get; }

        /// <summary>
        /// </summary>
        public MarketOrderService(TradingRepository tradingRepository)
        {
            TradingRepository = tradingRepository;
        }

        internal Task Buy(string user, string accountId, string instrument, decimal? quantity, string durationType,
            decimal? duration, decimal? stopLoss, decimal? takeProfit, string requestId)
        {
            throw new NotImplementedException();
        }

        internal Task Sell(string user, string accountId, string instrument, decimal? quantity, string durationType,
            decimal? duration, decimal? stopLoss, decimal? takeProfit, string requestId)
        {
            throw new NotImplementedException();
        }
    }
}
