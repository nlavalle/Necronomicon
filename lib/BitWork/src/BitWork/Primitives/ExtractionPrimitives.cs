using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace BitWork.Primitives;

// The bit count is likely to be a constant, offset is never:
// algorithms should take this into account (variable/constant cycles)
public static class ExtractionPrimitives
{
    #region INT LSB

    // checked 32 bits

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ExtractInt32LSB(uint source, int bitCount)
    {
        if (bitCount == 0)
            return 0;
        
        return UncheckedExtractInt32LSB(source, bitCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ExtractInt32LSB(int source, int bitCount)
        => ExtractInt32LSB((uint)source, bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ExtractInt32LSB(uint source, int bitOffset, int bitCount)
        => ExtractInt32LSB(source, bitOffset, bitCount, bitOffset + bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ExtractInt32LSB(int source, int bitOffset, int bitCount)
        => ExtractInt32LSB((uint)source, bitOffset, bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ExtractInt32LSB(uint source, int bitOffset, int bitCount, int bitTotal)
    {
        Debug.Assert((bitTotal & 63) == (bitOffset + bitCount & 63));

        if (bitCount == 0)
            return 0;

        return UncheckedExtractInt32LSB(source, bitOffset, bitCount, bitTotal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ExtractInt32LSB(int source, int bitOffset, int bitCount, int bitTotal)
        => ExtractInt32LSB((uint)source, bitOffset, bitCount, bitTotal);

    // unchecked 32 bits

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UncheckedExtractInt32LSB(uint source, int bitOffset, int bitCount, int bitTotal)
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            // ROR to SAR is faster on architectures known to have ROR
            case Architecture.X86:
            case Architecture.X64:
            case Architecture.Arm:
            case Architecture.Arm64:
            case Architecture.Wasm:
                // X86: (3/3 cycles)
                // SAR <- ROR <- ADD        (3/3)
                //   ^ <- NEG               (2/-)
                //   ^ *imm                 (-/1)
                // A32: (4/4 cycles)
                // ASR <- ROR <- AND <- ADD   (4/4)
                //   ^ <- AND <- RSB          (3/-)
                //   ^ *imm                   (-/1)
                // A64: (3/3 cycles)
                // ASRV <- RORV <- ADD        (3/-)
                //    ^ <- NEG                (2/-)
                // ASR <- RORV <- ADD         (-/3)
                //   ^ *imm                   (-/1)
                // NOTE: ADD is required for pre-call safety check, so this is preferred
                return (int)BitOperations.RotateRight(source, bitTotal) >> -bitCount;
            default:
                // X86: (4/4 cycles)
                // SAR <- SHL <- NEG <- ADD     (4/4)
                //   ^ <- NEG                   (2/-)
                //   ^ *imm                     (-/1)
                // A32: (5/5 cycles)
                // ASR <- LSL <- AND <- RSB <- ADD  (5/5)
                //   ^ <- AND <- RSB                (3/-)
                //   ^ *imm                         (-/1)
                // A64: (4/4 cycles)
                // ASRV <- LSLV <- NEG <- ADD   (4/-)
                //    ^ <- NEG                  (2/-)
                // ASR <- LSLV <- NEG <- ADD    (-/4)
                //   ^ *imm                     (-/1)
                return (int)source << -bitTotal >> -bitCount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UncheckedExtractInt32LSB(int source, int bitOffset, int bitCount, int bitTotal)
        => UncheckedExtractInt32LSB((uint)source, bitOffset, bitCount, bitTotal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UncheckedExtractInt32LSB(uint source, int bitCount)
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            // ROR to SAR is faster on architectures known to have ROR
            case Architecture.X86:
            case Architecture.X64:
            case Architecture.Arm:
            case Architecture.Arm64:
            case Architecture.Wasm:
                // X86: (2/2 cycles)
                // SAR <- ROR               (2/2)
                //   ^ <- NEG               (2/-)
                //   ^ *imm                 (-/1)
                // A32: (3/3 cycles)
                // ASR <- ROR <- AND        (3/3)
                //   ^ <- AND <- NEG        (3/-)
                //   ^ *imm                 (-/1)
                // A64: (2/2 cycles)
                // ASRV <- RORV             (2/-)
                //    ^ <- NEG              (2/-)
                // ASR <- RORV              (-/2)
                //   ^ *imm                 (-/1)
                return (int)BitOperations.RotateRight(source, bitCount) >> -bitCount;
            default:
                // X86: (3/2 cycles)
                // SAR <- SHL <- NEG    (3/-)
                // SAR <- SHL           (-/2)
                //   ^ *imm             (-/1)
                // A32: (4/1 cycles)
                // ASR <- LSL <- AND <- NEG     (4/-)
                // SBFX                         (-/1)
                // A64: (3/1 cycles)
                // ASRV <- LSLV <- NEG  (3/-)
                // SBFX                 (-/1)
                return (int)source << -bitCount >> -bitCount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int UncheckedExtractInt32LSB(int source, int bitCount)
        => UncheckedExtractInt32LSB((uint)source, bitCount);

    // checked 64 bits

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ExtractInt64LSB(ulong source, int bitCount)
    {
        if (bitCount == 0)
            return 0;

        return UncheckedExtractInt64LSB(source, bitCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ExtractInt64LSB(long source, int bitCount)
        => ExtractInt64LSB((ulong)source, bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ExtractInt64LSB(ulong source, int bitOffset, int bitCount)
        => ExtractInt64LSB(source, bitOffset, bitCount, bitOffset + bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ExtractInt64LSB(long source, int bitOffset, int bitCount)
        => ExtractInt64LSB((ulong)source, bitOffset, bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ExtractInt64LSB(ulong source, int bitOffset, int bitCount, int bitTotal)
    {
        Debug.Assert((bitTotal & 63) == (bitOffset + bitCount & 63));

        if (bitCount == 0)
            return 0;

        return UncheckedExtractInt64LSB(source, bitOffset, bitCount, bitTotal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ExtractInt64LSB(long source, int bitOffset, int bitCount, int bitTotal)
        => ExtractInt64LSB((ulong)source, bitOffset, bitCount, bitTotal);

    // unchecked 64 bits

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long UncheckedExtractInt64LSB(ulong source, int bitOffset, int bitCount, int bitTotal)
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
            case Architecture.Arm64:
            case Architecture.Wasm:
                return (long)BitOperations.RotateRight(source, bitTotal) >> -bitCount;
            default:
                return (long)source << -bitTotal >> -bitCount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long UncheckedExtractInt64LSB(long source, int bitOffset, int bitCount, int bitTotal)
        => UncheckedExtractInt64LSB((ulong)source, bitOffset, bitCount, bitTotal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long UncheckedExtractInt64LSB(ulong source, int bitCount)
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X64:
            case Architecture.Arm64:
            case Architecture.Wasm:
                return (long)BitOperations.RotateRight(source, bitCount) >> -bitCount;
            default:
                var shift = -bitCount;
                return (long)source << shift >> shift;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long UncheckedExtractInt64LSB(long source, int bitCount)
        => UncheckedExtractInt64LSB((ulong)source, bitCount);

    #endregion

    #region UINT LSB

    // checked 32 bits

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ExtractUInt32LSB(uint source, int bitCount)
    {
        if (bitCount == 0)
            return 0;

        return UncheckedExtractUInt32LSB(source, bitCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ExtractUInt32LSB(int source, int bitCount)
        => ExtractUInt32LSB((uint)source, bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ExtractUInt32LSB(uint source, int bitOffset, int bitCount)
        => ExtractUInt32LSB(source, bitOffset, bitCount, bitOffset + bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ExtractUInt32LSB(int source, int bitOffset, int bitCount)
        => ExtractUInt32LSB((uint)source, bitOffset, bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ExtractUInt32LSB(uint source, int bitOffset, int bitCount, int bitTotal)
    {
        if (bitCount == 0)
            return 0;

        return UncheckedExtractUInt32LSB(source, bitOffset, bitCount, bitTotal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ExtractUInt32LSB(int source, int bitOffset, int bitCount, int bitTotal)
        => ExtractUInt32LSB((uint)source, bitOffset, bitCount, bitTotal);

    // unchecked 32 bits

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint UncheckedExtractUInt32LSB(uint source, int bitOffset, int bitCount, int bitTotal)
    {
        if (Bmi2.IsSupported)
        {
            // SHRX -> BZHI is faster on BMI2 systems (2/2 cycles)
            // BZHI <- SHRX     (2/2)
            //    ^ <- MOV      (-/2)
            return Bmi2.ZeroHighBits(source >> bitOffset, unchecked((uint)bitCount));
        }
        else
        {
#if !ROR_TO_SHR_UINT_DISABLED
            switch (RuntimeInformation.ProcessArchitecture)
            {
                // ROR to SHR is faster on architectures known to have ROR
                case Architecture.X86:
                case Architecture.X64:
                case Architecture.Arm:
                case Architecture.Arm64:
                case Architecture.Wasm:
                    // ROR -> SHR is faster/slower on X86 systems (3/3 cycles)
                    // X86: (3/3 cycles)
                    // SHR <- ROR <- ADD        (3/3)
                    //   ^ <- NEG               (2/-)
                    //   ^ *imm                 (-/1)
                    // A32: (4/4 cycles)
                    // LSR <- ROR <- AND <- ADD   (4/4)
                    //   ^ <- AND <- NEG          (3/-)
                    //   ^ *imm                   (-/1)
                    // A64: (3/3 cycles)
                    // LSRV <- RORV <- ADD        (3/-)
                    //    ^ <- NEG                (2/-)
                    // LSR <- RORV <- ADD         (-/3)
                    //   ^ *imm                   (-/1)
                    // NOTE: ADD is required for pre-call safety check, so this is preferred
                    return BitOperations.RotateRight(source, bitTotal) >> -bitCount;
                default:
                    break;
            }
#endif

            // Other platforms are generally faster with default "extraction" code
            // X86: (3-4/2 cycles)
            // AND <- SHR                   (2/2)
            //   ^ <- DEC <- SHL <- MOV     (4/-) literal
            //   ^ <- SHR <- MOV            (3/-) optimized
            //          ^ <- NEG            (2/-)
            //   ^ *imm                     (-/1)
            // A32: (3/3 cycles)
            // AND <- LSR <- AND            (3/-) literal
            //   ^ <- SUB <- MOV            (3/-)
            //          ^ <- AND            (2/-)
            // BIC <- LSR <- AND            (3/-) optimized
            //   ^ <- MVN                   (2/-)
            //   ^ <- AND                   (2/-)
            // UBFX <- LSR <- AND           (-/3)
            // A64: (3-4/2 cycles)
            // AND <- LSRV                  (2/-) literal
            //   ^ <- SUB <- LSLV <- MOV    (4/-)
            // BIC <- LSRV                  (2/-) optimized
            //   ^ <- LSLV <- MOVN          (3/-)
            // UBFX <- LSR                  (-/2)
            return (source >> bitOffset) & GenerationPrimitives.GetPowerOfTwoMaskUInt32(bitCount);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint UncheckedExtractUInt32LSB(int source, int bitOffset, int bitCount, int bitTotal)
        => UncheckedExtractUInt32LSB((uint)source, bitOffset, bitCount, bitTotal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint UncheckedExtractUInt32LSB(uint source, int bitCount)
    {
        if (Bmi2.IsSupported)
        {
            // BZHI is faster on BMI2 systems (1/2 cycles)
            // BZHI             (1/-)
            // BZHI <- MOV      (-/2)
            return Bmi2.ZeroHighBits(source, unchecked((uint)bitCount));
        }
        else
        {
#if !ROR_TO_SHR_UINT_DISABLED
            switch (RuntimeInformation.ProcessArchitecture)
            {
                // ROR to SHR is faster on architectures known to have ROR
                case Architecture.X86:
                case Architecture.X64:
                case Architecture.Arm64:
                case Architecture.Wasm:
                    // X86: (2/2 cycles)
                    // SHR <- ROR                 (2/2)
                    //   ^ <- NEG                 (2/-)
                    //   ^ *imm                   (-/1)
                    // A32: (3/3 cycles) UNUSED
                    // LSR <- ROR <- AND          (3/3)
                    //   ^ <- AND <- NEG          (3/-)
                    //   ^ *imm                   (-/1)
                    // A64: (2/2 cycles)
                    // LSRV <- RORV               (2/-)
                    //    ^ <- NEG                (2/-)
                    // LSR <- RORV                (-/2)
                    //   ^ *imm                   (-/1)
                    return BitOperations.RotateRight(source, bitCount) >> -bitCount;
                default:
                    break;
            }
#endif

            // Other platforms are generally faster with default "extraction" code
            // X86: (3-4/1 cycles)
            // AND <- DEC <- SHL <- MOV     (4/-) literal
            // AND <- SHR <- MOV            (3/-) optimized
            //          ^ <- NEG            (2/-)
            //   ^ *imm                     (-/1)
            // A32: (2-3/1 cycles)
            // AND <- SUB <- MOV            (3/-) literal
            //          ^ <- AND            (2/-)
            // AND <- MOV                   (2/-) optimized
            //   ^ <- AND                   (2/-)
            // UBFX                         (-/1)
            // A64: (3-4/1 cycles)
            // AND <- SUB <- LSLV <- MOV    (4/-) literal
            // BIC <- LSRV                  (2/-) optimized
            //   ^ <- LSLV <- MOVN          (3/-)
            // UBFX                         (-/1)
            return source & GenerationPrimitives.GetPowerOfTwoMaskUInt32(bitCount);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint UncheckedExtractUInt32LSB(int source, int bitCount)
        => UncheckedExtractUInt32LSB((uint)source, bitCount);

    // checked 64 bits

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ExtractUInt64LSB(ulong source, int bitCount)
    {
        if (bitCount == 0)
            return 0;

        return UncheckedExtractUInt64LSB(source, bitCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ExtractUInt64LSB(long source, int bitCount)
        => ExtractUInt64LSB((ulong)source, bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ExtractUInt64LSB(ulong source, int bitOffset, int bitCount)
        => ExtractUInt64LSB(source, bitOffset, bitCount, bitOffset + bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ExtractUInt64LSB(long source, int bitOffset, int bitCount)
        => ExtractUInt64LSB((ulong)source, bitOffset, bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ExtractUInt64LSB(ulong source, int bitOffset, int bitCount, int bitTotal)
    {
        if (bitCount == 0)
            return 0;

        return UncheckedExtractUInt64LSB(source, bitOffset, bitCount, bitTotal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ExtractUInt64LSB(long source, int bitOffset, int bitCount, int bitTotal)
        => ExtractUInt64LSB((ulong)source, bitOffset, bitCount, bitTotal);

    // unchecked 64 bits

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong UncheckedExtractUInt64LSB(ulong source, int bitOffset, int bitCount, int bitTotal)
    {
        if (Bmi2.X64.IsSupported)
        {
            return Bmi2.X64.ZeroHighBits(source >> bitOffset, unchecked((uint)bitCount));
        }
        else
        {
#if !ROR_TO_SHR_UINT_DISABLED
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X64:
                case Architecture.Arm64:
                case Architecture.Wasm:
                    return BitOperations.RotateRight(source, bitTotal) >> -bitCount;
                default:
                    break;
            }
#endif

            return (source >> bitOffset) & GenerationPrimitives.GetPowerOfTwoMaskUInt64(bitCount);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong UncheckedExtractUInt64LSB(long source, int bitOffset, int bitCount, int bitTotal)
        => UncheckedExtractUInt64LSB((ulong)source, bitOffset, bitCount, bitTotal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong UncheckedExtractUInt64LSB(ulong source, int bitCount)
    {
        if (Bmi2.X64.IsSupported)
        {
            return Bmi2.X64.ZeroHighBits(source, unchecked((uint)bitCount));
        }
        else
        {
#if !ROR_TO_SHR_UINT_DISABLED
            switch (RuntimeInformation.ProcessArchitecture)
            {
                // ROR to SHR is faster on architectures known to have ROR
                case Architecture.X64:
                case Architecture.Arm64:
                case Architecture.Wasm:
                    return BitOperations.RotateRight(source, bitCount) >> -bitCount;
                default:
                    break;
            }
#endif

            return source & GenerationPrimitives.GetPowerOfTwoMaskUInt64(bitCount);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong UncheckedExtractUInt64LSB(long source, int bitCount)
        => UncheckedExtractUInt64LSB((ulong)source, bitCount);

    #endregion

    #region BOOL LSB

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractBoolLSB(byte source, int offset)
    {
        // X86: (2/1 cycles)
        // BT <- AND        (2/-)
        // BT               (-/1)
        // A32: (2/1 cycles)
        // TST <- AND       (2/-)
        //   ^ <- MOV       (2/-)
        // TST              (-/1)
        // A64: (2/1 cycles)
        // TST <- LSRV      (2/-)
        // TST              (-/1)
        return (source & (1U << (offset & 7))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractBoolLSB(byte source)
    {
        return (source & 1U) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractBoolLSB(uint source, int offset)
    {
        // X86: (1/1 cycles)
        // BT               (1/1)
        // A32: (2/1 cycles)
        // TST <- AND       (2/-)
        //   ^ <- MOV       (2/-)
        // TST              (-/1)
        // A64: (2/1 cycles)
        // TST <- LSRV      (2/-)
        // TST              (-/1)
        return (source & (1U << offset)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractBoolLSB(int source, int offset)
        => ExtractBoolLSB((uint)source, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractBoolLSB(uint source)
    {
        // X86: (1 cycles)
        // BT
        // ARM: (1 cycles)
        // TST
        return (source & 1U) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractBoolLSB(int source)
        => ExtractBoolLSB((uint)source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractBoolLSB(ulong source, int offset)
    {
        // X86: (1/1 cycles)
        // BT               (1/1)
        // A64: (2/1 cycles)
        // TST <- LSRV      (2/-)
        // TST              (-/1)
        return (source & (1UL << offset)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractBoolLSB(long source, int offset)
        => ExtractBoolLSB((ulong)source, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractBoolLSB(ulong source)
    {
        // X86: (1 cycles)
        // BT
        // A64: (1 cycles)
        // TST
        return (source & 1UL) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ExtractBoolLSB(long source)
        => ExtractBoolLSB((ulong)source);

    #endregion


}
