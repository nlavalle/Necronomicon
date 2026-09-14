using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitWork;
using BitWork.Primitives;
using EntityWork.Utilities;
using Steam.Protos.Dota2;

namespace EntityWork.Model.Types;

internal sealed class FloatFieldDecoderFactory
{
    public readonly record struct CacheKey(uint Low, uint High, int Flags, int BitCount);

    /*
    Going to implement faster (and less nonsensical) quantization algorithms

    There are two core algorithms:
        - Offset Algorithm
            1. Read integer
            2. Adjust the integer based on rounding
            3. Multiply by (upper bound - lower bound) / denominator
            4. Add lower bound
        - Centering Algorithm
            1. Read integer
            2. Adjust the integer to center the value on zero (half negative, half positive)
            3. Adjust the integer based on rounding
            4. Multiply by upper bound / denominator
            5. Add center: (upper bound + lower bound) / 2
    
    Centering is used where the range crosses zero (lower bound negative, upper bound positive)
    Offset is used everywhere else

    Old Quantization (4 bits):

            NONE        DOWN                    UP                          INT
    0000    1/15*0      1/15*(0/16)=0           1/15*(0/16)+(1/16)=1/16           
    0001    1/15*1      1/15*(15/16)~1/16       1/15*(15/16)+(1/16)~2/16       
    0010    1/15*2      1/15*(30/16)~2/16       1/15*(30/16)+(1/16)~3/16       
    0011    1/15*3      1/15*(45/16)~3/16       1/15*(45/16)+(1/16)~4/16       
    0100    1/15*4      1/15*(60/16)~4/16       1/15*(60/16)+(1/16)~5/16       
    0101    1/15*5      1/15*(75/16)~5/16       1/15*(75/16)+(1/16)~6/16       
    0110    1/15*6      1/15*(90/16)~6/16       1/15*(90/16)+(1/16)~7/16       
    0111    1/15*7      1/15*(105/16)~7/16      1/15*(105/16)+(1/16)~8/16      
    1000    1/15*8      1/15*(120/16)~8/16      1/15*(120/16)+(1/16)~9/16      
    1001    1/15*9      1/15*(135/16)~9/16      1/15*(135/16)+(1/16)~10/16      
    1010    1/15*10     1/15*(150/16)~10/16     1/15*(150/16)+(1/16)~11/16     
    1011    1/15*11     1/15*(165/16)~11/16     1/15*(165/16)+(1/16)~12/16     
    1100    1/15*12     1/15*(180/16)~12/16     1/15*(180/16)+(1/16)~13/16     
    1101    1/15*13     1/15*(195/16)~13/16     1/15*(195/16)+(1/16)~14/16     
    1110    1/15*14     1/15*(210/16)~14/16     1/15*(210/16)+(1/16)~15/16     
    1111    1/15*15     1/15*(225/16)~15/16     1/15*(225/16)+(1/16)~16/16     
    PRE     -           0                       1                           -


    New Quantization (4 bits):

            NONE    DOWN    UP      ZCZE    ZCNZE   INT
    0000    1/32    1/16    0/16    -8/8    -15/16  1/16
    0001    3/32    2/16    1/16    -7/8    -13/16  2/16
    0010    5/32    3/16    2/16    -6/8    -11/16  3/16
    0011    7/32    4/16    3/16    -5/8    -9/16   4/16
    0100    9/32    5/16    4/16    -4/8    -7/16   5/16
    0101    11/32   6/16    5/16    -3/8    -5/16   6/16
    0110    13/32   7/16    6/16    -2/8    -3/16   7/16
    0111    15/32   8/16    7/16    -1/8    -1/16   8/16
    1000    17/32   9/16    8/16    1/8     1/16    9/16
    1001    19/32   10/16   9/16    2/8     3/16    10/16
    1010    21/32   11/16   10/16   3/8     5/16    11/16
    1011    23/32   12/16   11/16   4/8     7/16    12/16
    1100    25/32   13/16   12/16   5/8     9/16    13/16
    1101    27/32   14/16   13/16   6/8     11/16   14/16
    1110    29/32   15/16   14/16   7/8     13/16   15/16
    1111    31/32   16/16   15/16   8/8     15/16   16/16
    PRE     -       0       1       0       -       -

    add     0       1       0       -8      -8      1
    midp    t       f       f       f       t       f
    mult    1/32    1/16    1/16    1/8     1/16    1/16
    decen   f       f       f       t       f       f
    off     0       0       0       0       0       0

    Offset From Zero:

            NONE    DOWN    UP   
    0000    33/32   17/16   16/16
    0001    35/32   18/16   17/16
    0010    37/32   19/16   18/16
    0011    39/32   20/16   19/16
    0100    41/32   21/16   20/16
    0101    43/32   22/16   21/16
    0110    45/32   23/16   22/16
    0111    47/32   24/16   23/16
    1000    49/32   25/16   24/16
    1001    51/32   26/16   25/16
    1010    53/32   27/16   26/16
    1011    55/32   28/16   27/16
    1100    57/32   29/16   28/16
    1101    59/32   30/16   29/16
    1110    61/32   31/16   30/16
    1111    63/32   32/16   31/16
    PRE     -       1       2    

    add     0       1       0     
    midp    t       f       f     
    mult    1/32    1/16    1/16  
    decen   f       f       f     
    off     1       1       1     

    */

