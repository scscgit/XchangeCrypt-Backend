using System;
using System.Collections.Generic;
using System.Linq;
using IO.Swagger.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using XchangeCrypt.Backend.ConvergenceService.Extensions;
using XchangeCrypt.Backend.DatabaseAccess.Models;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.ViewService.Services
{
    public class OrderCaching
    {
        private readonly ILogger<OrderCaching> _logger;
        public TradingRepository TradingRepository { get; }

        public OrderCaching(TradingRepository tradingRepository, ILogger<OrderCaching> logger)
        {
            _logger = logger;
            TradingRepository = tradingRepository;
        }

        internal List<Execution> GetExecutions(string user, string accountId, string instrument, int? maxCount)
        {
            return TradingRepository
                .TransactionHistory()
                .Find(e => e.User.Equals(user) && e.AccountId.Equals(accountId) && e.Instrument.Equals(instrument))
                .Limit(maxCount)
                .ToList()
                .Select(e => new Execution
                {
                    Id = e.Id.ToString(),
                    Instrument = e.Instrument,
                    Price = e.Price,
                    Time = e.ExecutionTime.GetUnixEpochMillis(),
                    Qty = e.FilledQty,
                    Side = MapSide(e.Side),
                })
                .ToList();
        }

        internal List<Order> GetOrders(string user, string accountId)
        {
            return TradingRepository
                .OrderBook()
                .Find(e => e.User.Equals(user) && e.AccountId.Equals(accountId))
                .ToList()
                .Select(e => new Order
                {
                    Id = e.Id.ToString(),
                    Instrument = e.Instrument,
                    Qty = e.Qty,
                    Side = MapSide(e.Side),
                    Type = TypeEnum.LimitEnum,
                    FilledQty = e.FilledQty,
                    AvgPrice = e.LimitPrice,
                    LimitPrice = e.LimitPrice,
                    StopPrice = null,
                    ParentId = e.ParentId,
                    ParentType = MapParentType(e.ParentType),
                    Duration = new OrderDuration
                    {
                        Type = e.DurationType,
                        Datetime = e.Duration,
                    },
                    Status = MapStatus(OrderStatus.Working),
                })
                .ToList()
                .Union(
                    TradingRepository
                        .HiddenOrders()
                        .Find(e => e.User.Equals(user) && e.AccountId.Equals(accountId))
                        .ToList()
                        .Select(e => new Order
                        {
                            Id = e.Id.ToString(),
                            Instrument = e.Instrument,
                            Qty = e.Qty,
                            Side = MapSide(e.Side),
                            Type = TypeEnum.StopEnum,
                            // Stop order is never active until it becomes a market order
                            FilledQty = 0,
                            // TODO reconsider avg
                            AvgPrice = e.StopPrice,
                            LimitPrice = null,
                            StopPrice = e.StopPrice,
                            ParentId = e.ParentId,
                            ParentType = MapParentType(e.ParentType),
                            Duration = new OrderDuration
                            {
                                Type = e.DurationType,
                                Datetime = e.Duration,
                            },
                            // Is always valid if placed within hidden orders
                            Status = MapStatus(OrderStatus.Inactive),
                        })
                        .ToList()
                ).ToList();
        }

        internal Order GetOrder(string user, string accountId, string orderId)
        {
            // TODO: faster implementation - try to measure driver differences?
            return GetOrders(user, accountId).Find(e => e.Id.ToString().Equals(orderId));
        }

        public List<Order> GetOrdersHistory(string user, string accountId, int? maxCount)
        {
            return TradingRepository
                .OrderHistory()
                .Find(e => e.User.Equals(user) && e.AccountId.Equals(accountId))
                .SortByDescending(e => e.CloseTime)
                // Id is the true guarantee, but we use CloseTime as the priority in case future implementation changes
                // (e.g. with a Trade Service parallelization)
                .SortByDescending(e => e.Id)
                .Limit(maxCount)
                .ToList()
                .Select(e => new Order
                    {
                        Id = e.Id.ToString(),
                        Instrument = e.Instrument,
                        Qty = e.Qty,
                        Side = MapSide(e.Side),
                        Type = MapType(e.Type),
                        // Stop order is never active until it becomes a market order
                        FilledQty = e.FilledQty,
                        // TODO reconsider avg
                        AvgPrice = 0,
                        LimitPrice = e.LimitPrice,
                        StopPrice = e.StopPrice,
                        ParentId = e.ParentId,
                        ParentType = MapParentType(e.ParentType),
                        Duration = new OrderDuration
                        {
                            Type = e.DurationType,
                            Datetime = e.Duration,
                        },
                        // Is always valid if placed within hidden orders
                        Status = MapStatus(e.Status),
                    }
                ).ToList();
        }

        public Depth GetDepth(string instrument)
        {
            return new Depth
            {
                Asks = TradingRepository
                    .OrderBook()
                    .Find(e => e.Instrument.Equals(instrument) && e.Side == OrderSide.Sell)
                    .ToList()
                    .OrderBy(e => e.LimitPrice)
                    .GroupBy(e => e.LimitPrice)
                    .Select(limitPrice => new DepthItem
                    {
                        limitPrice.Key, limitPrice.Sum(e => e.Qty - e.FilledQty)
                    })
                    .ToList(),
                Bids = TradingRepository
                    .OrderBook()
                    .Find(e => e.Instrument.Equals(instrument) && e.Side == OrderSide.Buy)
                    .ToList()
                    .OrderByDescending(e => e.LimitPrice)
                    .GroupBy(e => e.LimitPrice)
                    .Select(limitPrice => new DepthItem
                    {
                        limitPrice.Key, limitPrice.Sum(e => e.Qty - e.FilledQty)
                    })
                    .ToList(),
            };
        }

        public BarsArrays GetHistoryBars(
            string instrument, string resolution, decimal? from, decimal? to, decimal? countback)
        {
            long secondsInterval;
            switch (resolution.ToCharArray()[1].ToString())
            {
                case "m":
                    secondsInterval = 60;
                    break;
                case "H":
                    secondsInterval = 60 * 60;
                    break;
                case "D":
                    secondsInterval = 60 * 60 * 24;
                    break;
                case "W":
                    secondsInterval = 60 * 60 * 24 * 7;
                    break;
                case "M":
                    // Approximately
                    //secondsInterval = 60 * 60 * 24 * 7 * 30;
                    // Mocking as 10 seconds
                    secondsInterval = 10;
                    break;
                default:
                    throw new Exception($"Unsupported resolution {resolution}");
            }

            secondsInterval *= long.Parse($"{resolution.ToCharArray()[0].ToString()}");

            var transactionHistory = TradingRepository.TransactionHistory();
            var result = new List<(decimal, decimal, decimal, decimal, decimal, long)>();
            try
            {
                var cursor = transactionHistory
                    .MapReduce(
                        new BsonJavaScript(@"
function() {
    var transaction = this;
    emit(transaction.ExecutionTime, { count: 1, keyName: transaction.Field });
}"
                        ),
                        new BsonJavaScript(@"
function(key, values) {
    var result = {count: 0, keyName: 0 };

    values.forEach(function(value){
        result.count += value.count;
        result.keyName += value.keyName;
    });

return result;
}"
                        ),
                        new MapReduceOptions<
                            TransactionHistoryEntry,
                            (decimal, decimal, decimal, decimal, decimal, long)
                        >
                        {
                            OutputOptions = MapReduceOutputOptions.Inline,
                            Scope = new BsonDocument(new Dictionary<string, object>
                            {
                                // Values passed to global scope of the map, reduce, and finalize functions
                                {"instrument", instrument},
                                {"resolution", resolution},
                            }),
                        }
                    );
                while (cursor.MoveNext())
                {
                    result.AddRange(cursor.Current);
                }
            }
            catch (Exception ex) when (ex is MongoCommandException || ex is BsonSerializationException)
            {
                // MongoCommandException means MapReduce not supported by MongoDB server
                // Alternatively, BsonSerializationException means the MapReduce is not implemented properly
                _logger.LogError("MongoDB MapReduce unavailable, using fallback");
                var fromDate = ((long) from.Value).GetDateTimeFromUnixEpochMillis();
                var toDate = ((long) to.Value).GetDateTimeFromUnixEpochMillis();
                var grouping = transactionHistory
                    .Find(transaction =>
                        instrument.Equals(transaction.Instrument)
                        // The GetUnixEpochMillis() conversion isn't supported by MongoDB, we need an alternative
                        && transaction.ExecutionTime >= fromDate
                        && transaction.ExecutionTime <= toDate
                    )
                    .Sort(Builders<TransactionHistoryEntry>.Sort.Ascending(e => e.ExecutionTime))
                    .ToList()
                    .GroupBy(e => e.ExecutionTime.GetUnixEpochMillis() / (1000 * secondsInterval));
                result = grouping.Select(interval =>
                        (
                            interval.First().Price,
                            interval.Max(tx => tx.Price),
                            interval.Min(tx => tx.Price),
                            interval.Last().Price,
                            interval.Sum(tx => tx.FilledQty),
                            interval.Key * 1000 * secondsInterval
                        )
                    )
                    .ToList();
            }

            var (O, H, L, C, V, T) = result.Aggregate((
                    new List<decimal?>(),
                    new List<decimal?>(),
                    new List<decimal?>(),
                    new List<decimal?>(),
                    new List<decimal?>(),
                    new List<decimal?>()),
                (lists, tuples) =>
                {
                    var (o, h, l, c, v, t) = lists;
                    var (open, high, low, close, volume, time) = tuples;
                    o.Add(open);
                    h.Add(high);
                    l.Add(low);
                    c.Add(close);
                    v.Add(volume);
                    t.Add(time);
                    return (o, h, l, c, v, t);
                }
            );
            return new BarsArrays
            {
                S = SEnum.OkEnum,
                Errmsg = null,
                Nb = null,
                O = O,
                H = H,
                L = L,
                C = C,
                V = V,
                T = T,
            };
        }

        private static SideEnum MapSide(OrderSide side)
        {
            switch (side)
            {
                case OrderSide.Buy:
                    return SideEnum.BuyEnum;

                case OrderSide.Sell:
                    return SideEnum.SellEnum;

                default:
                    throw new Exception("Unknown OrderSide");
            }
        }

        private static TypeEnum MapType(OrderType type)
        {
            switch (type)
            {
                case OrderType.Limit:
                    return TypeEnum.LimitEnum;
                case OrderType.Stop:
                    return TypeEnum.StopEnum;
                case OrderType.Market:
                    return TypeEnum.MarketEnum;

                default:
                    throw new Exception("Unknown OrderType");
            }
        }

        private static ParentTypeEnum? MapParentType(ParentOrderType? parentType)
        {
            if (parentType == null)
            {
                return null;
            }

            switch (parentType)
            {
                case ParentOrderType.Order:
                    return ParentTypeEnum.OrderEnum;

                case ParentOrderType.Position:
                    return ParentTypeEnum.PositionEnum;

                default:
                    throw new Exception("Unknown ParentOrderType");
            }
        }

        private static StatusEnum? MapStatus(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.Cancelled:
                    return StatusEnum.CancelledEnum;

                case OrderStatus.Filled:
                    return StatusEnum.FilledEnum;

                case OrderStatus.Inactive:
                    return StatusEnum.InactiveEnum;

                case OrderStatus.Placing:
                    return StatusEnum.PlacingEnum;

                case OrderStatus.Rejected:
                    return StatusEnum.RejectedEnum;

                case OrderStatus.Working:
                    return StatusEnum.WorkingEnum;

                default:
                    throw new Exception("Unknown OrderStatus");
            }
        }
    }
}
