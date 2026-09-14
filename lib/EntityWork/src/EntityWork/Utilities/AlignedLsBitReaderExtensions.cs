using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitWork;

namespace EntityWork.Utilities;

internal static class AlignedLsBitReaderExtensions
{
    public static void Advance(this ref AlignedLsBitReader reader, int bitCount)
    {
        if (!reader.TryAdvance((uint)bitCount))
            throw new Exception();
    }

    public static bool ReadBool(this ref AlignedLsBitReader reader)
    {
        if (!reader.TryReadBool(out var value))
            throw new Exception();

        return value;
    }

    public static int ReadCoordInto(this ref AlignedLsBitReader reader, Span<byte> span)
    {
        var flags = reader.ReadUnsigned<uint>(2);
        if (flags == 0)
        {
            MemoryMarshal.Write(span, 0U);

            return 2;
        }

        const float mult = 1f / (1 << 5);
        int value, bits;

        // This produces 1 for a bit of 0, and -1 for a bit of 1
        var signMult = reader.ReadSigned<int>(1) | 1;

        switch (flags)
        {
            case 1:
                value = (reader.ReadUnsigned<int>(14) + 1) << 5;
                bits = 2 + 1 + 14;
                break;
            case 2:
                value = reader.ReadUnsigned<int>(5);
                bits = 2 + 1 + 5;
                break;
            case 3:
                value = reader.ReadUnsigned<int>(19) + 32;
                bits = 2 + 1 + 19;
                break;
            default:
                throw new UnreachableException();
        }

        MemoryMarshal.Write(span, value * signMult * mult);

        return bits;
    }

    public static int ReadCoordInto(this ref AlignedLsBitReader reader, ref float value)
    {
        var flags = reader.ReadUnsigned<uint>(2);
        if (flags == 0)
        {
            Unsafe.As<float, uint>(ref value) = 0U;

            return 2;
        }

        const float mult = 1f / (1 << 5);
        int read, bits;

        // This produces 1 for a bit of 0, and -1 for a bit of 1
        var signMult = reader.ReadSigned<int>(1) | 1;

        switch (flags)
        {
            case 1:
                read = (reader.ReadUnsigned<int>(14) + 1) << 5;
                bits = 2 + 1 + 14;
                break;
            case 2:
                read = reader.ReadUnsigned<int>(5);
                bits = 2 + 1 + 5;
                break;
            case 3:
                read = reader.ReadUnsigned<int>(19) + 32;
                bits = 2 + 1 + 19;
                break;
            default:
                throw new UnreachableException();
        }

        value = read * signMult * mult;

        return bits;
    }

    public static int SkipCoord(this ref AlignedLsBitReader reader)
    {
        var flags = reader.ReadUnsigned<uint>(2);
        if (flags == 0)
        {
            return 2;
        }

        int skip = flags switch
        {
            1 => 1 + 14,
            2 => 1 + 5,
            3 => 1 + 19,
            _ => throw new UnreachableException(),
        };

        reader.Advance(skip);

        return 2 + skip;
    }

    public static int SkipPrepend(this ref AlignedLsBitReader reader, int bitCount)
    {
        int readBits = 1;

        if (!reader.ReadBool())
        {
            reader.Advance(bitCount);
            readBits += bitCount;
        }

        return readBits;
    }

    public static int SkipPrependGrouped(this ref AlignedLsBitReader reader, int bitCount, int iterations)
    {
        int readBits = iterations;

        int flags = reader.ReadUnsigned<int>(iterations);

        if (flags != 0)
        {
            var skip = int.PopCount(flags) * bitCount;
            reader.Advance(skip);
            readBits += skip;
        }

        return readBits;
    }

    public static uint ReadUBitVarFP(this ref AlignedLsBitReader reader)
    {
        const int ReadFlatBitsPath1 = 1 + 2;
        const int ReadFlatBitsPath2 = 2 + 4;
        const int ReadFlatBitsPath3 = 3 + 10;
        const int ReadFlatBitsPath4 = 4 + 17;
        const int ReadFlatBitsPath5 = 4 + 31;
        const int ReadTreeBitsPath1 = ReadFlatBitsPath1;
        const int ReadTreeBitsPath2 = ReadFlatBitsPath4 - ReadTreeBitsPath1;

        var current = reader.ReadUnsigned<uint>(ReadTreeBitsPath1);
        if (current != 0)
        {
            if ((current & 1) != 0)
            {
                current >>= 1;
            }
            else if ((current & 2) != 0)
            {
                var upper = reader.ReadUnsigned<uint>(ReadFlatBitsPath2 - ReadTreeBitsPath1);
                current = (current >> 2) | (upper << 2);
            }
            else
            {
                current = reader.ReadUnsigned<uint>(ReadFlatBitsPath3 - ReadTreeBitsPath1);
            }
        }
        else
        {
            current = reader.ReadUnsigned<uint>(ReadTreeBitsPath2);
            if ((current & 1) != 0)
            {
                current >>= 1;
            }
            else
            {
                var upper = reader.ReadUnsigned<uint>(ReadFlatBitsPath5 - ReadFlatBitsPath4);
                current = (current >> 1) | (upper << 17);
            }
        }

        return current;
    }

