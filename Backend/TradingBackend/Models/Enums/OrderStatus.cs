namespace XchangeCrypt.Backend.TradingBackend.Models.Enums
{
    public enum OrderStatus
    {
        // Order book entry
        Placing,
        Inactive,
        Working,

        // Order history entry
        Rejected,
        Filled,
        Cancelled,
    }
}
