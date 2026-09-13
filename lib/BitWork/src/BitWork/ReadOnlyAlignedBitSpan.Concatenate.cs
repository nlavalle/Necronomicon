using System.Diagnostics;
using BitWork.Primitives;

namespace BitWork;

public readonly ref partial struct ReadOnlyAlignedBitSpan
{
    // Take existing bits in window, concatenate with bits read from span,
    // return the concatenated value, position the window offset past that value.
    internal T ConcatenateUnsignedFarWindow<T>(ref ReadOnlyLsBitWindow window, long bitOffset, int bitCount) where T : unmanaged
    {
        Debug.Assert((uint)bitCount <= 64U, "Cannot concatenate more than 64 bits.");

        Debug.Assert((ulong)bitOffset < (ulong)_bitLength);
        Debug.Assert((ulong)bitCount <= (ulong)(_bitLength - bitOffset));

        var lowerData = window.ToData(bitOffset);
        var bitsNeeded = bitCount - lowerData.Length;

        Debug.Assert(bitsNeeded <= 0, "Incorrect call, data is complete in current window, concatenation is not needed.");

        UnsignedLsBitData upperData;

        scoped var helper = GetUnsafeReferenceHelper(bitOffset + (uint)lowerData.Length, out var startAlignment, out var endAlignment);

        if (helper.IsFinalReference)
        {
            upperData = helper.ToDataSliced(startAlignment, endAlignment);
            Debug.Assert(upperData.Length >= bitsNeeded);
        }
        else
        {
            upperData = helper.ToDataSliced(startAlignment);
        }

        var concat = upperData.Data;
        var adjustment = upperData.Length - bitsNeeded;
        if (adjustment > 0)
        {
            lowerData = lowerData.InternalConcatenateUnsignedLs(ExtractionPrimitives.UncheckedExtractUInt64LSB(concat, bitsNeeded), bitCount);
            upperData = new(concat >> bitsNeeded, adjustment);
        }
        else if (adjustment == 0)
        {
            lowerData = lowerData.InternalConcatenateUnsignedLs(concat, bitCount);
            upperData = default;
        }
        else
        {
            lowerData = lowerData.InternalConcatenateUnsignedLs(concat, bitCount + adjustment);
            helper++;

            if (helper.IsFinalReference)
            {
                upperData = helper.ToDataTruncated(endAlignment);
            }
            else
            {
                upperData = helper.ToData();
            }

            adjustment = -adjustment;

            lowerData = lowerData.InternalConcatenateUnsignedLs(ExtractionPrimitives.UncheckedExtractUInt64LSB(upperData.Data, adjustment), bitCount);
            upperData = upperData.SliceUnchecked(adjustment);
        }

        window = new(bitOffset + (uint)bitCount, upperData);

        return lowerData.AsUnsigned<T>();
    }

    internal T ConcatenateSignedFarWindow<T>(ref ReadOnlyLsBitWindow window, long bitOffset, int bitCount) where T : unmanaged
    {
        Debug.Assert((uint)bitCount <= 64U, "Cannot concatenate more than 64 bits.");

        Debug.Assert((ulong)bitOffset < (ulong)_bitLength);
        Debug.Assert((ulong)bitCount <= (ulong)(_bitLength - bitOffset));

        var lowerData = window.ToData(bitOffset);
        var bitsNeeded = bitCount - lowerData.Length;

        Debug.Assert(bitsNeeded > 0, "Incorrect call, data is complete in current window, concatenation is not needed.");

        SignedBitData result;
        UnsignedLsBitData upperData;

        scoped var helper = GetUnsafeReferenceHelper(bitOffset + (uint)lowerData.Length, out var startAlignment, out var endAlignment);

        if (helper.IsFinalReference)
        {
            upperData = helper.ToDataSliced(startAlignment, endAlignment);
            Debug.Assert(upperData.Length >= bitsNeeded);
        }
        else
        {
            upperData = helper.ToDataSliced(startAlignment);
        }

        var concat = upperData.Data;
        var adjustment = upperData.Length - bitsNeeded;
        if (adjustment >= 0)
        {
            result = lowerData.InternalConcatenateSigned(ExtractionPrimitives.UncheckedExtractInt64LSB(concat, bitsNeeded), bitCount);
            upperData = new(concat >> bitsNeeded, adjustment);
        }
        else
        {
            lowerData = lowerData.InternalConcatenateUnsignedLs(concat, bitCount + adjustment);
            helper++;

            if (helper.IsFinalReference)
            {
                upperData = helper.ToDataTruncated(endAlignment);
            }
            else
            {
                upperData = helper.ToData();
            }

            adjustment = -adjustment;

            result = lowerData.InternalConcatenateSigned(ExtractionPrimitives.UncheckedExtractInt64LSB(upperData.Data, adjustment), bitCount);
            upperData = upperData.SliceUnchecked(adjustment);
        }

        window = new(bitOffset + (uint)bitCount, upperData);

        return result.AsSigned<T>();
    }

    // Take existing bits in window, concatenate with bits read from span,
    // return the concatenated value, position the window starting at that value.
    internal T ConcatenateUnsignedNearWindow<T>(ref ReadOnlyLsBitWindow window, long bitOffset, int bitCount) where T : unmanaged
    {
        var lowerData = ConcatenateNearWindow(in window, bitOffset, bitCount);

        window = new ReadOnlyLsBitWindow(bitOffset, lowerData);
        return lowerData.InternalExtractUnsigned<T>(bitCount);
    }

    internal T ConcatenateSignedNearWindow<T>(ref ReadOnlyLsBitWindow window, long bitOffset, int bitCount) where T : unmanaged
    {
        var lowerData = ConcatenateNearWindow(in window, bitOffset, bitCount);

        window = new ReadOnlyLsBitWindow(bitOffset, lowerData);
        return lowerData.InternalExtractSigned<T>(bitCount);
    }

    private UnsignedLsBitData ConcatenateNearWindow(in ReadOnlyLsBitWindow window, long bitOffset, int bitCount)
    {
        Debug.Assert((uint)bitCount <= 64U, "Cannot concatenate more than 64 bits.");

        Debug.Assert((ulong)bitOffset < (ulong)_bitLength);
        Debug.Assert((ulong)bitCount <= (ulong)(_bitLength - bitOffset));

        var lowerData = window.ToData(bitOffset);

        Debug.Assert(bitCount > lowerData.Length, "Incorrect call, data is complete in current window, concatenation is not needed.");

        UnsignedLsBitData upperData;

        scoped var helper = GetUnsafeReferenceHelper(bitOffset + (uint)lowerData.Length, out var startAlignment, out var endAlignment);

        if (helper.IsFinalReference)
        {
            upperData = helper.ToDataSliced(startAlignment, endAlignment);
        }
        else
        {
            upperData = helper.ToDataSliced(startAlignment);
        }

        lowerData = lowerData.InternalConcatenateUnsigned(in upperData);

        if (lowerData.Length < bitCount)
        {
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

        return lowerData;
    }


}