    public static uint ReadUBitVar(this ref AlignedLsBitReader reader)
    {
        var current = reader.ReadUnsigned<uint>(6);

        var flags = current >> 4;
        if (flags != 0)
        {
            current &= 15;

            switch (flags)
            {
                case 1:
                    current |= reader.ReadUnsigned<uint>(4) << 4;
                    break;
                case 2:
                    current |= reader.ReadUnsigned<uint>(8) << 4;
                    break;
                case 3:
                    current |= reader.ReadUnsigned<uint>(28) << 4;
                    break;
            }
        }

        return current;
    }

    public static uint ReadEmbeddedInt(this ref AlignedLsBitReader reader)
    {
        const int ReadBits = 6;
        const int DataBits = 4;
        const int Magic = 16;
        
        const int DataMask = (1 << DataBits) - 1;
        const int OpBits = ReadBits - DataBits;
        const int OpMask = (1 << OpBits) - 1;
        const int Addend = Magic - OpMask;

        // This is a unique header from Valve, the first two bits indicate how many bits to take after
        var low6 = reader.ReadUnsigned<uint>(ReadBits);
        var op = (int)(low6 >> DataBits);

        if (op == 0)
        {
            return low6;
        }
        else
        {
            // 01b = 4, 10b = 8, 11b = 28
            var upperCount = op * 4 + ((op + Addend) & Magic);

            return (low6 & DataMask) | (reader.ReadUnsigned<uint>(upperCount) << DataBits);
        }
    }


    public static int Read3BitNormalInto(this ref AlignedLsBitReader reader, Span<byte> span)
    {
        const int one = 1 << 22;
        const float multXY = 1f / (1 << 11);
        const float multZ = 1f / (1 << 22);

        int bits = 3;
        var flags = reader.ReadUnsigned<int>(2);

        if (flags == 0)
        {
            span.Clear();
            reader.Advance(1);

            return bits;
        }

        int rawX, rawY;

        if ((flags & 1) != 0)
        {
            var flagMult = reader.ReadSigned<int>(1) | 1;
            rawX = reader.ReadUnsigned<int>(11);

            MemoryMarshal.Write(span, flagMult * rawX * multXY);

            rawX *= rawX;

            bits += 12;
        }
        else
        {
            rawX = 0;

            MemoryMarshal.Write(span, 0U);
        }

        if ((flags & 2) != 0)
        {
            var flagMult = reader.ReadSigned<int>(1) | 1;
            rawY = reader.ReadUnsigned<int>(11);

            MemoryMarshal.Write(span.Slice(4), flagMult * rawY * multXY);

            rawY *= rawY;

            bits += 12;
        }
        else
        {
            rawY = 0;

            MemoryMarshal.Write(span.Slice(4), 0U);
        }

        var invDotProd = one - (rawX + rawY);
        if (invDotProd > 0)
        {
            var flagMult = reader.ReadSigned<int>(1) | 1;

            MemoryMarshal.Write(span.Slice(8), float.Sqrt(flagMult * invDotProd * multZ));
        }
        else
        {
            reader.Advance(1);

            MemoryMarshal.Write(span.Slice(8), 0U);
        }

        return bits;
    }

    public static int Skip3BitNormal(this ref AlignedLsBitReader reader)
    {
        var flags = reader.ReadUnsigned<int>(2);
        var count = int.PopCount(flags);
        var skip = 1 + count * 12;

        reader.Advance(skip);

        return 2 + skip;
    }

    public static T ReadSigned<T>(this ref AlignedLsBitReader reader, int bitCount)
        where T : unmanaged
    {
        if (!reader.TryReadSigned(bitCount, out T value))
            throw new Exception();

        return value;
    }

    public static T ReadUnsigned<T>(this ref AlignedLsBitReader reader, int bitCount)
        where T : unmanaged
    {
        if (!reader.TryReadUnsigned(bitCount, out T value))
            throw new Exception();

        return value;
    }

    public static uint ReadVarUInt32(this ref AlignedLsBitReader reader, out int bitCount)
    {
        if (!reader.TryReadVarUInt32(out var value, out bitCount))
            throw new Exception();

        return value;
    }

    public static bool TryReadVarUInt32(this ref AlignedLsBitReader reader, out uint value, out int bitCount)
    {
        uint result = 0;
        int i = 0;
        var offset = reader.Offset;

        while (reader.TryReadUnsigned(8, out uint working))
        {
            if (i > 4)
                break;

            result |= (working & 0x7F) << (i * 7);

            if ((working & 0x80) == 0U)
            {
                value = result;
                bitCount = i * 8;
                return true;
            }

            i++;
        }

        reader.TryRevert(offset);
        value = 0;
        bitCount = 0;
        return false;
    }

    public static ulong ReadVarUInt64(this ref AlignedLsBitReader reader, out int bitCount)
    {
        if (!reader.TryReadVarUInt64(out var value, out bitCount))
            throw new Exception();

        return value;
    }

    public static bool TryReadVarUInt64(this ref AlignedLsBitReader reader, out ulong value, out int bitCount)
    {
        ulong result = 0;
        int i = 0;
        var offset = reader.Offset;

        while (reader.TryReadUnsigned(8, out ulong working))
        {
            if (i > 9)
                break;

            result |= (working & 0x7F) << (i * 7);

            if ((working & 0x80) == 0U)
            {
                value = result;
                bitCount = i * 8;
                return true;
            }

            i++;
        }

        reader.TryRevert(offset);
        value = 0;
        bitCount = 0;
        return false;
    }

}
