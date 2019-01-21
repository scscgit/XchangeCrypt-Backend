using System;
using System.Threading.Tasks;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.TradingService.Services
{
    // TODO: one instance per instrument?
    public class StopOrderService : AbstractTradingOrderService
    {
        public TradingRepository TradingRepository { get; }

        /// <summary>
        /// </summary>
        public StopOrderService(TradingRepository tradingRepository)
        {
            TradingRepository = tradingRepository;
        }

        internal Task Buy(string user, string accountId, string instrument, decimal? quantity, decimal? stopPrice,
            string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit)
        {
            throw new NotImplementedException();
        }

        internal Task Sell(string user, string accountId, string instrument, decimal? quantity, decimal? stopPrice,
            string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit)
        {
            throw new NotImplementedException();
        }
    }
}
