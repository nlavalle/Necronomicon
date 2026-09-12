using System.Runtime.CompilerServices;
using BitWork.Primitives;

namespace BitWork;

internal readonly ref struct ReadOnlyLsBitWindow
{
    private readonly long _offset;
    private readonly UnsignedLsBitData _inner;

    internal UnsignedLsBitData Data => _inner;
    public int Length => _inner.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlyLsBitWindow(long bitOffset, UnsignedLsBitData data)
    {
        _offset = bitOffset;
        _inner = data;
    }

    #region Bounds

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitLengthInBounds(int bitLength)
        => _inner.IsBitLengthInBounds(bitLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitOffsetInBounds(long bitOffset)
        => IsBitOffsetInBounds(bitOffset, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitSpecInBounds(long bitOffset, int bitLength)
        => IsBitSpecInBounds(bitOffset, bitLength, out _, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsBitOffsetInBounds(long bitOffset, out int localOffset)
    {
        var calc = bitOffset - _offset;
        localOffset = (int)calc;

        return (ulong)calc < (uint)_inner.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsBitLengthInBounds(int localOffset, int bitLength, out int localTotal)
        => _inner.IsBitSpecInBounds(localOffset, bitLength, out localTotal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsBitSpecInBounds(long bitOffset, int bitLength, out int localOffset, out int localTotal)
    {
        if (!IsBitOffsetInBounds(bitOffset, out localOffset))
        {
            localTotal = 0;
            return false;
        }

        return IsBitLengthInBounds(localOffset, bitLength, out localTotal);
    }
    #endregion

    #region Slice

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyLsBitWindow Slice(int start)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start, (uint)_inner.Length, nameof(start));

        return InternalSlice(start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyLsBitWindow Slice(int start, int length)
    {
        var total = (ulong)(uint)start + (ulong)(uint)length;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(total, (ulong)(uint)_inner.Length);

        return InternalSlice(start, length, (int)total);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlyLsBitWindow InternalSlice(int bitStart)
        => new(_offset + bitStart, _inner.SliceChecked(bitStart));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlyLsBitWindow InternalSlice(int bitStart, int bitLength, int total)
        => new(_offset + bitStart, _inner.SliceChecked(bitStart, bitLength, total));

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsignedLsBitData ToByteAlignedData()
    {
        if ((_inner.Length & 7) == 0)
        {
            return _inner;
        }
        else
        {
            return _inner.TruncateChecked(_inner.Length & ~7);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsignedLsBitData ToData()
    {
        return _inner;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsignedLsBitData ToData(long bitOffset)
    {
        if (!IsBitOffsetInBounds(bitOffset, out var localOffset))
            return default;

        return new UnsignedLsBitData(_inner.Data >> localOffset, _inner.Length - localOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetData(long bitOffset, out UnsignedLsBitData data)
    {
        // _inner.Length - (bitOffset - _offset) = _inner.Length + (-bitOffset + _offset)
        if (!IsBitOffsetInBounds(bitOffset, out var localOffset))
        {
            data = default;
            return false;
        }

        data = new UnsignedLsBitData(_inner.Data >> localOffset, _inner.Length - localOffset);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetData(long bitOffset, int bitLength, out UnsignedLsBitData data)
    {
        if (!IsBitSpecInBounds(bitOffset, bitLength, out var offset, out var total))
        {
            data = default;
            return false;
        }

        data = new UnsignedLsBitData(ExtractionPrimitives.UncheckedExtractUInt64LSB(_inner.Data, offset, bitLength, total), bitLength);
        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ExtractBool(long bitOffset)
    {
        if (!IsBitOffsetInBounds(bitOffset, out var offset))
            throw new ArgumentOutOfRangeException(nameof(bitOffset));

        return _inner.InternalExtractBool(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractBool(long bitOffset, out bool result)
    {
        if (!IsBitOffsetInBounds(bitOffset, out var offset))
        {
            result = false;
            return false;
        }

        result = _inner.InternalExtractBool(offset);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractSigned<T>(long bitOffset, int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitSpecInBounds(bitOffset, bitCount, out var offset, out var total))
        {
            value = default;
            return false;
        }

        value = _inner.InternalExtractSigned<T>(offset, bitCount, total);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractUnsigned<T>(long bitOffset, int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitSpecInBounds(bitOffset, bitCount, out var offset, out var total))
        {
            value = default;
            return false;
        }

        value = _inner.InternalExtractUnsigned<T>(offset, bitCount, total);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractData(long bitOffset, int bitCount, out UnsignedLsBitData value)
    {
        if (!IsBitSpecInBounds(bitOffset, bitCount, out var offset, out var total))
        {
            value = default;
            return false;
        }

        value = _inner.InternalExtractUnsignedLsBitData(offset, bitCount, total);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractData(long bitOffset, out UnsignedLsBitData value)
    {
        if (!IsBitOffsetInBounds(bitOffset, out var offset))
        {
            value = default;
            return false;
        }

        value = _inner.InternalExtractUnsignedLsBitData(offset);
        return true;
    }

}
