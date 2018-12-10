using IO.Swagger.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using XchangeCrypt.Backend.TradingBackend.Models.Enums;
using XchangeCrypt.Backend.TradingBackend.Repositories;

namespace XchangeCrypt.Backend.ConvergenceBackend.Caching
{
    public class OrderCaching
    {
        public TradingRepository TradingRepository { get; }

        public OrderCaching(TradingRepository tradingRepository)
        {
            TradingRepository = tradingRepository;
        }

        public List<Execution> GetExecutions(string user, string accountId, string instrument, int? maxCount)
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
                    Time = e.EntryTime.ToBinary(),
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
                    Status = MapStatus(e.Status),
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
                            Status = StatusEnum.WorkingEnum,
                        })
                        .ToList()
                ).ToList();
        }

        internal Order GetOrder(string user, string accountId, string orderId)
        {
            // TODO: faster implementation - try to measure driver differences?
            return GetOrders(user, accountId).Find(e => e.Id.ToString().Equals(orderId));
        }

        private SideEnum MapSide(OrderSide side)
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

        private ParentTypeEnum? MapParentType(ParentOrderType? parentType)
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

        private StatusEnum? MapStatus(OrderStatus status)
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
