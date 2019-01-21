namespace XchangeCrypt.Backend.DatabaseAccess.Models.Enums
{
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
