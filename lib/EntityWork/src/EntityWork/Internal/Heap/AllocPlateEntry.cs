namespace EntityWork.Internal.Heap;

public struct AllocPlateEntry
{
    internal AllocPlateToken Token;
    public int Length { get; internal set; }

    internal AllocPlateEntry(AllocPlateToken token, int length)
    {
        Token = token;
        Length = length;
    }

    internal readonly bool CheckValidity(int bound)
    {
        return Token.CheckValidity(bound) && Length <= Token.Length;
    }
}
