using XchangeCrypt.Backend.TradingBackend.Models.Enums;

namespace XchangeCrypt.Backend.TradingBackend.Models
{
    public class OrderHistoryEntry
    {
        public string Id { get; set; }

        public string User { get; set; }

        public string AccountId { get; set; }

        public string Instrument { get; set; }

        public decimal Qty { get; set; }

        public OrderSide Side { get; set; }

        public OrderType Type { get; set; }

        public decimal FilledQty { get; set; }

        //public decimal? AvgPrice { get; set; }

        public decimal? LimitPrice { get; set; }

        public decimal? StopPrice { get; set; }

        /// <summary>
        /// Many to many reverse side.
        /// </summary>
        public string[] ChildrenIds { get; set; }

        public string DurationType { get; set; }

        public decimal? Duration { get; set; }

        public OrderStatus Status { get; set; }
    }
}
