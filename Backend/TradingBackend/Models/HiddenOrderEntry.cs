using XchangeCrypt.Backend.TradingBackend.Models.Enums;

namespace XchangeCrypt.Backend.TradingBackend.Models
{
    public class HiddenOrderEntry
    {
        public string Id { get; set; }

        public string User { get; set; }

        public string AccountId { get; set; }

        public string Instrument { get; set; }

        public decimal Qty { get; set; }

        public OrderSide Side { get; set; }

        public OrderType Type { get; set; }

        public decimal? LimitPrice { get; set; }

        public decimal? StopPrice { get; set; }

        public string ParentId { get; set; }

        public ParentOrderType ParentType { get; set; }
    }
}
