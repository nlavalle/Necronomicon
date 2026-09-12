using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using BitWork.Primitives;

namespace BitWork;

public readonly struct SignedBitData
{
    internal readonly long _data;
    internal readonly int _length;

    public long Data => _data;
    public int Length => _length;

    #region Construction

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SignedBitData(long data, int length)
    {
        Debug.Assert(
               (data == default && length == default)
            || (BitOperations.LeadingZeroCount((ulong)(data ^ data >> 63)) > 64 - length)
             , "Invalid data: sign bits invalid for length."
        );
        
        _data = data;
        _length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsignedLsBitData AsUnsignedLsBitData(bool truncateToLength = false)
    {
        if (truncateToLength)
            return new(ExtractionPrimitives.ExtractUInt64LSB(_data, _length), _length);

        return new((ulong)_data, 64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsignedLsBitData AsUnsignedLsBitData(int length)
    {
        if (length < _length || length > 64)
            throw new ArgumentOutOfRangeException(nameof(length));

        return new(ExtractionPrimitives.ExtractUInt64LSB(_data, length), length);
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T AsSigned<T>() where T : unmanaged
    {
        long result = _data;

        return Unsafe.As<long, T>(ref result);
    }
}