    public readonly static EntityFieldDecoder Fixed = InvokeFixed;
    public readonly static EntityFieldDecoder Coord = InvokeCoord;
    public readonly static FloatFieldDecoderFactory Singleton = new();

    private readonly ReaderWriterLockSlim _cacheLock = new();
    private readonly Dictionary<CacheKey, EntityFieldDecoder> _cache = new();

    private static int InvokeFixed(ref AlignedLsBitReader reader, in EntityWriter writer)
    {
        const int readBits = sizeof(uint) * 8;

        if (writer.IsSkipped)
        {
            reader.Advance(readBits);

            goto Exit;
        }

        var read = reader.ReadUnsigned<uint>(readBits);
        MemoryMarshal.Write(writer.Dereference(), in read);

    Exit:
        return readBits;
    }

    internal static EntityFieldDecoder CreateFixed(int iterations)
    {
        if (iterations == 1)
            return Fixed;

        // TODO Cache these in static or singleton
        return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
        {
            int readBytes = sizeof(float) * iterations;
            int readBits = readBytes * 8;

            if (writer.IsSkipped)
            {
                reader.Advance(readBits);

                goto Exit;
            }

            var span = writer.Dereference();
            Debug.Assert(span.Length == readBytes);

            if (BitConverter.IsLittleEndian)
            {
                reader.TryCopyTo(span);
            }
            else
            {
                ref var value = ref GetPtr<uint>(span, iterations);

                while (true)
                {
                    value = reader.ReadUnsigned<uint>(sizeof(uint) * 8);

                    if (--iterations == 0)
                        break;

                    value = ref Unsafe.Add(ref value, 1U);
                }
            }

        Exit:
            return readBits;
        };
    }

    private static int InvokeCoord(ref AlignedLsBitReader reader, in EntityWriter writer)
    {
        int readBits;

        if (writer.IsSkipped)
        {
            readBits = reader.SkipCoord();

            goto Exit;
        }

        readBits = reader.ReadCoordInto(ref GetFloatPtr(writer.Dereference()));

    Exit:
        return readBits;
    }

    internal static EntityFieldDecoder CreateCoord(int iterations)
    {
        if (iterations == 1)
            return Coord;

        // TODO Cache these in static or singleton
        return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
        {
            int readBits = 0;

            if (writer.IsSkipped)
            {
                do
                {
                    readBits += reader.SkipCoord();
                } while (--iterations == 0);

                goto Exit;
            }

            ref var value = ref GetFloatPtr(writer.Dereference(), iterations);

            while (true)
            {
                readBits += reader.ReadCoordInto(ref value);

                if (--iterations == 0)
                    break;

                value = ref Unsafe.Add(ref value, 1U);
            }

        Exit:
            return readBits;
        };
    }

