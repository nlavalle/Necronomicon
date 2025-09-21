using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace necronomicon.processor;

public static class ExtractionPrimitives
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CalculateShift8(int count)
    {
        const int BitMask = sizeof(byte) * 8 - 1;

        return count & BitMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CalculateReverseShift8(int count)
    {
        return CalculateShift8(-count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CalculateShift16(int count)
    {
        const int BitMask = sizeof(ushort) * 8 - 1;

        return count & BitMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CalculateReverseShift16(int count)
    {
        return CalculateShift16(-count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CalculateShift32(int count)
    {
        const int BitMask = sizeof(uint) * 8 - 1;

        switch (RuntimeInformation.ProcessArchitecture)
        {
            // Architectures that are known to mask shift values automatically
            case Architecture.X86:
            case Architecture.X64:
            case Architecture.Wasm:
            case Architecture.RiscV64:
                return count;
            default:
                return count & BitMask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CalculateReverseShift32(int count)
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            // Architectures that are known to have reverse subtract immediate
            case Architecture.Arm:
            case Architecture.Arm64:
                return 32 - count;
            default:
                return CalculateShift32(-count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CalculateShift64(int count)
    {
        const int BitMask = sizeof(ulong) * 8 - 1;

        switch (RuntimeInformation.ProcessArchitecture)
        {
            // Architectures that are known to mask shift values automatically
            case Architecture.X64:
            case Architecture.Wasm:
            case Architecture.RiscV64:
                return count;
            default:
                return count & BitMask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CalculateReverseShift64(int count)
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            // Architectures that are known to have reverse subtract immediate
            case Architecture.Arm64:
                return 64 - count;
            default:
                return CalculateShift64(-count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetPowerOfTwoMaskUInt64(int shift)
    {
#if NET5_0_OR_GREATER && !BZHI_MASK_DISABLE
        if (System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported)
        {
            return unchecked(System.Runtime.Intrinsics.X86.Bmi2.X64.ZeroHighBits((ulong)(long)-1, (uint)shift));
        }
        else
        {
#endif

            return (1UL << shift) - 1;

#if NET5_0_OR_GREATER && !BZHI_MASK_DISABLE
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetPowerOfTwoMaskUInt32(int shift)
    {
#if NET5_0_OR_GREATER && !BZHI_MASK_DISABLE
        if (System.Runtime.Intrinsics.X86.Bmi2.IsSupported)
        {
            return unchecked(System.Runtime.Intrinsics.X86.Bmi2.ZeroHighBits((uint)-1, (uint)shift));
        }
        else
        {
#endif

            return (1U << shift) - 1;

#if NET5_0_OR_GREATER && !BZHI_MASK_DISABLE
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetPowerOfTwoMaskInt32(int shift)
        => unchecked((int)GetPowerOfTwoMaskUInt32(shift));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetPowerOfTwoMaskInt64(int shift)
        => unchecked((long)GetPowerOfTwoMaskUInt64(shift));

    #region INT LSB

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ExtractInt32LSB(uint source, int bitOffset, int bitCount, int bitTotal)
    {
        int secondShift;
        int lefted;

        switch (RuntimeInformation.ProcessArchitecture)
        {
            // ROR to SAR is faster on architectures known to have ROR
            case Architecture.X86:
            case Architecture.X64:
            case Architecture.Arm:
            case Architecture.Arm64:
            case Architecture.Wasm:
                lefted = unchecked((int)BitOperations.RotateRight(source, CalculateShift32(bitTotal)));

                secondShift = CalculateShift32(-bitCount);
                break;
            default:
                var firstShift = CalculateShift32(-bitTotal);

                secondShift = CalculateShift32(-bitCount);

                lefted = unchecked((int)source) << firstShift;
                break;
        }

        return lefted >> secondShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ExtractInt32LSB(uint source, int bitCount)
    {
        int secondShift;
        int lefted;

        switch (RuntimeInformation.ProcessArchitecture)
        {
            // ROR to SAR is faster on architectures known to have ROR
            case Architecture.X86:
            case Architecture.X64:
            case Architecture.Arm:
            case Architecture.Arm64:
            case Architecture.Wasm:
                lefted = unchecked((int)BitOperations.RotateRight(source, CalculateShift32(bitCount)));

                secondShift = CalculateShift32(-bitCount);
                break;
            default:
                secondShift = CalculateShift32(-bitCount);

                lefted = unchecked((int)source) << secondShift;
                break;
        }

        return lefted >> secondShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ExtractInt64LSB(ulong source, int bitOffset, int bitCount, int bitTotal)
    {
        int secondShift;
        long lefted;

        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
            case Architecture.Arm64:
            case Architecture.Wasm:
                lefted = unchecked((long)BitOperations.RotateRight(source, CalculateShift64(bitTotal)));

                secondShift = CalculateShift64(-bitCount);
                break;
            default:
                var firstShift = CalculateShift64(-bitTotal);

                secondShift = CalculateShift64(-bitCount);

                lefted = unchecked((long)source) << firstShift;
                break;
        }

        return lefted >> secondShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ExtractInt64LSB(ulong source, int bitCount)
    {
        int secondShift;
        long lefted;

        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
            case Architecture.Arm64:
            case Architecture.Wasm:
                lefted = unchecked((long)BitOperations.RotateRight(source, CalculateShift64(bitCount)));

                secondShift = CalculateShift64(-bitCount);
                break;
            default:
                secondShift = CalculateShift64(-bitCount);

                lefted = unchecked((long)source) << secondShift;
                break;
        }

        return lefted >> secondShift;
    }

    #endregion

    #region INT MSB

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ExtractInt32MSB(uint source, int bitOffset, int bitCount)
    {
        var lefted = unchecked((int)source) << bitOffset;

        int secondShift = CalculateShift32(-bitCount);

        return lefted >> secondShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ExtractInt32MSB(uint source, int bitCount)
    {
        int secondShift = CalculateShift32(-bitCount);

        return unchecked((int)source) >> secondShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ExtractInt64MSB(ulong source, int bitOffset, int bitCount)
    {
        var lefted = unchecked((long)source) << bitOffset;

        int secondShift = CalculateShift64(-bitCount);

        return lefted >> secondShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ExtractInt64MSB(ulong source, int bitCount)
    {
        int secondShift = CalculateShift64(-bitCount);

        return unchecked((long)source) >> secondShift;
    }

    #endregion

    #region UINT LSB

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ExtractUInt32LSB(uint source, int bitOffset, int bitCount, int bitTotal)
    {
        // The bit count is likely to be a constant, offset is never:
        // algorithms should take this into account (variable/constant cycles)
#if NET5_0_OR_GREATER
        if (System.Runtime.Intrinsics.X86.Bmi2.IsSupported)
        {
            // SHRX -> BZHI is faster on BMI2 systems (2/2 cycles)
            // BZHI <- SHRX     (2/2)
            //                  (0/-)
            //    ^ <- MOV      (-/2)
            return System.Runtime.Intrinsics.X86.Bmi2.ZeroHighBits(source >> CalculateShift32(bitOffset), unchecked((uint)bitCount));
        }
#if ROR_TO_SHR_UINT_ENABLED
        else if (X86Base.IsSupported)
        {
            // ROR -> SHR is faster/slower on X86 systems (3/3 cycles)
            // SHR <- ROR <- ADD        (3/3)
            //   ^ <- NEG               (2/-)
            //   ^ *imm                 (-/1)
            return BitOperations.RotateRight(source, bitTotal) >> CalculateShift32(-count);
        }
#endif
        else
        {
#endif
            // Other platforms are generally faster with default "extraction" code
            // X86: (4/2 cycles)
            // AND <- SHR                   (2/2)
            //   ^ <- DEC <- SHL <- MOV     (4/-)
            //   ^ *imm                     (-/1)
            // ARM: (3/2-3 cycles)
            // AND <- LSR                   (2/2)
            //   ^ <- SUB <- MOV            (3/3)
            //   ^ *imm                     (-/1) where count <= 8
            // BFC <- LSR                   (-/2)
            return source >> bitOffset & ((1U << bitCount) - 1U);
#if NET5_0_OR_GREATER
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ExtractUInt64LSB(ulong source, int bitOffset, int bitCount, int bitTotal)
    {
        // The bit count is likely to be a constant, offset is never:
        // algorithms should take this into account (variable/constant cycles)
#if NET5_0_OR_GREATER
        if (System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported)
        {
            // SHRX -> BZHI is faster on BMI2 systems (2/2 cycles)
            // BZHI <- SHRX     (2/2)
            //                  (0/-)
            //    ^ <- MOV      (-/2)
            return System.Runtime.Intrinsics.X86.Bmi2.X64.ZeroHighBits(source >> CalculateShift64(bitOffset), unchecked((uint)bitCount));
        }
#if ROR_TO_SHR_UINT_ENABLED
        else if (X86Base.X64.IsSupported)
        {
            // ROR -> SHR is faster/slower on X86 systems (3/3 cycles)
            // SHR <- ROR <- ADD        (3/3)
            //   ^ <- NEG               (2/-)
            //   ^ *imm                 (-/1)
            return BitOperations.RotateRight(source, bitTotal) >> CalculateShift64(-count);
        }
#endif
        else
        {
#endif

            // Other platforms are generally faster with default "extraction" code
            // X86: (4/2 cycles)
            // AND <- SHR                   (2/2)
            //   ^ <- DEC <- SHL <- MOV     (4/-)
            //   ^ *imm                     (-/1)
            // ARM: (3/2-3 cycles)
            // AND <- LSR                   (2/2)
            //   ^ <- SUB <- MOV            (3/3)
            //   ^ *imm                     (-/1) where count <= 8
            // BFC <- LSR                   (-/2)
            return source >> bitOffset & ((1UL << bitCount) - 1UL);

#if NET5_0_OR_GREATER
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ExtractUInt64LSB(ulong source, int bitCount)
    {
#if NET5_0_OR_GREATER
        if (System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported)
        {
            return System.Runtime.Intrinsics.X86.Bmi2.X64.ZeroHighBits(source, unchecked((uint)bitCount));
        }
#if ROR_TO_SHR_UINT_ENABLED
        else if (X86Base.X64.IsSupported)
        {
            return BitOperations.RotateRight(source, bitCount) >> CalculateShift64(-count);
        }
#endif
        else
        {
#endif

            return source & ((1UL << bitCount) - 1UL);

#if NET5_0_OR_GREATER
        }
#endif
    }

    #endregion

    #region UINT MSB

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ExtractUInt32MSB(uint source, int bitOffset, int bitCount)
    {
        // X86: (2/2 cycles)
        // SHR <- SHL           (2/2)
        //   ^ <- NEG           (2/-)
        //   ^ *imm             (-/1)
        // ARM: (3/2 cycles)
        // LSR <- LSL           (2/2)
        //   ^ <- RSB           (3/-)
        //   ^ *imm             (-/2)
        int secondShift = CalculateShift32(-bitCount);

        return source << bitOffset >> secondShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ExtractUInt32MSB(uint source, int bitCount)
    {
        int secondShift = CalculateShift32(-bitCount);

        return source >> secondShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ExtractUInt64MSB(ulong source, int bitOffset, int bitCount)
    {
        // X86: (2/2 cycles)
        // SHR <- SHL           (2/2)
        //   ^ <- NEG           (2/-)
        //   ^ *imm             (-/1)
        // ARM: (3/2 cycles)
        // LSR <- LSL           (2/2)
        //   ^ <- RSB           (3/-)
        //   ^ *imm             (-/2)
        int secondShift = CalculateShift64(-bitCount);

        return source << bitOffset >> secondShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ExtractUInt64MSB(ulong source, int bitCount)
    {
        int secondShift = CalculateShift64(-bitCount);

        return source >> secondShift;
    }

    #endregion

    #region BOOL LSB

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolLSB(byte source, int offset)
    {
        // X86: (2 cycles)
        // BT <- AND
        // ARM: (2 cycles)
        // TST <- AND
        //   ^ <- MOV
        return (source & (1U << CalculateShift8(offset))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolLSB(byte source)
    {
        return (source & 1U) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolLSB(uint source, int offset)
    {
        // X86: (1 cycles)
        // BT
        // ARM: (2 cycles)
        // TST <- AND
        //   ^ <- MOV
        return (source & (1U << CalculateShift32(offset))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolLSB(uint source)
    {
        // X86: (1 cycles)
        // BT
        // ARM: (2 cycles)
        // TST <- AND
        //   ^ <- MOV
        return (source & 1U) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolLSB(ulong source, int offset)
    {
        // X86: (1 cycles)
        // BT
        // ARM: (2 cycles)
        // TST <- AND
        //   ^ <- MOV
        return (source & (1UL << CalculateShift64(offset))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolLSB(ulong source)
    {
        // X86: (1 cycles)
        // BT
        // ARM: (2 cycles)
        // TST <- AND
        //   ^ <- MOV
        return (source & 1UL) != 0;
    }

    #endregion

    #region BOOL MSB

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolMSB(byte source, int offset)
    {
        // BMI1 (3 cycles):
        // BT <- ANDN <- MOV
        // X86 (3 cycles):
        // BT <- AND <- NOT
        //         ^ *imm
        // ARM: (3 cycles)
        // TST <- BIC <- MOV
        //   ^ <- MOV
        return (source & (1U << CalculateShift8(~offset))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolMSB(byte source)
    {
        // BMI1 (3 cycles):
        // BT <- ANDN <- MOV
        // X86 (3 cycles):
        // BT <- AND <- NOT
        //         ^ *imm
        // ARM: (3 cycles)
        // TST <- BIC <- MOV
        //   ^ <- MOV
        return (source & (1U << 7)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolMSB(uint source, int offset)
    {
        // BMI1 (3 cycles):
        // BT <- ANDN <- MOV
        // X86 (3 cycles):
        // BT <- AND <- NOT
        //         ^ *imm
        // ARM: (3 cycles)
        // TST <- BIC <- MOV
        //   ^ <- MOV
        return (source & (1U << CalculateShift32(~offset))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolMSB(uint source)
    {
        // BMI1 (3 cycles):
        // BT <- ANDN <- MOV
        // X86 (3 cycles):
        // BT <- AND <- NOT
        //         ^ *imm
        // ARM: (3 cycles)
        // TST <- BIC <- MOV
        //   ^ <- MOV
        return (source & (1U << 31)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolMSB(ulong source, int offset)
    {
        // BMI1 (3 cycles):
        // BT <- ANDN <- MOV
        // X86 (3 cycles):
        // BT <- AND <- NOT
        //         ^ *imm
        // ARM: (3 cycles)
        // TST <- BIC <- MOV
        //   ^ <- MOV
        return (source & (1UL << CalculateShift64(~offset))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ExtractBoolMSB(ulong source)
    {
        // BMI1 (3 cycles):
        // BT <- ANDN <- MOV
        // X86 (3 cycles):
        // BT <- AND <- NOT
        //         ^ *imm
        // ARM: (3 cycles)
        // TST <- BIC <- MOV
        //   ^ <- MOV
        return (source & (1UL << 63)) != 0;
    }

    #endregion

}

public readonly struct ReadOnlyAlignedBitMemory
{
    private readonly ReadOnlyMemory<byte> _buffer;
    private readonly int _alignment;

    public int Length => _buffer.Length * 8;

    public ReadOnlyAlignedBitSpan Extractor => ReadOnlyAlignedBitSpan.Create(_buffer.Span, _alignment);

    public ReadOnlyAlignedBitMemory(ReadOnlyMemory<byte> buffer)
    {
        _buffer = buffer;
        _alignment = ReadOnlyAlignedBitSpan.CalculateAlignment(buffer.Span, Unsafe.SizeOf<nint>());
    }

    public BitSpanLSBReader CreateLSBReader(int bitOffset)
    {
        if ((uint)bitOffset > (uint)_buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(bitOffset));

        return new BitSpanLSBReader(Extractor, bitOffset);
    }
}

public readonly ref struct ReadOnlyAlignedBitSpan
{
    internal const int BitsPerByte = 8;
    internal const int SizeOfULong = sizeof(ulong);
    internal const int BitsInULong = SizeOfULong * BitsPerByte;

    private readonly ReadOnlySpan<byte> _buffer;
    private readonly int _bitAlignment;
    private readonly int _bitLength;

    public int Length => _bitLength;

    internal ReadOnlyAlignedBitSpan(ReadOnlySpan<byte> buffer, int bitAlignment, int bitLength)
    {
        Debug.Assert(bitAlignment + bitLength <= buffer.Length * BitsPerByte);

        _buffer = buffer;
        _bitAlignment = bitAlignment;
        _bitLength = bitLength;
    }

    public ReadOnlyAlignedBitSpan Slice(int bitStart)
    {
        if ((uint)bitStart > (uint)_bitLength)
            throw new ArgumentOutOfRangeException(nameof(bitStart));

        var bitAlignment = _bitAlignment + bitStart;
        var newAlignment = bitAlignment & 63;
        var byteAlignment = (bitAlignment >> 3) & ~7;

        return new ReadOnlyAlignedBitSpan(_buffer.Slice(byteAlignment), newAlignment, _bitLength - bitStart);
    }

    public ReadOnlyAlignedBitSpan Slice(int bitStart, int bitLength)
    {
        if ((ulong)(uint)bitStart + (ulong)(uint)bitLength > (ulong)(uint)_bitLength)
            throw new ArgumentOutOfRangeException();

        var bitAlignment = _bitAlignment + bitStart;
        var newAlignment = bitAlignment & 63;
        var alignedBitLength = newAlignment + bitLength;
        var byteAlignment = bitAlignment >> 6 & ~7;
        var byteLength = ((alignedBitLength + 63) >> 3) & ~7;

        return new ReadOnlyAlignedBitSpan(_buffer.Slice(byteAlignment, byteLength), newAlignment, bitLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetByteAlignedBuffer(int bitOffset, int byteCount, out ReadOnlySpan<byte> buffer)
    {
        var bitStart = bitOffset + _bitAlignment;
        if ((bitStart & 7) == 0)
        {
            buffer = _buffer.Slice(bitStart >> 3, byteCount);
            return true;
        }
        else
        {
            buffer = default;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyBytesLSB(int bitOffset, Span<byte> dest)
    {
        var bitStart = bitOffset + _bitAlignment;
        if ((bitStart & 7) == 0)
        {
            //buffer = _buffer.Slice(bitStart >> 3, dest.Length);
            return true;
        }
        else
        {
            //buffer = default;
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyBytesLSB(ref BitExtractor data, int bitOffset, Span<byte> dest)
    {
        var byteLength = dest.Length;

        var bitTotal = (ulong)(uint)bitOffset + (ulong)(uint)byteLength * 8;
        if (bitTotal > (ulong)(uint)_bitLength)
            return false;

        var bitStart = bitOffset + _bitAlignment;
        var bytePosition = CalculateRoundedDownLength(bitStart >> 3, sizeof(ulong));
        ref byte src = ref Unsafe.Add(ref MemoryMarshal.GetReference(_buffer), (uint)bytePosition);

        if ((bitStart & 7) == 0)
        {
            // Direct copy (aligned)
            var sliced = MemoryMarshal.CreateReadOnlySpan(in src, byteLength);
            
            return sliced.TryCopyTo(dest);
        }
        else
        {
            // 1. Destination Alignment
            // 2. Copy chunks
            // 3. Fill in 4-2-1
            ref byte dst = ref MemoryMarshal.GetReference(dest);

            if (byteLength >= 8)
            {
                var lower = InternalReadAddressLSB(in src);

                var upperShift = -bitStart & 63;
                // Destination alignment
                var destMisalignment = -(int)CalculateAddress(ref dst) & 7;
                if (destMisalignment != 0)
                {
                    var srcCheck = bitStart & 64;

                    var upper = InternalReadAddressLSB(in Unsafe.Add(ref src, (uint)sizeof(ulong)));

                    BinaryPrimitives.WriteUInt64LittleEndian(
                        MemoryMarshal.CreateSpan(ref dst, sizeof(ulong)),
                        (lower >> bitStart) | (upper << upperShift)
                    );

                    bitStart += destMisalignment * 8;
                    upperShift = -bitStart & 63;
                    dst = ref Unsafe.Add(ref dst, (uint)destMisalignment);
                    byteLength -= destMisalignment;

                    // Check if bit 64 of bitStart changed during alignment
                    if ((srcCheck ^ (bitStart & 64)) != 0)
                    {
                        src = ref Unsafe.Add(ref src, (uint)sizeof(ulong));
                        lower = upper;
                    }
                }

                var iterations8 = byteLength >> 3;
                bitStart += iterations8 * 64;

                while (iterations8-- != 0)
                {
                    // Copy chunks
                    ref var upperRef = ref Unsafe.Add(ref src, (uint)sizeof(ulong));
                    var upper = InternalReadAddressLSB(in upperRef);

                    BinaryPrimitives.WriteUInt64LittleEndian(
                        MemoryMarshal.CreateSpan(ref dst, sizeof(ulong)),
                        (lower >> bitStart) | (upper << upperShift)
                    );

                    lower = upper;
                    src = ref upperRef;
                    dst = ref Unsafe.Add(ref dst, (uint)sizeof(ulong));
                }

                data = new BitExtractor(lower >> bitStart, bitStart, Math.Min(upperShift, _bitLength - bitStart));
            }

            // Fill in 4-2-1
            if ((byteLength & (1 << 2)) != 0)
            {
                // Copy 4 bytes
                if (!TryExtractUnsignedLSB(ref data, bitStart, sizeof(uint) * 8, out uint copy))
                    throw new UnreachableException();

                BinaryPrimitives.WriteUInt32LittleEndian(
                    MemoryMarshal.CreateSpan(ref dst, sizeof(uint)),
                    copy
                );

                bitStart += sizeof(uint) * 8;
                dst = ref Unsafe.Add(ref dst, (uint)sizeof(uint));
            }

            if ((byteLength & (1 << 1)) != 0)
            {
                // Copy 2 bytes
                if (!TryExtractUnsignedLSB(ref data, bitStart, sizeof(ushort) * 8, out ushort copy))
                    throw new UnreachableException();

                BinaryPrimitives.WriteUInt16LittleEndian(
                    MemoryMarshal.CreateSpan(ref dst, sizeof(ushort)),
                    copy
                );

                bitStart += sizeof(ushort) * 8;
                dst = ref Unsafe.Add(ref dst, (uint)sizeof(ushort));
            }

            if ((byteLength & 1) != 0)
            {
                // Copy 1 byte
                if (!TryExtractUnsignedLSB(ref data, bitStart, sizeof(byte) * 8, out byte copy))
                    throw new UnreachableException();

                dst = copy;

                bitStart += sizeof(byte) * 8;
                dst = ref Unsafe.Add(ref dst, (uint)sizeof(byte));
            }

            Debug.Assert(bitStart == (int)bitTotal);
        }

        return true;
    }


    public static ReadOnlyAlignedBitSpan Create(ReadOnlySpan<byte> buffer)
        => Create(buffer, 0, buffer.Length * 8);

    public static ReadOnlyAlignedBitSpan Create<T>(ReadOnlySpan<T> buffer) where T : struct
        => Create(MemoryMarshal.AsBytes(buffer));

    public static ReadOnlyAlignedBitSpan Create(ReadOnlySpan<byte> buffer, int bitStart)
        => Create(buffer, bitStart, buffer.Length * 8 - bitStart);

    public static ReadOnlyAlignedBitSpan Create<T>(ReadOnlySpan<T> buffer, int bitStart) where T : struct
        => Create(MemoryMarshal.AsBytes(buffer), bitStart);

    public static ReadOnlyAlignedBitSpan Create(ReadOnlySpan<byte> buffer, int bitStart, int bitLength)
    {
        int alignment, length;

#pragma warning disable IDE0057 // Use range operator
        buffer = buffer.Slice(bitStart >> 3);
#pragma warning restore IDE0057 // Use range operator

        bitStart &= 7;

        if (buffer.Length == 0)
        {
            if (bitStart != 0 || bitLength != 0)
                throw new ArgumentException();

            return default;
        }

        if ((ulong)(uint)bitStart + (ulong)(uint)bitLength > (ulong)(uint)buffer.Length * 8)
            throw new ArgumentOutOfRangeException();

        var sizeOfNint = Unsafe.SizeOf<nint>();

        // This should be elided by the JIT
        if (sizeOfNint < sizeof(uint))
            throw new PlatformNotSupportedException();

        alignment = CalculateAlignment(buffer, sizeOfNint);
        length = CalculateRoundedUpLength(buffer.Length + alignment, sizeOfNint);
        Debug.Assert(length >= 0);

        var bitAlignment = bitStart + alignment * BitsPerByte;

        return InternalCreate(buffer, alignment, length, bitAlignment, bitLength);
    }

    public static ReadOnlyAlignedBitSpan Create<T>(ReadOnlySpan<T> buffer, int bitStart, int bitLength) where T : struct
        => Create(MemoryMarshal.AsBytes(buffer), bitStart, bitLength);

    internal static ReadOnlyAlignedBitSpan InternalCreate(ReadOnlySpan<byte> buffer, int byteAlignment, int byteLength, int bitAlignment, int bitLength)
    {
        var span = MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Subtract(ref MemoryMarshal.GetReference(buffer), (uint)byteAlignment),
            byteLength
        );

        return new ReadOnlyAlignedBitSpan(span, bitAlignment, bitLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CalculateAlignment(ReadOnlySpan<byte> buffer, int sizeOf)
    {
        return (int)CalculateAddress(ref MemoryMarshal.GetReference(buffer)) & (sizeOf - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nint CalculateAddress(ref byte ptr)
    {
        var offset = Unsafe.ByteOffset(ref Unsafe.NullRef<byte>(), ref ptr);
        Debug.Assert((ulong)offset >= 0);

        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitSpecInBounds(int bitOffset, int bitCount)
        => IsBitSpecInBounds(bitOffset, bitCount, out _, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsBitSpecInBounds(int bitOffset, int bitCount, out int offset, out int remainder)
    {
        var temp = unchecked((ulong)(uint)_bitLength - (ulong)(uint)bitOffset);
        if (unchecked(temp < (ulong)(uint)bitCount))
        {
            offset = 0;
            remainder = 0;
            return false;
        }

        remainder = (int)temp;
        offset = bitOffset + _bitAlignment;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetExtractorLSB(int bitOffset, int bitCount, out BitExtractor data)
    {
        if (!IsBitSpecInBounds(bitOffset, bitCount, out var offset, out var remainder))
        {
            data = default;
            return false;
        }

        var bytePosition = CalculateRoundedDownLength(offset >> 3, sizeof(ulong));

        ref var ptr = ref Unsafe.Add(ref MemoryMarshal.GetReference(_buffer), (uint)bytePosition);
        ulong result = InternalReadAddressLSB(in ptr);

        var bitPosition = offset & 63;
        if (bitPosition != 0)
        {
            result >>= bitPosition;

            if (remainder + bitPosition > 64)
            {
                Debug.Assert(bytePosition + sizeof(ulong) < _buffer.Length);

                result = InternalReadMixLSB(
                    in Unsafe.Add(ref ptr, (uint)sizeof(long)),
                    result,
                    ExtractionPrimitives.CalculateReverseShift64(bitPosition)
                );
            }
        }

        if (remainder > 64)
            remainder = 64;

        data = new BitExtractor(result, bitOffset, remainder);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal (ulong buffer, int bits) InternalGetExtractorLSB(int offset, int remainder)
    {
        var bytePosition = CalculateRoundedDownLength(offset >> 3, sizeof(ulong));

        ref var ptr = ref Unsafe.Add(ref MemoryMarshal.GetReference(_buffer), (uint)bytePosition);
        ulong result = BinaryPrimitives.ReadUInt64LittleEndian(
            MemoryMarshal.CreateReadOnlySpan(ref ptr, sizeof(ulong))
        );

        var bitPosition = offset & 63;
        if (bitPosition != 0 && remainder + bitPosition > 64)
        {
            Debug.Assert(bytePosition + sizeof(ulong) < _buffer.Length);

            result = InternalReadMixLSB(
                in Unsafe.Add(ref ptr, (uint)sizeof(long)),
                result,
                ExtractionPrimitives.CalculateReverseShift64(bitPosition)
            );
        }

        if (remainder > 64)
            remainder = 64;

        return (result, remainder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ulong InternalCopy8ByteIterationsLSB(scoped ref byte src, scoped ref byte dest, ulong current, int lowerShift, int upperShift, int iterations)
    {
        Debug.Assert(iterations > 0);

        var lower = current;

        do
        {
            BinaryPrimitives.WriteUInt64LittleEndian(
                MemoryMarshal.CreateSpan(ref dest, sizeof(ulong)),
                InternalReadMixReplaceLSB(in src, ref lower, lowerShift, upperShift)
            );

            src = ref Unsafe.Add(ref src, (uint)sizeof(ulong));
            dest = ref Unsafe.Add(ref dest, (uint)sizeof(ulong));
        } while (--iterations != 0);

        return lower;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong InternalReadMixReplaceLSB(scoped in byte upperRef, scoped ref ulong lowerRef, int lowerShift, int upperShift)
    {
        ulong upper = InternalReadAddressLSB(in upperRef);

        var result = lowerRef | (upper << upperShift);
        lowerRef = upper >> lowerShift;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong InternalReadMixReplaceMSB(scoped in byte upperRef, scoped ref ulong lowerRef, int lowerShift, int upperShift)
    {
        ulong upper = InternalReadAddressMSB(in upperRef);

        var result = lowerRef | (upper >> upperShift);
        lowerRef = upper << lowerShift;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong InternalReadMixLSB(scoped in byte upperPtr, ulong result, int upperShift)
    {
        var upper = InternalReadAddressLSB(in upperPtr);
        return result | (upper << upperShift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong InternalReadMixMSB(scoped in byte upperPtr, ulong result, int upperShift)
    {
        var upper = InternalReadAddressMSB(in upperPtr);
        return result | (upper >> upperShift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong InternalReadAddressLSB(scoped in byte ptr)
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(
            MemoryMarshal.CreateReadOnlySpan(in ptr, sizeof(ulong))
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong InternalReadAddressMSB(scoped in byte ptr)
    {
        return BinaryPrimitives.ReadUInt64BigEndian(
            MemoryMarshal.CreateReadOnlySpan(in ptr, sizeof(ulong))
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetExtractorMSB(int bitOffset, int bitCount, out BitExtractor data)
    {
        if (!IsBitSpecInBounds(bitOffset, bitCount, out var offset, out var remainder))
        {
            data = default;
            return false;
        }

        var bytePosition = CalculateRoundedDownLength(offset >> 3, sizeof(ulong));

        ref var ptr = ref Unsafe.Add(ref MemoryMarshal.GetReference(_buffer), (uint)bytePosition);
        ulong result = BinaryPrimitives.ReadUInt64BigEndian(
            MemoryMarshal.CreateReadOnlySpan(ref ptr, sizeof(ulong))
        );

        var bitPosition = offset & 63;
        if (bitPosition != 0)
        {
            result <<= bitPosition;

            var temp = BinaryPrimitives.ReadUInt64BigEndian(
                MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref ptr, (uint)sizeof(long)), sizeof(ulong))
            );

            temp >>= ExtractionPrimitives.CalculateShift64(-bitPosition);
            result |= temp;
        }

        if (remainder > 64)
            remainder = 64;

        data = new BitExtractor(result, bitOffset, remainder);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateRoundedDownLength(int length, int sizeOf)
    {
        var mask = sizeOf - 1;
        return length & ~mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateRoundedUpLength(int length, int sizeOf)
    {
        var mask = sizeOf - 1;
        return (length + mask) & ~mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractUnsignedLSB<T>(ref BitExtractor data, int bitOffset, int bitCount, out T value) where T : unmanaged
    {
        if (data.IsBitSpecInBounds(bitOffset, bitCount, out var offset, out var total))
        {
            value = data.InternalExtractUnsignedLSB<T>(offset, bitCount, total);
            return true;
        }

        if (TryGetExtractorLSB(bitOffset, bitCount, out data))
        {
            value = data.InternalExtractUnsignedLSB<T>(bitCount);
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractSignedLSB<T>(ref BitExtractor data, int bitOffset, int bitCount, out T value) where T : unmanaged
    {
        if (data.IsBitSpecInBounds(bitOffset, bitCount, out var offset, out var total))
        {
            value = data.InternalExtractSignedLSB<T>(offset, bitCount, total);
            return true;
        }

        if (TryGetExtractorLSB(bitOffset, bitCount, out data))
        {
            value = data.InternalExtractSignedLSB<T>(bitCount);
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractBoolLSB(ref BitExtractor data, int bitOffset, out bool value)
    {
        if (data.IsBitOffsetInBounds(bitOffset, out var offset))
        {
            value = data.InternalExtractBoolLSB(offset);
            return true;
        }

        if (TryGetExtractorLSB(bitOffset, 1, out data))
        {
            value = data.InternalExtractBoolLSB();
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractUnsignedMSB<T>(ref BitExtractor data, int bitOffset, int bitCount, out T value) where T : unmanaged
    {
        if (data.IsBitSpecInBounds(bitOffset, bitCount, out var offset, out _))
        {
            value = data.InternalExtractUnsignedMSB<T>(offset, bitCount);
            return true;
        }

        if (TryGetExtractorMSB(bitOffset, bitCount, out data))
        {
            value = data.InternalExtractUnsignedMSB<T>(bitCount);
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractSignedMSB<T>(ref BitExtractor data, int bitOffset, int bitCount, out T value) where T : unmanaged
    {
        if (data.IsBitSpecInBounds(bitOffset, bitCount, out var offset, out _))
        {
            value = data.InternalExtractSignedMSB<T>(offset, bitCount);
            return true;
        }

        if (TryGetExtractorMSB(bitOffset, bitCount, out data))
        {
            value = data.InternalExtractSignedMSB<T>(bitCount);
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractBytesLSB(int bitOffset, Span<byte> dest)
    {
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractBytesMSB(int bitOffset, Span<byte> dest)
    {
        return false;
    }

}

public static class BitSpanReaderExtensions
{
    public static bool ReadBool(this ref BitSpanLSBReader reader)
    {
        if (!reader.TryReadBool(out var value))
            throw new Exception();

        return value;
    }

    public static T ReadUnsigned<T>(this ref BitSpanLSBReader reader, int bitCount) where T : unmanaged
    {
        if (!reader.TryReadUnsigned<T>(bitCount, out var value))
            throw new Exception();

        return value;
    }

    public static bool TryReadZigZagInt32(this ref BitSpanLSBReader reader, out int value)
    {
        if (reader.TryReadVarUInt32(out uint working))
        {
            value = (int)(working >> 1) ^ -((int)working & 1);
            return true;
        }

        value = 0;
        return false;
    }

    public static uint ReadVarUInt32(this ref BitSpanLSBReader reader)
    {
        if (!reader.TryReadVarUInt32(out var value))
            throw new Exception();

        return value;
    }

    public static bool TryReadVarUInt32(this ref BitSpanLSBReader reader, out uint value)
    {
        uint result = 0;
        int shift = 0;
        var offset = reader.Offset;

        while (reader.TryReadUnsigned(8, out uint working))
        {
            if (shift > 28)
                break;

            result |= (working & 0x7F) << shift;

            if ((working & 0x80) == 0U)
            {
                value = result;
                return true;
            }

            shift += 7;
        }

        reader.Offset = offset;
        value = 0;
        return false;
    }

    public static int ReadVarInt32(this ref BitSpanLSBReader reader)
    {
        if (!reader.TryReadVarInt32(out var value))
            throw new Exception();

        return value;
    }

    public static bool TryReadVarInt32(this ref BitSpanLSBReader reader, out int value)
    {
        if (reader.TryReadVarUInt32(out var result))
        {
            value = (int)result;
            return true;
        }
        else
        {
            value = 0;
            return false;
        }
    }

    public static bool TryReadEmbeddedInt(this ref BitSpanLSBReader reader, out uint value)
    {
        const int ReadBits = 6;
        const int DataBits = 4;
        const int Magic = 16;

        const int DataMask = (1 << DataBits) - 1;
        const int OpBits = ReadBits - DataBits;
        const int OpMask = (1 << OpBits) - 1;
        const int Addend = Magic - OpMask;

        // This is a unique header from Valve, the first two bits indicate how many bits to take after
        if (reader.TryReadUnsigned(ReadBits, out uint low6))
        {
            var op = (int)(low6 >> DataBits);
            if (op == 0)
            {
                value = low6;
                return true;
            }

            // 01b = 4, 10b = 8, 11b = 28
            var upperCount = op * 4 + ((op + Addend) & Magic);

            if (reader.TryReadUnsigned(upperCount, out uint upper))
            {
                value = (low6 & DataMask) | (upper << DataBits);
                return true;
            }

            reader.Offset -= ReadBits;
        }

        value = 0;
        return false;
    }


}

public ref struct BitSpanLSBReader
{
    private readonly ReadOnlyAlignedBitSpan _extractor;
    private BitExtractor _data;
    private int _offset;

    public int Offset
    {
        get => _offset;
        set
        {
            if ((uint)value > (uint)_extractor.Length)
                throw new ArgumentOutOfRangeException(nameof(value));

            _offset = value;
        }
    }
    public int Remaining => _extractor.Length - Offset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal BitSpanLSBReader(ReadOnlyAlignedBitSpan extractor, int offset)
    {
        _extractor = extractor;
        _data = default;
        _offset = offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitSpanLSBReader Create(ReadOnlySpan<byte> buffer)
    {
        return new BitSpanLSBReader(ReadOnlyAlignedBitSpan.Create(buffer), 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdvance(int bitCount)
    {
        var offset = (ulong)(uint)Offset + (ulong)(uint)bitCount;
        if (offset > (ulong)(uint)_extractor.Length)
            return false;

        _offset = (int)offset;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBool(out bool value)
    {
        if (TryPeek(out value))
        {
            _offset++;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUnsigned<T>(int bitCount, out T value) where T : unmanaged
    {
        if (TryPeekUnsigned(bitCount, out value))
        {
            _offset += bitCount;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadSigned<T>(int bitCount, out T value) where T : unmanaged
    {
        if (TryPeekSigned(bitCount, out value))
        {
            _offset += bitCount;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(out bool value)
        => _extractor.TryExtractBoolLSB(ref _data, Offset, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeekSigned<T>(int bitCount, out T value) where T : unmanaged
        => _extractor.TryExtractSignedLSB(ref _data, Offset, bitCount, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeekUnsigned<T>(int bitCount, out T value) where T : unmanaged
        => _extractor.TryExtractUnsignedLSB(ref _data, Offset, bitCount, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetByteAlignedBuffer(int byteCount, out ReadOnlySpan<byte> buffer)
    {
        return _extractor.TryGetByteAlignedBuffer(Offset, byteCount, out buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyTo(Span<byte> buffer)
    {
        if (_extractor.TryCopyBytesLSB(ref _data, Offset, buffer))
        {
            _offset += buffer.Length * 8;
            return true;
        }

        return false;
    }
}

public readonly struct BitExtractor
{
    private readonly ulong _data;
    private readonly int _offset;
    private readonly int _available;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitExtractor(ulong data)
        : this(data, 0, sizeof(ulong) * 8) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitExtractor(ulong data, int available)
        : this(data, 0, available) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal BitExtractor(ulong data, int offset, int available)
    {
        _data = data;
        _offset = offset;
        _available = available;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitCountInBounds(int bitCount)
    {
        if ((uint)_available < (uint)bitCount)
            return false;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitOffsetInBounds(int bitOffset)
        => IsBitOffsetInBounds(bitOffset, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsBitOffsetInBounds(int bitOffset, out int offset)
    {
        offset = bitOffset - _offset;
        if (bitOffset < _offset || (uint)offset > (uint)_available)
        {
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBitSpecInBounds(int bitOffset, int bitCount)
        => IsBitSpecInBounds(bitOffset, bitCount, out _, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsBitSpecInBounds(int bitOffset, int bitCount, out int offset, out int total)
    {
        offset = bitOffset - _offset;
        if (bitOffset < _offset)
        {
            total = 0;
            return false;
        }

        var temp = (ulong)(uint)offset + (ulong)(uint)bitCount;
        if (temp > (ulong)(uint)_available)
        {
            total = 0;
            return false;
        }

        total = (int)temp;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ExtractBoolLSB(int bitOffset)
    {
        if (!IsBitOffsetInBounds(bitOffset, out var offset))
            throw new ArgumentOutOfRangeException(nameof(bitOffset));

        return InternalExtractBoolLSB(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractBoolLSB(int bitOffset, out bool result)
    {
        if (!IsBitOffsetInBounds(bitOffset, out var offset))
        {
            result = false;
            return false;
        }

        result = InternalExtractBoolLSB(offset);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ExtractBoolLSB()
    {
        if (_available == 0)
            throw new ArgumentOutOfRangeException();

        return InternalExtractBoolLSB();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalExtractBoolLSB(int localOffset)
    {
        return ExtractionPrimitives.ExtractBoolLSB(_data, localOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalExtractBoolLSB()
    {
        return ExtractionPrimitives.ExtractBoolLSB(_data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ExtractBoolMSB(int bitOffset)
    {
        if (!IsBitOffsetInBounds(bitOffset, out var offset))
            throw new ArgumentOutOfRangeException(nameof(bitOffset));

        return InternalExtractBoolMSB(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ExtractBoolMSB()
    {
        if (_available == 0)
            throw new ArgumentOutOfRangeException();

        return InternalExtractBoolMSB();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalExtractBoolMSB(int localOffset)
    {
        return ExtractionPrimitives.ExtractBoolMSB(_data, localOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool InternalExtractBoolMSB()
    {
        return ExtractionPrimitives.ExtractBoolMSB(_data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractUnsignedLSB<T>(int bitOffset, int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitSpecInBounds(bitOffset, bitCount, out var offset, out var total))
        {
            value = default;
            return false;
        }

        value = InternalExtractUnsignedLSB<T>(offset, bitCount, total);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractUnsignedLSB<T>(int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitCountInBounds(bitCount))
        {
            value = default;
            return false;
        }

        value = InternalExtractUnsignedLSB<T>(bitCount);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractUnsignedLSB<T>(int localOffset, int bitCount, int total) where T : unmanaged
    {
        ulong result = ExtractionPrimitives.ExtractUInt64LSB(_data, localOffset, bitCount, total);

        return Unsafe.As<ulong, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractUnsignedLSB<T>(int bitCount) where T : unmanaged
    {
        ulong result = ExtractionPrimitives.ExtractUInt64LSB(_data, bitCount);

        return Unsafe.As<ulong, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractSignedLSB<T>(int bitOffset, int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitSpecInBounds(bitOffset, bitCount, out var offset, out var total))
        {
            value = default;
            return false;
        }

        value = InternalExtractSignedLSB<T>(offset, bitCount, total);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractSignedLSB<T>(int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitCountInBounds(bitCount))
        {
            value = default;
            return false;
        }

        value = InternalExtractSignedLSB<T>(bitCount);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractSignedLSB<T>(int localOffset, int bitCount, int total) where T : unmanaged
    {
        long result = ExtractionPrimitives.ExtractInt64LSB(_data, localOffset, bitCount, total);

        return Unsafe.As<long, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractSignedLSB<T>(int bitCount) where T : unmanaged
    {
        long result = ExtractionPrimitives.ExtractInt64LSB(_data, bitCount);

        return Unsafe.As<long, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryExtractUnsignedMSB<T>(int bitOffset, int bitCount, out T value) where T : unmanaged
    {
        // This should be elided by the JIT
        if (Unsafe.SizeOf<T>() > sizeof(ulong))
            throw new InvalidCastException();

        if (!IsBitSpecInBounds(bitOffset, bitCount, out var offset, out _))
        {
            value = default;
            return false;
        }

        value = InternalExtractUnsignedMSB<T>(offset, bitCount);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractUnsignedMSB<T>(int localOffset, int bitCount) where T : unmanaged
    {
        ulong result = ExtractionPrimitives.ExtractUInt64MSB(_data, localOffset, bitCount);

        return Unsafe.As<ulong, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractUnsignedMSB<T>(int bitCount) where T : unmanaged
    {
        ulong result = ExtractionPrimitives.ExtractUInt64MSB(_data, bitCount);

        return Unsafe.As<ulong, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractSignedMSB<T>(int localOffset, int bitCount) where T : unmanaged
    {
        long result = ExtractionPrimitives.ExtractInt64MSB(_data, localOffset, bitCount);

        return Unsafe.As<long, T>(ref result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T InternalExtractSignedMSB<T>(int bitCount) where T : unmanaged
    {
        long result = ExtractionPrimitives.ExtractInt64MSB(_data, bitCount);

        return Unsafe.As<long, T>(ref result);
    }

    public BitExtractor ShiftLSB(int shift)
    {
        if ((uint)shift > (uint)_available)
            shift = _available;

        return new BitExtractor(_data >> shift, _offset + shift, _available - shift);
    }

    public BitExtractor ShiftMSB(int shift)
    {
        if ((uint)shift > (uint)_available)
            shift = _available;

        return new BitExtractor(_data << shift, _offset + shift, _available - shift);
    }

}
