using System.Diagnostics;
using BitWork;

namespace EntityWork.Model;

public readonly struct EntityWriter
{
    internal bool IsSkipped => true;

    public Span<byte> Dereference()
    {
        throw new NotImplementedException();
    }

    internal bool TryAllocate(int length, out Span<byte> span)
    {
        throw new NotImplementedException();
    }
}