    // This is the base quantization operation, all others are optimizations of this
    internal static EntityFieldDecoder CreateQuantized(int bits, int addend, float multiplicand, float offset, float prepend, bool doPrepend, bool doMidpoint, bool doDecenter, int iterations)
    {
        if (iterations == 1)
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits = 0;

                if (writer.IsSkipped)
                {
                    if (doPrepend)
                    {
                        readBits += 1;

                        if (reader.ReadBool())
                        {
                            goto Exit;
                        }
                    }

                    reader.Advance(bits);
                    readBits += bits;

                    goto Exit;
                }

                float calculated;

                // Prepending is based upon flags
                if (doPrepend)
                {
                    readBits += 1;

                    if (reader.ReadBool())
                    {
                        calculated = prepend;

                        goto Write;
                    }
                }

                readBits += bits;

                // Read the bits into an integer
                var read = reader.ReadUnsigned<int>(bits);

                // Addend is precalculated from flags, usually: 0, 1, -1 << bits, or -1 << (bits - 1)
                read += addend;

                if (doMidpoint)
                {
                    read = (read << 1) | 1;
                }
                else
                {
                    if (doDecenter)
                    {
                        // MSB is flag, 0 is negative, 1 is positive; adding this flag
                        // will put the same number of integers above and below center
                        var msbShift = bits - 1;
                        var decenter = read >> msbShift;

                        read += decenter;
                    }
                }

                // Multiplicand is precalulated, usually: (HighValue - LowValue) / (1U << bits) or HighValue / (1U << (bits - 1))
                // Offset is precalculated, usually: 0, LowValue, or (HighValue + LowValue) / 2
                calculated = read * multiplicand + offset;

                // Optimizations can occur when:
                // - center is 0
                // - addend is 0, 1, -1<<bits, (-1<<bits) - 1 (top or bottom is 0)
                // - addend can be calculated faster than ~4 cycles
                // - decenter is not used

            Write:
                GetFloatPtr(writer.Dereference()) = calculated;

            Exit:
                return readBits;
            };
        }
        else
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits = 0;

                if (writer.IsSkipped)
                {
                    if (doPrepend)
                    {
                        do
                        {
                            readBits += reader.SkipPrepend(bits);
                        } while (--iterations != 0);
                    }
                    else
                    {
                        readBits = bits * iterations;
                        reader.Advance(readBits);
                    }

                    goto Exit;
                }

                ref var value = ref GetFloatPtr(writer.Dereference());

                while (true)
                {
                    float calculated;

                    // Prepending is based upon flags
                    if (doPrepend)
                    {
                        readBits += 1;

                        if (reader.ReadBool())
                        {
                            calculated = prepend;

                            goto Write;
                        }
                    }

                    readBits += bits;

                    // Read the bits into an integer
                    var read = reader.ReadUnsigned<int>(bits);

                    // Addend is precalculated from flags, usually: 0, 1, -1 << bits, or -1 << (bits - 1)
                    read += addend;

                    if (doMidpoint)
                    {
                        read = (read << 1) | 1;
                    }
                    else
                    {
                        if (doDecenter)
                        {
                            // MSB is flag, 0 is negative, 1 is positive; adding this flag
                            // will put the same number of integers above and below center
                            var msbShift = bits - 1;
                            var decenter = read >> msbShift;

                            read += decenter;
                        }
                    }

                    // Multiplicand is precalulated, usually: (HighValue - LowValue) / (1U << bits) or HighValue / (1U << (bits - 1))
                    // Offset is precalculated, usually: 0, LowValue, or (HighValue + LowValue) / 2
                    calculated = read * multiplicand + offset;

                    // Optimizations can occur when:
                    // - center is 0
                    // - addend is 0, 1, -1<<bits, (-1<<bits) - 1 (top or bottom is 0)
                    // - addend can be calculated faster than ~4 cycles
                    // - decenter is not used

                Write:
                    value = calculated;

                    if (--iterations == 0)
                        break;

                    value = ref Unsafe.Add(ref value, 1U);
                }

            Exit:
                return readBits;
            };
        }
    }

    private static EntityFieldDecoder CreateQuantizedZeroCentered(int bits, float multiplicand)
    {
        return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
        {
            float value;
            int readBits = 1;

            if (writer.IsSkipped)
            {
                if (!reader.ReadBool())
                {
                    reader.Advance(bits);
                    readBits += bits;
                }

                goto Exit;
            }

            if (reader.ReadBool())
            {
                value = 0f;
            }
            else
            {
                readBits += bits;

                var read = reader.ReadUnsigned<int>(bits);

                var msbShift = bits - 1;
                var half = -1 << msbShift;

                // MSB is flag, 0 is negative, 1 is positive
                var flag = read >> msbShift;

                // Change read to be the correct orientation
                read += half;

                // Need to add flag for zero-center
                read += flag;

                value = read * multiplicand;
            }

            MemoryMarshal.Write(writer.Dereference(), in value);

        Exit:
            return readBits;
        };
    }

    private static EntityFieldDecoder CreateQuantizedCentered(int bits, float center, float multiplicand)
    {
        return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
        {
            float value;
            int readBits = 1;

            if (writer.IsSkipped)
            {
                if (!reader.ReadBool())
                {
                    reader.Advance(bits);
                    readBits += bits;
                }

                goto Exit;
            }

            if (reader.ReadBool())
            {
                value = 0f;
            }
            else
            {
                var read = reader.ReadUnsigned<int>(bits);

                var msbShift = bits - 1;
                var half = -1 << msbShift;

                read += half;

                value = read * multiplicand + center;
            }

            MemoryMarshal.Write(writer.Dereference(), in value);

        Exit:
            return readBits;
        };
    }

    private static EntityFieldDecoder CreateQuantizedZeroBottom(int bitCount, float lowValue, int iterations)
    {
        // (read(bits) + 1) * (high / (1 << bits)) [(read(4) + 1) * (high / 16)]
        float multiplicand = lowValue / (1 << bitCount);
        
        if (iterations == 1)
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                if (writer.IsSkipped)
                {
                    reader.Advance(bitCount);

                    goto Exit;
                }

                ref var value = ref GetFloatPtr(writer.Dereference());

                CopyAsAddToAddend(ref reader, ref value, bitCount, multiplicand, 1);

            Exit:
                return bitCount;
            };
        }
        else
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits = bitCount * iterations;

                if (writer.IsSkipped)
                {
                    reader.Advance(readBits);

                    goto Exit;
                }

                ref var value = ref GetFloatPtr(writer.Dereference(), iterations);

                while (true)
                {
                    CopyAsAddToAddend(ref reader, ref value, bitCount, multiplicand, 1);

                    if (--iterations == 0)
                        break;

                    value = ref Unsafe.Add(ref value, 1U);
                }

            Exit:
                return readBits;
            };
        }
    }

    private static EntityFieldDecoder CreateQuantizedZeroBottomRoundDown(int bitCount, float highValue, int iterations)
    {
        // (read(bits) + 1) * (high / (1 << bits)) [(read(4) + 1) * (high / 16)]
        float multiplicand = highValue / (1 << bitCount);
        
        if (iterations == 1)
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits;

                if (writer.IsSkipped)
                {
                    readBits = reader.SkipPrepend(bitCount);

                    goto Exit;
                }

                ref var value = ref GetFloatPtr(writer.Dereference());

                readBits = CopyAsAddToAddend(ref reader, ref value, bitCount, multiplicand, 1, highValue);

            Exit:
                return readBits;
            };
        }
        else
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits = 0;

                if (writer.IsSkipped)
                {
                    do
                    {
                        readBits += reader.SkipPrepend(bitCount);
                    } while (--iterations != 0);

                    goto Exit;
                }

                ref var value = ref GetFloatPtr(writer.Dereference(), iterations);

                while (true)
                {
                    readBits += CopyAsAddToAddend(ref reader, ref value, bitCount, multiplicand, 1, highValue);

                    if (--iterations == 0)
                        break;

                    value = ref Unsafe.Add(ref value, 1U);
                }

            Exit:
                return readBits;
            };
        }
    }

    private static EntityFieldDecoder CreateQuantizedZeroBottomRoundUp(int bitCount, float highValue, int iterations)
    {
        // read(bits) * (high / (1 << bits)) [read(4) * (high / 16)]
        float multiplicand = highValue / (1 << bitCount);
        
        if (iterations == 1)
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits;

                if (writer.IsSkipped)
                {
                    readBits = reader.SkipPrepend(bitCount);

                    goto Exit;
                }

                ref var value = ref GetFloatPtr(writer.Dereference());

                readBits = CopyAsStandard(ref reader, ref value, bitCount, multiplicand, highValue);

            Exit:
                return readBits;
            };
        }
        else
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits = 0;

                if (writer.IsSkipped)
                {
                    do
                    {
                        readBits += reader.SkipPrepend(bitCount);
                    } while (--iterations != 0);

                    goto Exit;
                }

                ref var value = ref GetFloatPtr(writer.Dereference(), iterations);

                while (true)
                {
                    readBits += CopyAsStandard(ref reader, ref value, bitCount, multiplicand, highValue);

                    if (--iterations == 0)
                        break;

                    value = ref Unsafe.Add(ref value, 1U);
                }

            Exit:
                return readBits;
            };
        }
    }

    private static EntityFieldDecoder CreateQuantizedZeroBottomNoFlags(int bitCount, float highValue, int iterations)
    {
        // (read(bits) << 1 | 1) * (high / (1 << bits + 1)) [(read(4) << 1 | 1) * (high / 32)]
        float multiplicand = highValue / (1 << bitCount + 1);
        
        if (iterations == 1)
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                if (writer.IsSkipped)
                {
                    reader.Advance(bitCount);

                    goto Exit;
                }

                ref var value = ref GetFloatPtr(writer.Dereference());

                CopyAsMidpoint(ref reader, ref value, bitCount, multiplicand);

            Exit:
                return bitCount;
            };
        }
        else
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits = bitCount * iterations;

                if (writer.IsSkipped)
                {
                    reader.Advance(readBits);

                    goto Exit;
                }

                ref var value = ref GetFloatPtr(writer.Dereference(), iterations);

                while (true)
                {
                    CopyAsMidpoint(ref reader, ref value, bitCount, multiplicand);

                    if (--iterations == 0)
                        break;

                    value = ref Unsafe.Add(ref value, 1U);
                }

            Exit:
                return readBits;
            };
        }
    }

    private static EntityFieldDecoder CreateQuantizedZeroTop(int bitCount, float lowValue, int iterations)
    {
        // ((1 << bits) - 1 - read(bits)) * (low / (1 << bits)) [(15 - read(4)) * (low / 16)]
        float multiplicand = lowValue / (1 << bitCount);
        
        if (iterations == 1)
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                if (writer.IsSkipped)
                {
                    reader.Advance(bitCount);

                    goto Exit;
                }

                var minuend = GenerationPrimitives.GetPowerOfTwoMaskUInt32(bitCount);

                ref var value = ref GetFloatPtr(writer.Dereference());

                CopyAsSubFromMinuend(ref reader, ref value, bitCount, multiplicand, minuend);

            Exit:
                return bitCount;
            };
        }
        else
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits = bitCount * iterations;

                if (writer.IsSkipped)
                {
                    reader.Advance(readBits);

                    goto Exit;
                }

                var minuend = GenerationPrimitives.GetPowerOfTwoMaskUInt32(bitCount);

                ref var value = ref GetFloatPtr(writer.Dereference(), iterations);

                while (true)
                {
                    CopyAsSubFromMinuend(ref reader, ref value, bitCount, multiplicand, minuend);

                    if (--iterations == 0)
                        break;

                    value = ref Unsafe.Add(ref value, 1U);
                }

            Exit:
                return readBits;
            };
        }
    }

    private static EntityFieldDecoder CreateQuantizedZeroTopNoFlags(int bitCount, float lowValue, int iterations)
    {
        // ((1 << bits + 1) - 1 - (read(bits) << 1)) * (low / (1 << bits + 1)) [(31 - (read(4) << 1)) * (low / 32)]
        float multiplicand = lowValue / (1 << bitCount + 1);
        
        if (iterations == 1)
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                if (writer.IsSkipped)
                {
                    reader.Advance(bitCount);

                    goto Exit;
                }

                var mask = GenerationPrimitives.GetPowerOfTwoMaskUInt32(bitCount + 1);

                ref var value = ref GetFloatPtr(writer.Dereference());

                CopyAsMidpointXorMask(ref reader, ref value, bitCount, multiplicand, mask);

            Exit:
                return bitCount;
            };
        }
        else
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits = bitCount * iterations;

                if (writer.IsSkipped)
                {
                    reader.Advance(readBits);

                    goto Exit;
                }

                var mask = GenerationPrimitives.GetPowerOfTwoMaskUInt32(bitCount + 1);

                ref var value = ref GetFloatPtr(writer.Dereference(), iterations);

                while (true)
                {
                    CopyAsMidpointXorMask(ref reader, ref value, bitCount, multiplicand, mask);

                    if (--iterations == 0)
                        break;

                    value = ref Unsafe.Add(ref value, 1U);
                }

            Exit:
                return readBits;
            };
        }
    }

    private static EntityFieldDecoder CreateQuantizedZeroTopRoundDown(int bitCount, float lowValue, int iterations)
    {
        // ((1 << bits) - 1 - read(bits)) * (low / (1 << bits)) [(15 - read(4)) * (low / 16)]
        float multiplicand = lowValue / (1 << bitCount);
        
        if (iterations == 1)
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits;

                if (writer.IsSkipped)
                {
                    readBits = reader.SkipPrepend(bitCount);

                    goto Exit;
                }

                var minuend = GenerationPrimitives.GetPowerOfTwoMaskUInt32(bitCount);

                ref var value = ref GetFloatPtr(writer.Dereference());

                readBits = CopyAsSubFromMinuend(ref reader, ref value, bitCount, multiplicand, minuend, lowValue);

            Exit:
                return readBits;
            };
        }
        else
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits = 0;

                if (writer.IsSkipped)
                {
                    do
                    {
                        readBits += reader.SkipPrepend(bitCount);
                    } while (--iterations != 0);

                    goto Exit;
                }

                var minuend = GenerationPrimitives.GetPowerOfTwoMaskUInt32(bitCount);

                ref var value = ref GetFloatPtr(writer.Dereference(), iterations);

                while (true)
                {
                    readBits += CopyAsSubFromMinuend(ref reader, ref value, bitCount, multiplicand, minuend, lowValue);

                    if (--iterations == 0)
                        break;

                    value = ref Unsafe.Add(ref value, 1U);
                }

            Exit:
                return readBits;
            };
        }
    }

    private static EntityFieldDecoder CreateQuantizedZeroTopRoundUp(int bitCount, float lowValue, int iterations)
    {
        // ((1 << bits) - read(bits)) * (low / (1 << bits)) [(16 - read(4)) * (low / 16)]
        float multiplicand = lowValue / (1 << bitCount);
        
        if (iterations == 1)
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits;

                if (writer.IsSkipped)
                {
                    readBits = reader.SkipPrepend(bitCount);

                    goto Exit;
                }

                var minuend = 1U << bitCount;

                ref var value = ref GetFloatPtr(writer.Dereference());

                readBits = CopyAsSubFromMinuend(ref reader, ref value, bitCount, multiplicand, minuend, 0f);

            Exit:
                return readBits;
            };
        }
        else
        {
            return (ref AlignedLsBitReader reader, in EntityWriter writer) =>
            {
                int readBits = 0;

                if (writer.IsSkipped)
                {
                    do
                    {
                        readBits += reader.SkipPrepend(bitCount);
                    } while (--iterations != 0);

                    goto Exit;
                }

                var minuend = 1U << bitCount;

                ref var value = ref GetFloatPtr(writer.Dereference(), iterations);

                while (true)
                {
                    readBits += CopyAsSubFromMinuend(ref reader, ref value, bitCount, multiplicand, minuend, 0f);

                    if (--iterations == 0)
                        break;

                    value = ref Unsafe.Add(ref value, 1U);
                }

            Exit:
                return readBits;
            };
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref float GetFloatPtr(Span<byte> span, int iterations = 1)
    {
        Debug.Assert(span.Length >= sizeof(float) * iterations);

        ref var guy = ref MemoryMarshal.GetReference(span);
        Debug.Assert(GenerationPrimitives.CalculateMisalignment(in guy, sizeof(float)) == 0);

        return ref Unsafe.As<byte, float>(ref guy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T GetPtr<T>(Span<byte> span, int iterations = 1)
        where T : unmanaged
    {
        Debug.Assert(span.Length >= Unsafe.SizeOf<T>() * iterations);

        ref var guy = ref MemoryMarshal.GetReference(span);
        Debug.Assert(GenerationPrimitives.CalculateMisalignment(in guy, (uint)Unsafe.SizeOf<T>()) == 0);

        return ref Unsafe.As<byte, T>(ref guy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyAsSubFromMinuend(ref AlignedLsBitReader reader, ref float value, int bits, float multiplicand, uint minuend)
    {
        value = ReadAsSubFromMinuend(ref reader, bits, multiplicand, minuend);

        return bits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyAsMidpointXorMask(ref AlignedLsBitReader reader, ref float value, int bits, float multiplicand, uint mask)
    {
        value = ReadAsMidpointXorMask(ref reader, bits, multiplicand, mask);

        return bits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyAsMidpoint(ref AlignedLsBitReader reader, ref float value, int bits, float multiplicand)
    {
        value = ReadAsMidpoint(ref reader, bits, multiplicand);

        return bits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyAsSubFromMinuend(ref AlignedLsBitReader reader, ref float value, int bits, float multiplicand, uint minuend, float prepend)
    {
        int readBits = 1;

        if (reader.ReadBool())
        {
            value = prepend;
        }
        else
        {
            value = ReadAsSubFromMinuend(ref reader, bits, multiplicand, minuend);

            readBits += bits;
        }

        return readBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyAsAddToAddend(ref AlignedLsBitReader reader, ref float value, int bits, float multiplicand, uint addend)
    {
        value = ReadAsAddToAddend(ref reader, bits, multiplicand, addend);

        return bits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyAsAddToAddend(ref AlignedLsBitReader reader, ref float value, int bits, float multiplicand, uint addend, float prepend)
    {
        int readBits = 1;

        if (reader.ReadBool())
        {
            value = prepend;
        }
        else
        {
            value = ReadAsAddToAddend(ref reader, bits, multiplicand, addend);

            readBits += bits;
        }

        return readBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyAsStandard(ref AlignedLsBitReader reader, ref float value, int bits, float multiplicand, float prepend)
    {
        int readBits = 1;

        if (reader.ReadBool())
        {
            value = prepend;
        }
        else
        {
            value = ReadAsStandard(ref reader, bits, multiplicand);

            readBits += bits;
        }

        return readBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ReadAsSubFromMinuend(ref AlignedLsBitReader reader, int bitCount, float multiplicand, uint minuend)
    {
        var read = reader.ReadUnsigned<uint>(bitCount);
        return (minuend - read) * multiplicand;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ReadAsAddToAddend(ref AlignedLsBitReader reader, int bitCount, float multiplicand, uint addend)
    {
        var read = reader.ReadUnsigned<uint>(bitCount);
        return (addend + read) * multiplicand;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ReadAsStandard(ref AlignedLsBitReader reader, int bitCount, float multiplicand)
    {
        var read = reader.ReadUnsigned<uint>(bitCount);
        return read * multiplicand;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ReadAsMidpoint(ref AlignedLsBitReader reader, int bitCount, float multiplicand)
    {
        var read = reader.ReadUnsigned<uint>(bitCount);
        return ((read << 1) | 1) * multiplicand;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ReadAsMidpointXorMask(ref AlignedLsBitReader reader, int bitCount, float multiplicand, uint mask)
    {
        var read = reader.ReadUnsigned<uint>(bitCount);
        return ((read << 1) ^ mask) * multiplicand;
    }

    public EntityFieldDecoder GetFieldDecoder(ProtoFlattenedSerializerField_t field, int iterations = 1)
    {
        const int bitCountShiftForLookup = 24;
        Debug.Assert(iterations > 0 && iterations <= 1 << bitCountShiftForLookup);

        if (field is not null)
        {
            var bitCount = field.BitCount;
            if (((uint)bitCount - 1) < 31)
            {
                Debug.Assert(bitCount > 0 && bitCount < 32);

                // Read the floats as bits and eliminate any potential negative zeroes
                uint lowInt = BitConverter.SingleToUInt32Bits(field.LowValue),
                    highInt = BitConverter.SingleToUInt32Bits(field.HighValue);

                if (lowInt << 1 == 0)
                    lowInt = 0;

                if (highInt << 1 == 0)
                    highInt = 0;

                var flags = field.EncodeFlags;

                var lookup = new CacheKey(lowInt, highInt, flags, bitCount << bitCountShiftForLookup | iterations - 1);

                _cacheLock.EnterUpgradeableReadLock();
                try
                {
                    if (!_cache.TryGetValue(lookup, out var decoder))
                    {
                        float numerator;

                        if (lowInt == 0)
                        {
                            // lowInt is 0 or -0
                            if (highInt == 0)
                                numerator = 1f;
                            else
                                numerator = field.HighValue;

                            // var denominator = 1 << bitCount;

                            if (flags == 0)
                            {
                                // decoder = CreateQuantized(bitCount, 0, numerator / (denominator << 1), 0f, 0f, false, true, false);
                                decoder = CreateQuantizedZeroBottomNoFlags(bitCount, numerator, iterations);
                            }
                            else
                            {
                                if ((flags & (1 | 4)) != 0)
                                {
                                    // decoder = CreateQuantized(bitCount, 1, numerator / denominator, 0f, 0f, true, false, false);
                                    decoder = CreateQuantizedZeroBottomRoundDown(bitCount, numerator, iterations);
                                }
                                else if ((flags & 2) != 0)
                                {
                                    // decoder = CreateQuantized(bitCount, 0, numerator / denominator, 0f, numerator, true, false, false);
                                    decoder = CreateQuantizedZeroBottomRoundUp(bitCount, numerator, iterations);
                                }
                                else if ((flags & 8) != 0)
                                {
                                    // decoder = CreateQuantized(bitCount, 1, numerator / denominator, 0f, 0f, false, false, false);
                                    decoder = CreateQuantizedZeroBottom(bitCount, numerator, iterations);
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }
                        }
                        else if (highInt == 0)
                        {
                            // ZeroTop
                            numerator = field.LowValue;

                            // var denominator = 1 << bitCount;
                            
                            if (flags == 0)
                            {
                                // decoder = CreateQuantized(bitCount, -denominator, -numerator / (denominator << 1), 0f, 0f, false, true, false);
                                decoder = CreateQuantizedZeroTopNoFlags(bitCount, numerator, iterations);
                            }
                            else
                            {
                                if ((flags & 1) != 0)
                                {
                                    // decoder = CreateQuantized(bitCount, -denominator + 1, -numerator / denominator, 0f, numerator, true, false, false);
                                    decoder = CreateQuantizedZeroTopRoundDown(bitCount, numerator, iterations);
                                }
                                else if ((flags & (2 | 4)) != 0)
                                {
                                    // decoder = CreateQuantized(bitCount, -denominator, -numerator / denominator, 0f, 0f, true, false, false);
                                    decoder = CreateQuantizedZeroTopRoundUp(bitCount, numerator, iterations);
                                }
                                else if ((flags & 8) != 0)
                                {
                                    // decoder = CreateQuantized(bitCount, -denominator + 1, -numerator / denominator, 0f, 0f, false, false, false);
                                    decoder = CreateQuantizedZeroTop(bitCount, numerator, iterations);
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }
                        }
                        else if (lowInt << 1 == highInt << 1)
                        {
                            // ZeroCentered
                            numerator = field.HighValue;
                            var denominator = 1 << (bitCount - 1);
                            var addend = -denominator;

                            if (flags == 0)
                            {
                                decoder = CreateQuantized(bitCount, addend, numerator / (denominator << 1), 0f, 0f, false, true, false, iterations);
                            }
                            else
                            {
                                if ((flags & 1) != 0)
                                {
                                    decoder = CreateQuantized(bitCount, addend + 1, numerator / denominator, 0f, -numerator, true, false, false, iterations);
                                }
                                else if ((flags & 2) != 0)
                                {
                                    decoder = CreateQuantized(bitCount, addend, numerator / denominator, 0f, numerator, true, false, false, iterations);
                                }
                                else if ((flags & 4) != 0)
                                {
                                    decoder = CreateQuantized(bitCount, addend, numerator / denominator, 0f, 0f, true, false, true, iterations);
                                }
                                else if ((flags & 8) != 0)
                                {
                                    decoder = CreateQuantized(bitCount, addend + 1, numerator / denominator, 0f, 0f, false, false, false, iterations);
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }
                        }
                        else
                        {
                            // Two different algorithms to prevent overflow of large floats
                            if (((lowInt ^ highInt) & (1 << 31)) == 0)
                            {
                                // Same signs (offset algorithm)
                                numerator = field.HighValue - field.LowValue;
                                var denominator = 1 << bitCount;

                                if (flags == 0)
                                {
                                    decoder = CreateQuantized(bitCount, 0, numerator / (denominator << 1), field.LowValue, 0f, false, true, false, iterations);
                                }
                                else
                                {
                                    if ((flags & 1) != 0)
                                    {
                                        decoder = CreateQuantized(bitCount, 1, numerator / denominator, field.LowValue, field.LowValue, true, false, false, iterations);
                                    }
                                    else if ((flags & 2) != 0)
                                    {
                                        decoder = CreateQuantized(bitCount, 0, numerator / denominator, field.LowValue, numerator, true, false, false, iterations);
                                    }
                                    else if ((flags & 8) != 0)
                                    {
                                        decoder = CreateQuantized(bitCount, 1, numerator / denominator, field.LowValue, 0f, false, false, false, iterations);
                                    }
                                    else
                                    {
                                        throw new Exception();
                                    }
                                }
                            }
                            else
                            {
                                // Opposite signs (centering algorithm)
                                numerator = field.HighValue;
                                var denominator = 1 << (bitCount - 1);
                                var addend = -denominator;
                                var center = (field.HighValue + field.LowValue) / 2f;

                                if (flags == 0)
                                {
                                    decoder = CreateQuantized(bitCount, addend, numerator / (denominator << 1), center, 0f, false, true, false, iterations);
                                }
                                else
                                {
                                    if ((flags & 1) != 0)
                                    {
                                        decoder = CreateQuantized(bitCount, addend + 1, numerator / denominator, center, field.LowValue, true, false, false, iterations);
                                    }
                                    else if ((flags & 2) != 0)
                                    {
                                        decoder = CreateQuantized(bitCount, addend, numerator / denominator, center, numerator, true, false, false, iterations);
                                    }
                                    else if ((flags & 8) != 0)
                                    {
                                        decoder = CreateQuantized(bitCount, addend + 1, numerator / denominator, center, 0f, false, false, false, iterations);
                                    }
                                    else
                                    {
                                        throw new Exception();
                                    }
                                }
                            }
                        }

                        Debug.Assert(decoder is not null);

                        bool cacheWrite;

                        _cacheLock.EnterWriteLock();
                        try
                        {
                            cacheWrite = _cache.TryAdd(lookup, decoder);
                        }
                        finally
                        {
                            _cacheLock.ExitWriteLock();
                        }

                        if (!cacheWrite)
                            decoder = _cache[lookup];
                    }

                    return decoder;
                }
                finally
                {
                    _cacheLock.ExitUpgradeableReadLock();
                }
            }
        }

        return Fixed;
    }
}
