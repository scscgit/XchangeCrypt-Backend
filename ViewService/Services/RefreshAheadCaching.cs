using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Swagger.Models;
using Microsoft.Extensions.Logging;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.ViewService.Services
{
    public class RefreshAheadCaching : OrderCaching
    {
        public RefreshAheadCaching(
            TradingRepository tradingRepository,
            ILogger<RefreshAheadCaching> logger
        ) : base(tradingRepository, logger)
        {
        }

        private IDictionary<(string, string, string, int?), List<Execution>> _executions =
            new ConcurrentDictionary<(string, string, string, int?), List<Execution>>();

        private IDictionary<(string, string), List<Order>> _orders =
            new ConcurrentDictionary<(string, string), List<Order>>();

        private IDictionary<(string, string, string), Order> _order =
            new ConcurrentDictionary<(string, string, string), Order>();

        private IDictionary<(string, string, int?), List<Order>> _ordersHistory =
            new ConcurrentDictionary<(string, string, int?), List<Order>>();

        private IDictionary<string, Depth> _depth =
            new ConcurrentDictionary<string, Depth>();

        private IDictionary<(string, string, decimal?, decimal?, decimal?), BarsArrays> _historyBars =
            new ConcurrentDictionary<(string, string, decimal?, decimal?, decimal?), BarsArrays>();

        internal override List<Execution> GetExecutions(string user, string accountId, string instrument, int? maxCount)
        {
            var refresh = new Task(() =>
            {
                _executions[(user, accountId, instrument, maxCount)] =
                    base.GetExecutions(user, accountId, instrument, maxCount);
            });
            refresh.Start();
            if (!_executions.ContainsKey((user, accountId, instrument, maxCount)))
            {
                refresh.Wait();
            }

            return _executions[(user, accountId, instrument, maxCount)];
        }

        internal override List<Order> GetOrders(string user, string accountId)
        {
            var refresh = new Task(() => { _orders[(user, accountId)] = base.GetOrders(user, accountId); });
            refresh.Start();
            if (!_orders.ContainsKey((user, accountId)))
            {
                refresh.Wait();
            }

            return _orders[(user, accountId)];
        }

        internal override Order GetOrder(string user, string accountId, string orderId)
        {
            var refresh = new Task(() =>
            {
                _order[(user, accountId, orderId)] = base.GetOrder(user, accountId, orderId);
            });
            refresh.Start();
            if (!_order.ContainsKey((user, accountId, orderId)))
            {
                refresh.Wait();
            }

            return _order[(user, accountId, orderId)];
        }

        public override List<Order> GetOrdersHistory(string user, string accountId, int? maxCount)
        {
            var refresh = new Task(() =>
            {
                _ordersHistory[(user, accountId, maxCount)] = base.GetOrdersHistory(user, accountId, maxCount);
            });
            refresh.Start();
            if (!_ordersHistory.ContainsKey((user, accountId, maxCount)))
            {
                refresh.Wait();
            }

            return _ordersHistory[(user, accountId, maxCount)];
        }

        public override Depth GetDepth(string instrument)
        {
            var refresh = new Task(() => { _depth[instrument] = base.GetDepth(instrument); });
            refresh.Start();
            if (!_depth.ContainsKey(instrument))
            {
                refresh.Wait();
            }

            return _depth[instrument];
        }

        public override BarsArrays GetHistoryBars(
            string instrument, string resolution, decimal? from, decimal? to, decimal? countback)
        {
            var refresh = new Task(() =>
            {
                _historyBars[(instrument, resolution, from, to, countback)] =
                    base.GetHistoryBars(instrument, resolution, from, to, countback);
            });
            refresh.Start();
            if (!_historyBars.ContainsKey((instrument, resolution, from, to, countback)))
            {
                refresh.Wait();
            }

            return _historyBars[(instrument, resolution, from, to, countback)];
        }
    }
}
