using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitWork.Primitives;

namespace BitWork;

public readonly ref partial struct ReadOnlyAlignedBitSpan
{
    internal readonly ref ulong _reference;
    private readonly uint _bitAlignment;
    private readonly long _bitLength;

    public long Length => _bitLength;

    internal ReadOnlyAlignedBitSpan(ref ulong reference, uint bitAlignment, long bitLength)
    {
        Debug.Assert(bitAlignment < 64U);
        Debug.Assert((ulong)bitLength <= (ulong)Array.MaxLength * sizeof(ulong) * 8);

        _reference = ref reference;
        _bitAlignment = bitAlignment;
        _bitLength = bitLength;
    }

    #region Slice

    public ReadOnlyAlignedBitSpan Slice(long bitStart)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)bitStart, (ulong)_bitLength, nameof(bitStart));

        return InternalSlice(bitStart);
    }

    public ReadOnlyAlignedBitSpan Slice(long bitStart, long bitLength)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)bitStart, (ulong)_bitLength, nameof(bitStart));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)bitLength, (ulong)(_bitLength - bitStart), nameof(bitLength));

        return InternalSlice(bitStart, bitLength);
    }

    internal ReadOnlyAlignedBitSpan InternalSlice(long bitStart)
    {
        return InternalSlice(bitStart, _bitLength - bitStart);
    }

    internal ReadOnlyAlignedBitSpan InternalSlice(long bitStart, long bitLength)
    {
        Debug.Assert((ulong)bitStart <= (ulong)_bitLength);
        Debug.Assert((ulong)bitLength <= (ulong)(_bitLength - bitStart));

        if (bitLength == 0)
            return default;

        var bitAlignment = _bitAlignment + bitStart;
        var newAlignment = (uint)bitAlignment & 63;
        var longAlignment = bitAlignment >> 6;

        return new ReadOnlyAlignedBitSpan(ref Unsafe.Add(ref _reference, (nuint)longAlignment), newAlignment, bitLength);
    }

    #endregion

    #region Bounds

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitOffsetInBounds(long bitOffset)
        => (ulong)_bitLength > (ulong)bitOffset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitSpecInBounds(long bitOffset, int bitLength)
    {
        // See: SubCmpAssembly in benchmarks
        // var remaining = (ulong)lengthInner - (ulong)offsetInput;
        // return remaining <= (ulong)lengthInner && remaining >= (uint)length;

        return (ulong)_bitLength >= (ulong)bitOffset && (ulong)_bitLength - (ulong)bitOffset >= (uint)bitLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitSpecInBounds(long bitOffset, long bitLength)
    {
        // See: SubCmpAssembly in benchmarks
        // var remaining = (ulong)lengthInner - (ulong)offsetInput;
        // return remaining <= (ulong)lengthInner && remaining >= (ulong)length;

        return (ulong)_bitLength >= (ulong)bitOffset && (ulong)_bitLength - (ulong)bitOffset >= (ulong)bitLength;
    }

    #endregion

    #region Window

    internal ReadOnlyLsBitWindow GetLsWindow(long bitOffset, bool greedy = false)
    {
        Debug.Assert((ulong)bitOffset < (ulong)_bitLength);

        UnsignedLsBitData lowerData;

        scoped var helper = GetUnsafeReferenceHelper(bitOffset, out var startAlignment, out var endAlignment);

        if (helper.IsFinalReference)
        {
            lowerData = helper.ToDataSliced(startAlignment, endAlignment);
        }
        else
        {
            lowerData = helper.ToDataSliced(startAlignment);

            if (greedy)
            {
                UnsignedLsBitData upperData;

                helper++;

                if (helper.IsFinalReference)
                {
                    upperData = helper.ToDataTruncated(endAlignment);
                }
                else
                {
                    upperData = helper.ToData();
                }

                lowerData = lowerData.InternalConcatenateUnsigned(in upperData);
            }
        }

        return new(bitOffset, lowerData);
    }

    internal ReadOnlyLsBitWindow GetNearLsWindow(long bitOffset, int bitLength)
    {
        Debug.Assert((ulong)bitOffset < (ulong)_bitLength);
        Debug.Assert((uint)bitLength <= 64);
        Debug.Assert(bitOffset + (uint)bitLength <= _bitLength);

        UnsignedLsBitData lowerData;

        scoped var helper = GetUnsafeReferenceHelper(bitOffset, out var startAlignment, out var endAlignment);

        if (helper.IsFinalReference)
        {
            lowerData = helper.ToDataSliced(startAlignment, endAlignment);
        }
        else
        {
            lowerData = helper.ToDataSliced(startAlignment);

            if (bitLength > lowerData.Length)
            {
                UnsignedLsBitData upperData;

                helper++;

                if (helper.IsFinalReference)
                {
                    upperData = helper.ToDataTruncated(endAlignment);
                }
                else
                {
                    upperData = helper.ToData();
                }

                lowerData = lowerData.InternalConcatenateUnsigned(in upperData);
            }
        }

        return new(bitOffset, lowerData);
    }

    #endregion

    #region Copy

    internal int GetByteCountUntilZeroLSB(scoped in ReadOnlyLsBitWindow window, long bitOffset)
    {
        var bitRemaining = _bitLength - bitOffset;
        if (bitRemaining < 8)
            return -1;

        UnsignedLsBitData lowerData;
        int byteCount = 0;
        if (window.TryGetData(bitOffset, out lowerData))
        {
            if (lowerData.Length == bitRemaining)
            {
                // shortcut for the window containing all data
                if (lowerData.TryFindLowestZeroByte(out var lzb))
                    return lzb;

                return -1;
            }

            Debug.Assert(lowerData.Length < bitRemaining);

            if (lowerData.Length >= 8)
            {
                // check the existing window
                if (lowerData.TryFindLowestZeroByte(out var lzb))
                    return lzb;
            }

            byteCount = lowerData.Length >> 3;
            bitOffset += byteCount << 3;
        }

        scoped var helper = GetUnsafeReferenceHelper(bitOffset, out var startAlignment, out var endAlignment);

        if (helper.IsFinalReference)
        {
            // one buffer check
            lowerData = helper.ToDataSliced(startAlignment, endAlignment);

            if (lowerData.TryFindLowestZeroByte(out var lzb))
                return byteCount + lzb;

            return -1;
        }

        if ((startAlignment & 7) == 0)
        {
            // if byte aligned, just let the default implementation do it
            const byte zero = 0;

            var sliced = helper.ToSpan(startAlignment, endAlignment);

            var index = sliced.IndexOf(zero);
            if (index >= 0)
                index += byteCount;

            return index;
        }

        lowerData = helper++.ToDataSliced(startAlignment);

        bool loop = true;
        do
        {
            UnsignedLsBitData upperData;

            if (helper.IsFinalReference)
            {
                upperData = helper.ToDataTruncated(endAlignment);

                var length = lowerData.Length + upperData.Length;
                if (length <= 64 || (length & 63) <= 7)
                {
                    if (length <= 7)
                        return -1;

                    lowerData = lowerData.InternalConcatenateUnsignedLs(upperData.Data, length);

                    break;
                }

                loop = false;
            }
            else
            {
                upperData = helper++.ToData();
            }

            lowerData = lowerData.ConcatenateRotateUnsignedUnchecked(ref upperData);

            var check = lowerData.TryFindLowestZeroByte(out var lzb);
            byteCount += lzb;

            if (check)
                return byteCount;

            lowerData = upperData;
        } while (loop);

        Debug.Assert(lowerData.Length > 7);

        if (lowerData.TryFindLowestZeroByte(out var tzc))
            return byteCount + tzc;

        return -1;
    }

    internal void CopyBytesLSB(long bitOffset, long bitLength, scoped Span<byte> dest)
    {
        Debug.Assert(bitLength <= (long)(uint)dest.Length << 3);
        Debug.Assert(bitOffset + bitLength <= _bitLength);

        UnsignedLsBitData nextData;
        scoped var helper = GetUnsafeReferenceHelper(bitOffset, bitLength, out var startAlignment, out var endAlignment);

        if (bitLength <= 64)
        {
            UnsignedLsBitData copyData;

            if (helper.IsFinalReference)
            {
                copyData = helper.ToDataSliced(startAlignment, endAlignment);
            }
            else
            {
                copyData = helper++.ToDataSliced(startAlignment);
                nextData = helper.ToDataTruncated(endAlignment);
                copyData.InternalConcatenateUnsigned(in nextData);
            }

            Debug.Assert(copyData.Length == bitLength);

            copyData.WriteToUnknownAligned(dest);

            return;
        }

        nextData = helper++.ToDataSliced(startAlignment);

        var misalignment = GetReferenceMisalignment(ref MemoryMarshal.GetReference(dest)) << 3;
        if (misalignment != 0)
        {
            UnsignedLsBitData copyData;

            var bitMisalignment = misalignment << 3;

            var adjustment = bitMisalignment - nextData.Length;
            if (adjustment > 0)
            {
                copyData = nextData;

                HelperOperation(ref helper, endAlignment);

                copyData.InternalConcatenateUnsignedLs(ExtractionPrimitives.UncheckedExtractUInt64LSB(nextData.Data, adjustment), bitMisalignment);
                nextData = nextData.SliceUnchecked(adjustment);
            }
            else if (adjustment == 0)
            {
                copyData = nextData;

                HelperOperation(ref helper, endAlignment);
            }
            else
            {
                copyData = nextData.TruncateUnchecked(bitMisalignment);
                nextData = nextData.SliceUnchecked(bitMisalignment);
            }

            Debug.Assert(copyData.Length == bitMisalignment);
            Debug.Assert(nextData.Length != 0);

            copyData.WriteToUnknownAligned(dest);

            dest = dest.Slice(misalignment);

            static UnsignedLsBitData HelperOperation(ref UnsafeReferenceHelper helper, int endAlignment)
            {
                if (helper.IsFinalReference)
                {
                    return helper.ToDataTruncated(endAlignment);
                }
                else
                {
                    return helper++.ToData();
                }
            }
        }

        if (nextData.Length == 64)
        {
            while (true)
            {
                nextData.WriteToKnownAligned(dest);

                dest = dest.Slice(8);

                if (helper.IsFinalReference)
                {
                    nextData = helper.ToDataTruncated(endAlignment);
                    break;
                }
                else
                {
                    nextData = helper++.ToData();
                }
            }
        }
        else
        {
            bool loop = true;

            do
            {
                var copyData = nextData;

                if (helper.IsFinalReference)
                {
                    nextData = helper.ToDataTruncated(endAlignment);

                    var lengthCheck = copyData.Length + nextData.Length;
                    if (lengthCheck <= 64)
                    {
                        nextData = copyData.InternalConcatenateUnsigned(in nextData);
                        break;
                    }
                    else
                    {
                        copyData = copyData.ConcatenateRotateUnsignedUnchecked(ref nextData, lengthCheck);
                        loop = false;
                    }
                }
                else
                {
                    nextData = helper++.ToData();
                    copyData = copyData.ConcatenateRotateUnsignedUnchecked(ref nextData);
                }

                copyData.WriteToKnownAligned(dest);

                dest = dest.Slice(8);
            } while (loop);
        }

        Debug.Assert(helper.IsFinalReference);

        int write = nextData.WriteToKnownAligned(dest);
        Debug.Assert(write >= nextData.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryCopyBytesUntilZeroLSB(scoped ref ReadOnlyLsBitWindow window, long bitOffset, scoped Span<byte> dest, out long copied)
    {
        // byteLimit, srcAddr, dstAddr, dstOrigin, lowerData, upperData, lzb
        var bitLimit = _bitLength - bitOffset;
        if (bitLimit < 8)
        {
            goto NOSRC;
        }

        // Shortcut for zero-length spans
        if (dest.Length == 0)
        {
            goto NODST;
        }

        var byteLimit = (int)(bitLimit >> 3);
        if (byteLimit > dest.Length)
        {
            byteLimit = dest.Length + 1;
        }

        Debug.Assert(byteLimit > 0);

        int bytePosition = 0;
        UnsignedLsBitData lowerData;

        // check the current window to see if it will fulfill the request
        if (window.TryGetData(bitOffset, out lowerData) && lowerData.Length >= 8)
        {
            bool zeroFound;
            int truncate;

            if (lowerData.TryFindLowestZeroByte(out var lzb) && (zeroFound = lzb <= byteLimit))
            {
                truncate = lzb << 3;
            }
            else
            {
                zeroFound = false;
                truncate = byteLimit - 1 << 3;
            }

            if (zeroFound || (bytePosition = lowerData.Length >> 3) >= byteLimit)
            {
                if (truncate != 0)
                {
                    lowerData.TruncateUnchecked(truncate).WriteToUnknownAligned(dest);
                }

                copied = (uint)truncate;
                return zeroFound;
            }
        }

        // if data fits in a single temporal block, window and check it
        if (byteLimit <= 8)
        {
            var localRemaining = byteLimit << 3;

            lowerData = ConcatenateNearWindow(in window, bitOffset, localRemaining);
            window = new(bitOffset, lowerData);

            bool zeroFound;
            int truncate;

            if (lowerData.TryFindLowestZeroByte(out var lzb) && (zeroFound = lzb <= byteLimit))
            {
                truncate = lzb << 3;
            }
            else
            {
                zeroFound = false;
                truncate = localRemaining - 8;
            }

            if (truncate != 0)
            {
                lowerData.TruncateUnchecked(truncate).WriteToUnknownAligned(dest);
            }

            copied = (uint)truncate;
            return zeroFound;
        }

        bitLimit = (long)(uint)byteLimit << 3;

        var misalignment = GetReferenceMisalignment(ref MemoryMarshal.GetReference(dest));

        long copyCount = 0;
        scoped UnsafeReferenceHelper helper = GetUnsafeReferenceHelper(bitOffset, out var startAlignment, out var endAlignment);
        ulong lower;

        // Here:
        // 0 <= startAlignment <= 63
        // 0 <= endAlignment <= 63
        // 0 <= lowerData.Length <= 64 (0 <= searchPosition <= 8)
        // searchPosition == lowerData.Length >> 3
        // 0 <= misalignment <= 7
        // 0 <= startAlignment + lowerData.Length <= 127
        // 0 <= startAlignment + bitMisalignment <= 119
        // need to re-read lower if: startAlignment + lowerData.Length < 64 && startAlignment + bitMisalignment < 64
        // else, need to truncate lower at: 64 - startAlignment

        if (misalignment != 0)
        {
            // 1 <= misalignment <= 7 (8 <= bitMisalignment <= 56)
            // 8 <= startAlignment + bitMisalignment <= 119

            var bitMisalignment = misalignment << 3;

            if (bytePosition >= misalignment)
            {
                // lowerData contains enough data to align the destination
                lowerData.TruncateUnchecked(bitMisalignment).WriteToUnknownAligned(dest);
                copyCount += bitMisalignment;

                startAlignment += bitMisalignment;
                if (startAlignment > 63)
                {
                    startAlignment &= 63;
                    helper++;
                }

                lower = helper;
            }
            else
            {
                // lowerData does not contain enough data to align the destination
                // ignore the window
                lowerData = helper.ToDataSliced(startAlignment);
                startAlignment += bitMisalignment;

                ulong upper;

                if (startAlignment >= 64)
                {
                    upper = ++helper;
                    lowerData = lowerData.InternalConcatenateUnsignedLs(upper, 64);
                }
                else
                {
                    upper = helper;
                }

                int truncate = 0;

                if (lowerData.TryFindLowestZeroByte(out var lzb))
                {
                    truncate = lzb << 3;
                    lowerData = lowerData.TruncateUnchecked(truncate);
                }

                Debug.Assert(dest.Length >= 8);
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(dest), lowerData.Data);

                if (truncate != 0)
                {
                    copied = (uint)truncate;
                    return true;
                }

                copyCount += bitMisalignment;
                startAlignment &= 63;
                lower = upper;
            }

            dest = dest.Slice(misalignment);
            byteLimit -= misalignment;
            bitLimit -= bitMisalignment;
        }
        else
        {
            lower = helper;
        }

        int iters = (int)(bitLimit >> 3);
        if (iters > byteLimit)
        {
            iters = byteLimit;
        }

        ref ulong dstRef = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(dest));

        iters >>= 3;
        dest = dest.Slice(iters << 3);

        if (startAlignment == 0)
        {
            int truncate, lzb;

            while (true)
            {
                if (GenerationPrimitives.TryFindLowestZeroByte(lower, 64, out lzb))
                {
                    // near window here
                    window = new(bitOffset + copyCount, UnsignedLsBitData.Create(lower));

                    if (lzb != 0)
                    {
                        truncate = lzb << 3;

                        dstRef = ExtractionPrimitives.UncheckedExtractUInt64LSB(lower, truncate);

                        copyCount += (uint)truncate;
                    }

                    copied = copyCount;
                    return true;
                }

                dstRef = lower;

                copyCount += 64;

                if (--iters == 0)
                    break;

                dstRef = ref Unsafe.Add(ref dstRef, 1U);
                lower = ++helper;
            }

            // Here:
            // startAlignment == 0
            // 0 <= endAlignment <= 63
            // misalignment == 0
            // 0 <= bitLimit
            // 0 <= dest.Length
            // bitLimit <= 63 || dest.Length <= 7

            if (helper.IsFinalReference)
            {
                copied = copyCount;
                return false;
            }

            if ((++helper).IsFinalReference)
            {
                if (endAlignment <= 7)
                {
                    copied = copyCount;
                    return false;
                }

                lowerData = helper.ToDataTruncated(endAlignment);
            }
            else
            {
                lowerData = helper.ToData();
            }

            window = new(bitOffset + copyCount, lowerData);

            bool result = false;

            if (lowerData.TryFindLowestZeroByte(out lzb))
            {
                if (lzb == 0)
                {
                    copied = copyCount;
                    return true;
                }

                if (lzb <= dest.Length + 1)
                {
                    truncate = lzb << 3;
                    result = true;
                }
                else
                {
                    truncate = dest.Length << 3;
                }
            }
            else
            {
                truncate = lowerData.Length & ~7;
            }

            copyCount += (uint)lowerData.TruncateUnchecked(truncate).WriteToKnownAligned(dest);

            copied = copyCount;
            return result;
        }
        else
        {
            while (true)
            {
                ulong upper = ++helper;
                lower = lower >> startAlignment | upper << -startAlignment;

                int truncate = 0;

                if (GenerationPrimitives.TryFindLowestZeroByte(lower, 64, out var lzb))
                {

                    if (lzb == 0)
                    {
                        // near window here
                        window = new(bitOffset + copyCount, UnsignedLsBitData.Create(lower));
                    }
                    else
                    {
                        truncate = lzb << 3;

                        dstRef = ExtractionPrimitives.UncheckedExtractUInt64LSB(lower, truncate);
                        copyCount += (uint)truncate;

                        // TODO
                        // far window here
                        window = new(bitOffset + copyCount, UnsignedLsBitData.Create(lower));
                    }

                    copied = copyCount;
                    return true;
                }

                dstRef = lower;
                copyCount += 64;

                lower = upper;

                if (--iters == 0)
                    break;

                dstRef = ref Unsafe.Add(ref dstRef, 1U);
            }

            // Here:
            // 0 < startAlignment <= 63
            // 0 <= endAlignment <= 63
            // misalignment == 0
            // 0 <= bitLimit
            // 0 <= dest.Length
            // bitLimit <= 63 || dest.Length <= 7

            if (helper.IsFinalReference)
            {
                int length = endAlignment - startAlignment;
                Debug.Assert(length >= 0);

                if (length == 0)
                {
                    copied = copyCount;
                    return false;
                }

                lowerData = UnsignedLsBitData.Create(ExtractionPrimitives.UncheckedExtractUInt64LSB(lower, startAlignment, length, endAlignment), length);
                window = new(bitOffset + copyCount, lowerData);

                if (length <= 7)
                {
                    copied = copyCount;
                    return false;
                }

                int truncate;
                bool result = false;

                if (lowerData.TryFindLowestZeroByte(out var lzb))
                {
                    if (lzb == 0)
                    {
                        copied = copyCount;
                        return true;
                    }

                    if (lzb <= dest.Length + 1)
                    {
                        truncate = lzb << 3;
                        result = true;
                    }
                    else
                    {
                        truncate = dest.Length << 3;
                    }
                }
                else
                {
                    truncate = length & ~7;
                }

                copied = copyCount + (uint)lowerData.TruncateUnchecked(truncate).WriteToKnownAligned(dest);
                return result;
            }
            else
            {
                lowerData = UnsignedLsBitData.Create(ExtractionPrimitives.UncheckedExtractUInt64LSB(lower, startAlignment), 64 - startAlignment);

                int truncate;
                bool result = false;

                if (lowerData.TryFindLowestZeroByte(out var lzb))
                {
                    window = new(bitOffset + copyCount, lowerData);

                    if (lzb == 0)
                    {
                        copied = copyCount;
                        return true;
                    }

                    if (lzb <= dest.Length + 1)
                    {
                        truncate = lzb << 3;
                        result = true;
                    }
                    else
                    {
                        truncate = dest.Length << 3;
                    }

                    copied = copyCount + (uint)lowerData.TruncateUnchecked(truncate).WriteToKnownAligned(dest);
                    return result;
                }
                else
                {
                    UnsignedLsBitData upperData;

                    if ((++helper).IsFinalReference)
                    {
                        upperData = helper.ToDataTruncated(endAlignment);
                    }
                    else
                    {
                        upperData = helper.ToData();
                    }

                    lowerData = lowerData.ConcatenateRotateUnsignedChecked(ref upperData);
                    if (lowerData.TryFindLowestZeroByte(out lzb))
                    {
                        if (lzb == 0)
                        {
                            window = new(bitOffset + copyCount, lowerData);

                            copied = copyCount;
                            return true;
                        }

                        if (lzb <= dest.Length + 1)
                        {
                            truncate = lzb << 3;
                            result = true;
                        }
                        else
                        {
                            truncate = dest.Length << 3;
                        }
                    }
                    else
                    {
                        truncate = lowerData.Length & ~7;
                    }

                    copyCount += (uint)lowerData.TruncateUnchecked(truncate).WriteToKnownAligned(dest);
                    window = new(bitOffset + copyCount, lowerData.SliceUnchecked(truncate).InternalConcatenateUnsigned(in upperData));

                    copied = copyCount;
                    return result;
                }

            }

        }

    NOSRC:
        copied = 0;
        return false;

    NODST:
        if (!window.TryExtractUnsigned(bitOffset, 8, out byte value))
        {
            value = ConcatenateUnsignedNearWindow<byte>(ref window, bitOffset, 8);
        }

        copied = 0;
        return value == 0;
    }


    public bool TryCopyTo(scoped Span<byte> span)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region References

    private static int GetReferenceMisalignment(ref byte reference)
    {
        return ~(int)GenerationPrimitives.CalculateAddress(ref reference) & 7;
    }

    private readonly ref ulong GetReference(long bitOffset)
    {
        return ref Unsafe.Add(ref _reference, (uint)(bitOffset >> 6));
    }

    private UnsafeReferenceHelper GetUnsafeReferenceHelper(long bitOffset, out int startAlignment, out int endAlignment)
    {
        Debug.Assert((ulong)bitOffset < (ulong)_bitLength);

        var startOffset = bitOffset + _bitAlignment;
        startAlignment = (int)startOffset & 63;

        var endOffset = _bitLength + _bitAlignment;
        endAlignment = (int)endOffset & 63;

        return new(in GetReference(endOffset - 1), ref GetReference(startOffset));
    }

    private UnsafeReferenceHelper GetUnsafeReferenceHelper(long bitOffset, long bitLength, out int startAlignment, out int endAlignment)
    {
        Debug.Assert((ulong)bitOffset < (ulong)_bitLength);
        Debug.Assert((ulong)bitLength <= (ulong)(_bitLength - bitOffset));

        var startOffset = _bitAlignment + bitOffset;
        startAlignment = (int)startOffset & 63;

        var endOffset = startOffset + bitLength;
        endAlignment = (int)endOffset & 63;

        return new(in GetReference(endOffset - 1), ref GetReference(startOffset));
    }

    public BitPositionDebugView GenerateDebugInfo(BitPositionDebugView.InterpretationType type, long bitOffset, long bitLength, string description)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bitLength, 0, nameof(bitLength));
        if (!IsBitSpecInBounds(bitOffset, bitLength))
            throw new ArgumentOutOfRangeException();

        byte[] array;
        if (type == BitPositionDebugView.InterpretationType.LsBit)
            array = new byte[(int)(bitLength + 63 >> 6) << 3];
        else
            array = new byte[(int)(bitLength + 7 >> 3)];

        CopyBytesLSB(bitOffset, bitLength, array);

        return new(type, description, array, bitOffset, bitLength);
    }

    private readonly ref struct UnsafeReferenceHelper
    {
        private readonly ref readonly ulong _final;
        private readonly ref ulong _reference;

        public bool IsValid => Unsafe.IsAddressLessThanOrEqualTo(in _reference, in _final) && !Unsafe.IsNullRef(in _reference);
        public bool IsFinalReference => Unsafe.AreSame(in _reference, in _final);

        public UnsafeReferenceHelper(in ulong final, ref ulong reference)
        {
            _final = ref final;
            _reference = ref reference;
        }

        public UnsignedLsBitData ToData()
        {
            Debug.Assert(IsValid);

            var result = _reference;

            return new(result, 64);
        }

        public ref ulong ToReference() => ref _reference;

        public UnsignedLsBitData ToDataTruncated(int endAlignment)
        {
            Debug.Assert(IsValid);
            Debug.Assert(IsFinalReference);
            Debug.Assert((uint)endAlignment < 64);

            var result = _reference;

            if (endAlignment == 0)
                return new(result, 64);

            return new(ExtractionPrimitives.UncheckedExtractUInt64LSB(result, endAlignment), endAlignment);
        }

        public UnsignedLsBitData ToDataSliced(int startAlignment)
        {
            Debug.Assert(IsValid);
            Debug.Assert(!IsFinalReference);
            Debug.Assert(startAlignment < 64);

            var result = _reference;

            return new(result >> startAlignment, 64 - startAlignment);
        }

        public UnsignedLsBitData ToDataSliced(int startAlignment, int endAlignment)
        {
            Debug.Assert(IsValid);
            Debug.Assert(IsFinalReference);
            Debug.Assert(((uint)startAlignment | (uint)endAlignment) < 64);

            var result = _reference;

            var bitLength = endAlignment - startAlignment;
            Debug.Assert(bitLength >= 0);

            bitLength &= 63;
            if (bitLength == 0)
                return new(result, 64);

            return new(ExtractionPrimitives.UncheckedExtractUInt64LSB(result, startAlignment, bitLength, endAlignment), bitLength);
        }

        public ReadOnlySpan<byte> ToSpan(int startAlignment, int endAlignment)
        {
            Debug.Assert(IsValid);

            var start = (uint)startAlignment;
            var end = (uint)endAlignment;

            Debug.Assert((start | end) < 64);
            Debug.Assert((start & 7) == 0);

            if (end == 0)
                end = 64;

            start >>= 3;
            end >>= 3;

            var length = Unsafe.ByteOffset(in _reference, in _final) - start + end;
            Debug.Assert(length >= 0);

            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref Unsafe.As<ulong, byte>(ref _reference), start), (int)length);
        }

        public static UnsafeReferenceHelper operator ++(UnsafeReferenceHelper value)
        {
            Debug.Assert(value.IsValid);
            Debug.Assert(!value.IsFinalReference);

            return new(in value._final, ref Unsafe.Add(ref value._reference, 1U));
        }

        public static implicit operator ulong(UnsafeReferenceHelper value)
        {
            Debug.Assert(value.IsValid);

            return value._reference;
        }
    }
    
    #endregion
}
