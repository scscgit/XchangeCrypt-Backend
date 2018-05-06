using MongoDB.Bson;
using System;

namespace XchangeCrypt.Backend.TradingBackend.Models
{
    public class TransactionHistoryEntry
    {
        public ObjectId Id { get; set; }

        public DateTime EntryTime { get; set; }

        public string User { get; set; }

        public string AccountId { get; set; }

        public string Instrument { get; set; }

        /// <summary>
        /// Optional.
        /// </summary>
        public string OrderId { get; set; }

        public decimal FilledQty { get; set; }
    }
}
