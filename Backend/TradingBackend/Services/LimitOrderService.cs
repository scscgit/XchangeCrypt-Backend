using System;

namespace XchangeCrypt.Backend.TradingBackend.Services
{
    // TODO: one instance per instrument?
    public class LimitOrderService
    {
        public LimitOrderService()
        {
        }

        internal void Buy(string user, int limitPrice)
        {
            Console.WriteLine($"User {user} bought at {limitPrice}");
        }

        internal void Sell(string user, int limitPrice)
        {
            Console.WriteLine($"User {user} sold at {limitPrice}");
        }
    }
}
