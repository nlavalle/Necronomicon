namespace EntityWork.Model.Types;

public enum Source2EntityFlags : byte
{
    None = 0,

    Exclude = 1 << 0,
    Array = 1 << 4,
    Heap = 1 << 5,
}
