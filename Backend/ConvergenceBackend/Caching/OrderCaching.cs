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

        public List<Execution> GetExecutions(string user, string instrument, int? maxCount)
        {
            return (List<Execution>)TradingRepository
                .TransactionHistory()
                .Find(e => e.User.Equals(user) && e.Instrument.Equals(instrument))
                .Limit(maxCount)
                .ToList()
                .Select(e => new Execution
                {
                    Id = e.OrderId,
                    Instrument = e.Instrument,
                    Price = e.Price,
                    Time = e.EntryTime.ToBinary(),
                    Qty = e.FilledQty,
                    Side = MapSide(e.Side),
                });
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
    }
}
