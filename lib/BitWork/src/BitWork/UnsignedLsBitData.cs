using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitWork.Primitives;

namespace BitWork;

public readonly struct UnsignedLsBitData
{
    private readonly ulong _data;
    private readonly int _length;

    public ulong Data => _data;
    public int Length => _length;

    #region Construction

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UnsignedLsBitData(ulong data, int length)
    {
        Debug.Assert(
               BitOperations.LeadingZeroCount(data) > 63 - length
             , "Invalid data: zero bits invalid for length."
        );

        _data = data;
        _length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnsignedLsBitData Create(ulong data)
    {
        return new UnsignedLsBitData(data, 64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnsignedLsBitData Create(ulong data, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, 64U, nameof(length));

        return new UnsignedLsBitData(ExtractionPrimitives.ExtractUInt64LSB(data, length), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SignedBitData AsSignedBitData()
    {
        return new SignedBitData(ExtractionPrimitives.ExtractInt64LSB(_data, _length), _length);
    }

    #endregion

    #region Bounds

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitLengthInBounds(int bitLength)
        => (uint)bitLength <= (uint)_length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitOffsetInBounds(int bitOffset)
        => (uint)bitOffset < (uint)_length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitSpecInBounds(int bitOffset, int bitLength)
        => IsBitSpecInBounds(bitOffset, bitLength, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsBitSpecInBounds(int bitOffset, int bitLength, out int localTotal)
    {
        var total = (ulong)(uint)bitOffset + (ulong)(uint)bitLength;

        localTotal = (int)total;
        return total <= (ulong)(uint)_length;
    }

    #endregion

    #region Slice

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsignedLsBitData Slice(int start)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start, (uint)_length, nameof(start));

        return SliceChecked(start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsignedLsBitData Slice(int start, int length)
    {
        var total = (ulong)(uint)start + (ulong)(uint)length;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(total, (ulong)(uint)_length);

        return SliceChecked(start, length, (int)total);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsignedLsBitData Truncate(int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)_length, nameof(length));

        return TruncateChecked(length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UnsignedLsBitData SliceChecked(int start)
    {
        if (start > 63)
            return default;

        return SliceUnchecked(start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UnsignedLsBitData SliceChecked(int start, int length, int total)
        => new(ExtractionPrimitives.ExtractUInt64LSB(_data, start, length, total), length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UnsignedLsBitData TruncateChecked(int length)
        => new(ExtractionPrimitives.ExtractUInt64LSB(_data, length), length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UnsignedLsBitData SliceUnchecked(int start)
    {
        Debug.Assert((uint)start <= (uint)_length);
        Debug.Assert(start != 64);

        return new(_data >> start, _length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UnsignedLsBitData TruncateUnchecked(int length)
    {
        Debug.Assert((uint)length <= (uint)_length);
        Debug.Assert(length != 0);

        return new(ExtractionPrimitives.UncheckedExtractUInt64LSB(_data, length), length);
    }

    #endregion

    #region Bool Extract

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractBool(int bitOffset, out bool value)
    {
        if (!IsBitOffsetInBounds(bitOffset))
        {
            value = default;
            return false;
        }

        value = InternalExtractBool(bitOffset);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractBool(out bool value)
    {
        if (!IsBitOffsetInBounds(0))
        {
            value = default;
            return false;
        }

        value = InternalExtractBool();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalExtractBool(int bitOffset)
    {
        return ExtractionPrimitives.ExtractBoolLSB(_data, bitOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalExtractBool()
    {
        return ExtractionPrimitives.ExtractBoolLSB(_data);
    }

    #endregion

    #region Signed Extract

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractSigned<T>(int bitOffset, int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitSpecInBounds(bitOffset, bitCount, out var total))
        {
            value = default;
            return false;
        }

        value = InternalExtractSigned<T>(bitOffset, bitCount, total);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractSigned<T>(int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitLengthInBounds(bitCount))
        {
            value = default;
            return false;
        }

        value = InternalExtractSigned<T>(bitCount);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractSignedData(int bitCount, out SignedBitData value)
    {
        if (!IsBitLengthInBounds(bitCount))
        {
            value = default;
            return false;
        }

        value = InternalExtractSignedData(bitCount);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractSignedData(int bitOffset, int bitCount, out SignedBitData value)
    {
        if (!IsBitSpecInBounds(bitOffset, bitCount, out var total))
        {
            value = default;
            return false;
        }

        value = InternalExtractSignedData(bitOffset, bitCount, total);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractSigned<T>(int bitOffset, int bitCount, int total) where T : unmanaged
    {
        long result = ExtractionPrimitives.ExtractInt64LSB(_data, bitOffset, bitCount, total);

        return Unsafe.As<long, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractSigned<T>(int bitCount) where T : unmanaged
    {
        long result = ExtractionPrimitives.ExtractInt64LSB(_data, bitCount);

        return Unsafe.As<long, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SignedBitData InternalExtractSignedData(int bitOffset, int bitCount, int total)
    {
        long result = ExtractionPrimitives.ExtractInt64LSB(_data, bitOffset, bitCount, total);

        return new(result, bitCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SignedBitData InternalExtractSignedData(int bitCount)
    {
        long result = ExtractionPrimitives.ExtractInt64LSB(_data, bitCount);

        return new(result, bitCount);
    }

    #endregion

    #region  Unsigned Extract

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T AsUnsigned<T>() where T : unmanaged
    {
        ulong result = _data;

        return Unsafe.As<ulong, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractUnsigned<T>(int bitOffset, int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitSpecInBounds(bitOffset, bitCount, out var total))
        {
            value = default;
            return false;
        }

        value = InternalExtractUnsigned<T>(bitOffset, bitCount, total);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractUnsigned<T>(int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitLengthInBounds(bitCount))
        {
            value = default;
            return false;
        }

        value = InternalExtractUnsigned<T>(bitCount);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractUnsignedLsData(int bitCount, out UnsignedLsBitData value)
    {
        if (!IsBitLengthInBounds(bitCount))
        {
            value = default;
            return false;
        }

        value = InternalExtractUnsignedLsBitData(bitCount);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractUnsignedLsData(int bitOffset, int bitCount, out UnsignedLsBitData value)
    {
        if (!IsBitSpecInBounds(bitOffset, bitCount, out var total))
        {
            value = default;
            return false;
        }

        value = InternalExtractUnsignedLsBitData(bitOffset, bitCount, total);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractUnsigned<T>(int bitOffset, int bitCount, int total) where T : unmanaged
    {
        ulong result = ExtractionPrimitives.ExtractUInt64LSB(_data, bitOffset, bitCount, total);

        return Unsafe.As<ulong, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractUnsigned<T>(int bitCount) where T : unmanaged
    {
        ulong result = ExtractionPrimitives.ExtractUInt64LSB(_data, bitCount);

        return Unsafe.As<ulong, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UnsignedLsBitData InternalExtractUnsignedLsBitData(int bitOffset, int bitCount, int total)
    {
        ulong result = ExtractionPrimitives.ExtractUInt64LSB(_data, bitOffset, bitCount, total);

        return new(result, bitCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UnsignedLsBitData InternalExtractUnsignedLsBitData(int bitCount)
    {
        ulong result = ExtractionPrimitives.ExtractUInt64LSB(_data, bitCount);

        return new(result, bitCount);
    }

    #endregion

    #region Concatenate

    public bool TryConcatenate(scoped in UnsignedLsBitData upper, out UnsignedLsBitData concatenated)
    {
        var total = _length + upper.Length;
        if (total > 64)
        {
            concatenated = this;
            return false;
        }

        concatenated = InternalConcatenateUnsigned(in upper, total);
        return true;
    }

    public bool TryConcatenate(scoped in SignedBitData upper, out SignedBitData concatenated)
    {
        var total = _length + upper.Length;
        if (total > 64)
        {
            concatenated = default;
            return false;
        }

        concatenated = InternalConcatenateSigned(in upper);
        return true;
    }

    internal UnsignedLsBitData InternalConcatenateUnsigned(scoped in UnsignedLsBitData upper)
    {
        return InternalConcatenateUnsignedLs(upper._data, Math.Min(_length + upper._length, 64));
    }

    internal UnsignedLsBitData ConcatenateRotateUnsignedChecked(scoped ref UnsignedLsBitData upper)
    {
        var upperData = upper._data;

        var upperLength = _length + upper.Length;
        if (upperLength > 64)
        {
            upper = new(upperData >> -_length, upperLength & 63);

            return InternalConcatenateUnsignedLs(upperData, 64);
        }
        else
        {
            upper = default;

            return InternalConcatenateUnsignedLs(upperData, upperLength & 63);
        }
    }

    internal UnsignedLsBitData ConcatenateRotateUnsignedUnchecked(scoped ref UnsignedLsBitData upper)
    {
        return ConcatenateRotateUnsignedUnchecked(ref upper, _length + upper._length);
    }

    internal UnsignedLsBitData ConcatenateRotateUnsignedUnchecked(scoped ref UnsignedLsBitData upper, int upperLength)
    {
        Debug.Assert(upperLength > 64);

        var upperData = upper._data;

        upper = new(upperData >> -_length, upperLength & 63);

        return InternalConcatenateUnsignedLs(upperData, 64);
    }

    internal UnsignedLsBitData InternalConcatenateUnsigned(scoped in UnsignedLsBitData upper, int length)
    {
        return InternalConcatenateUnsignedLs(upper._data, length);
    }

    internal UnsignedLsBitData InternalConcatenateUnsignedLs(ulong upper, int length)
    {
        return new UnsignedLsBitData(_data | (upper << _length), length);
    }

    internal SignedBitData InternalConcatenateSigned(scoped in SignedBitData upper)
    {
        return InternalConcatenateSigned(upper._data, Math.Min(_length + upper._length, 64));
    }

    internal SignedBitData InternalConcatenateSigned(long upper)
    {
        return InternalConcatenateSigned(upper, 64);
    }

    internal SignedBitData InternalConcatenateSigned(long upper, int length)
    {
        return new SignedBitData((long)_data | (upper << _length), length);
    }

    #endregion

    #region Utilities

    public bool TryFindLowestZeroByte(out int position)
    {
        var tzc = GenerationPrimitives.TzcLowestZeroHighestBit(_data);
        position = tzc >> 3;
        return tzc < _length;
    }

    #endregion

    #region Copying

    public bool TryWriteTo(Span<byte> span)
    {
        if (_length == 0)
            return true;

        if (span.Length >= 8)
        {
            MemoryPrimitives.WriteUInt64UncheckedLSB(ref MemoryMarshal.GetReference(span), _data);
            return true;
        }

        var byteLength = (_length + 7) >> 3;
        if (byteLength > span.Length)
            return false;

        // Debug.Assert(byteLength >= 1 && byteLength <= 7);
        // Debug.Assert(span.Length >= 1 && span.Length <= 7);

        WriteToImbalanced(span, byteLength);

        return true;
    }

    internal int WriteToUnknownAligned(Span<byte> span)
    {
        Debug.Assert(_length > 0);

        var bitLength = _length;
        if (span.Length >= 8)
        {
            MemoryPrimitives.WriteUInt64UncheckedLSB(ref MemoryMarshal.GetReference(span), _data);
        }
        else
        {
            var byteLength = bitLength + 7 >> 3;
            byteLength = Math.Min(byteLength, span.Length);

            WriteToImbalanced(span, byteLength);
            bitLength = byteLength << 3;
        }

        return bitLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteToImbalanced(Span<byte> span, int byteLength)
    {
        Debug.Assert(byteLength > 0);
        Debug.Assert(byteLength <= span.Length);

        int destAlignment;

        ref byte reference = ref MemoryMarshal.GetReference(span);
        //var byteLength = Math.Min((_length + 7) >> 3, span.Length);

        switch (byteLength)
        {
            case 1:
                reference = (byte)_data;
                break;
            case 2:
                MemoryPrimitives.WriteUInt16UncheckedLSB(ref reference, (ushort)_data);
                break;
            case 3:
                if (span.Length >= 4)
                {
                    MemoryPrimitives.WriteUInt32UncheckedLSB(ref reference, (uint)_data);
                }
                else
                {
                    destAlignment = GetAlignment(ref reference);
                    if ((destAlignment & 1) != 0)
                    {
                        reference = (byte)_data;
                        MemoryPrimitives.WriteUInt16UncheckedLSB(ref Unsafe.Add(ref reference, 1U), (ushort)(_data >> 8));
                    }
                    else
                    {
                        MemoryPrimitives.WriteUInt16UncheckedLSB(ref reference, (ushort)_data);
                        Unsafe.Add(ref reference, 2U) = (byte)(_data >> 16);
                    }
                }
                break;
            case 4:
                MemoryPrimitives.WriteUInt32UncheckedLSB(ref reference, (uint)_data);
                break;
            case 5:
                destAlignment = GetAlignment(ref reference);
                if ((destAlignment & 1) != 0)
                {
                    reference = (byte)_data;
                    MemoryPrimitives.WriteUInt32UncheckedLSB(ref Unsafe.Add(ref reference, 1U), (uint)(_data >> 8));
                }
                else
                {
                    MemoryPrimitives.WriteUInt32UncheckedLSB(ref reference, (uint)_data);
                    Unsafe.Add(ref reference, 4U) = (byte)(_data >> 32);
                }
                break;
            case 6:
                destAlignment = GetAlignment(ref reference);
                if ((destAlignment & 1) != 0)
                {
                    reference = (byte)_data;
                    MemoryPrimitives.WriteUInt32UncheckedLSB(ref Unsafe.Add(ref reference, 1U), (uint)(_data >> 8));
                    Unsafe.Add(ref reference, 5U) = (byte)(_data >> 40);
                }
                else
                {
                    if ((destAlignment & 2) != 0)
                    {
                        MemoryPrimitives.WriteUInt16UncheckedLSB(ref reference, (ushort)_data);
                        MemoryPrimitives.WriteUInt32UncheckedLSB(ref Unsafe.Add(ref reference, 2U), (uint)(_data >> 16));
                    }
                    else
                    {
                        MemoryPrimitives.WriteUInt32UncheckedLSB(ref reference, (uint)_data);
                        MemoryPrimitives.WriteUInt16UncheckedLSB(ref Unsafe.Add(ref reference, 4U), (ushort)(_data >> 32));
                    }
                }
                break;
            case 7:
                destAlignment = GetAlignment(ref reference);
                switch (destAlignment & 3)
                {
                    case 0:
                        MemoryPrimitives.WriteUInt32UncheckedLSB(ref reference, (uint)_data);
                        MemoryPrimitives.WriteUInt16UncheckedLSB(ref Unsafe.Add(ref reference, 4U), (ushort)(_data >> 32));
                        Unsafe.Add(ref reference, 6U) = (byte)(_data >> 48);
                        break;
                    case 1:
                        reference = (byte)_data;
                        MemoryPrimitives.WriteUInt16UncheckedLSB(ref Unsafe.Add(ref reference, 1U), (ushort)(_data >> 8));
                        MemoryPrimitives.WriteUInt32UncheckedLSB(ref Unsafe.Add(ref reference, 3U), (uint)(_data >> 24));
                        break;
                    case 2:
                        MemoryPrimitives.WriteUInt16UncheckedLSB(ref reference, (ushort)_data);
                        MemoryPrimitives.WriteUInt32UncheckedLSB(ref Unsafe.Add(ref reference, 2U), (uint)(_data >> 16));
                        Unsafe.Add(ref reference, 6U) = (byte)(_data >> 48);
                        break;
                    case 3:
                        reference = (byte)_data;
                        MemoryPrimitives.WriteUInt32UncheckedLSB(ref Unsafe.Add(ref reference, 1U), (uint)(_data >> 8));
                        MemoryPrimitives.WriteUInt16UncheckedLSB(ref Unsafe.Add(ref reference, 5U), (ushort)(_data >> 40));
                        break;
                    default:
                        throw new UnreachableException();
                }
                break;
            default:
                throw new UnreachableException();
        }

        static int GetAlignment(ref byte dst)
        {
            return -(int)GenerationPrimitives.CalculateAddress(ref dst);
        }
    }

    internal int WriteToKnownAligned(Span<byte> span)
    {
        Debug.Assert((_length & 7) == 0);
        Debug.Assert(_length > 0);

        if (span.Length == 0)
            return 0;

        ref byte reference = ref MemoryMarshal.GetReference(span);
        var data = _data;

        if (span.Length >= 8)
        {
            MemoryPrimitives.WriteUInt64UncheckedLSB(ref reference, data);

            return _length;
        }

        var spanLength = span.Length;

        if (spanLength >= 4)
        {
            var tmpInt = (uint)data;

            MemoryPrimitives.WriteUInt32UncheckedLSB(ref reference, (uint)data);

            if (_length <= 32)
            {
                Debug.Assert(_length <= span.Length * 8);
                return _length;
            }

            data >>= 32;
            reference = ref Unsafe.Add(ref reference, 4U);
            spanLength -= 4;
        }

        if ((_length & 16) != 0)
        {
            var tmpShort = MemoryPrimitives.LocalizeEndiannessLSB((ushort)data);

            if (spanLength == 1)
            {
                Debug.Assert(span.Length == 1 || span.Length == 5);

                reference = (byte)tmpShort;

                Debug.Assert(_length > span.Length * 8);
                return span.Length << 3;
            }

            Debug.Assert(span.Length == 2 || span.Length >= 6);

            Unsafe.WriteUnaligned(ref reference, tmpShort);

            data >>= 16;
            reference = ref Unsafe.Add(ref reference, 2U);
            spanLength -= 2;
        }

        if ((_length & 8) != 0 && spanLength > 0)
        {
            reference = (byte)data;

            Debug.Assert(_length <= span.Length * 8);
            return _length;
        }

        Debug.Assert(_length > span.Length * 8);
        return span.Length << 3;
    }

    #endregion
}
