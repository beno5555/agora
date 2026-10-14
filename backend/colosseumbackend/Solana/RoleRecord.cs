namespace colosseumbackend.Solana;

/// <summary>One wallet's history in one role (as buyer or as seller). Mirrors `RoleRecord` in state.rs.</summary>
public record RoleRecord(uint Completed, uint Disputed, uint Undelivered, uint OpenDisputes, ulong Volume)
{
    public static readonly RoleRecord Empty = new(0, 0, 0, 0, 0);

    /// <summary>Same as the program's RoleRecord::bad_marks: each one raises the deposit.</summary>
    public ulong BadMarks => (ulong)Undelivered + OpenDisputes;

    public static RoleRecord Read(ref BorshReader reader)
    {
        return new RoleRecord(reader.U32(), reader.U32(), reader.U32(), reader.U32(), reader.U64());
    }
}
