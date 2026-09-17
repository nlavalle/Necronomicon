namespace EntityWork.Internal.Heap;

public readonly struct AllocPlateToken
{
    internal int Index { get; }
    internal int Length { get; }

    internal AllocPlateToken(int index, int length)
    {
        Index = index;
        Length = length;
    }

    internal readonly bool CheckValidity(int bound)
    {
        return (ulong)(uint)Index + (ulong)(uint)Length <= (uint)bound;
    }
}
