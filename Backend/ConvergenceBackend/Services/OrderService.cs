using System.Collections.Generic;
using System.Threading.Tasks;

namespace XchangeCrypt.Backend.ConvergenceBackend.Services
{
    public class OrderService
    {
        public static async Task CreateLimitOrder(
            string accountId,
            string instrument,
            decimal? qty,
            string side,
            string type,
            decimal? limitPrice,
            decimal? durationDateTime,
            decimal? takeProfit)
        {
            await QueueWriter.SendMessageAsync(
                new Dictionary<string, object>()
                {
                    { "accountId", accountId }
                },
                "LimitOrder"
            );
        }
    }
}
