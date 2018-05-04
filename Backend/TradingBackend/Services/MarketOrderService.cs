using System;
using System.Threading.Tasks;

namespace XchangeCrypt.Backend.TradingBackend.Services
{
    // TODO: one instance per instrument?
    public class MarketOrderService : AbstractTradingOrderService
    {
        /// <summary>
        /// </summary>
        public MarketOrderService()
        {
        }

        internal Task Buy(string user, string accountId, string instrument, decimal? quantity, string side, string type, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit, string requestId)
        {
            throw new NotImplementedException();
        }

        internal Task Sell(string user, string accountId, string instrument, decimal? quantity, string side, string type, string durationType, decimal? duration, decimal? stopLoss, decimal? takeProfit, string requestId)
        {
            throw new NotImplementedException();
        }
    }
}
