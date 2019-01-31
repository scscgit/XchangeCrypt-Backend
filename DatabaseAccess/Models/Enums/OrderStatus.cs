namespace XchangeCrypt.Backend.DatabaseAccess.Models.Enums
{
    [System.Obsolete]
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
