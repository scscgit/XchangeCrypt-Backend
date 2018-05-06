using XchangeCrypt.Backend.TradingBackend.Models.Enums;

namespace XchangeCrypt.Backend.TradingBackend.Models
{
    public class ActivityHistoryWalletOperationEntry
    {
        public string Id { get; set; }

        public ActivityHistoryEntryType EntryType { get; set; }

        public string Coin { get; set; }

        public string DepositType { get; set; }

        public string WithdrawalType { get; set; }

        public decimal Amount { get; set; }
    }
}
