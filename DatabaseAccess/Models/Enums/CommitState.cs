namespace XchangeCrypt.Backend.DatabaseAccess.Models.Enums
{
    [System.Obsolete]
    public enum CommitState
    {
        Initial,
        Pending,
        Applied,
        Done,
        Canceling,
        Canceled
    }
}
