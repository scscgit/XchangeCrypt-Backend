using System;
using System.Collections.Generic;
using System.Linq;
using IO.Swagger.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using XchangeCrypt.Backend.ConvergenceService.Extensions;
using XchangeCrypt.Backend.DatabaseAccess.Models.Enums;
using XchangeCrypt.Backend.DatabaseAccess.Repositories;

namespace XchangeCrypt.Backend.ViewService.Services
{
    public class OrderCaching
    {
        public TradingRepository TradingRepository { get; }

        public OrderCaching(TradingRepository tradingRepository)
        {
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
