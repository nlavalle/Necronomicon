using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BitWork.Primitives;

[Config(typeof(LocalConfig))]
public class ExtractionPrimitivesBenchmark
{
    private class LocalConfig : ManualConfig
    {
        public LocalConfig()
        {
            AddJob(Job.Dry.WithEnvironmentVariable("DOTNET_TC_QuickJit", "0"));
            AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(printInstructionAddresses: true)));
            Orderer = new DefaultOrderer(SummaryOrderPolicy.Declared, MethodOrderPolicy.Declared);
        }
    }

    private const int UIntBitCount = sizeof(uint) * 8;
    private const int ULongBitCount = sizeof(ulong) * 8;

    #region RorToShift

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Int32LSBRorToShift(uint source, int offset, int count)
    {
        // X86 (3/3 cycles):
        // SAR <- ROR <- ADD            (3/3)
        //   ^ <- NEG                   (2/-)
        //   ^ *imm                     (-/1)
        // A32 (4/4 cycles):
        // ASR <- ROR <- AND <- ADD     (4/4)
        //   ^ <- AND <- RSB            (3/-)
        //   ^ *imm                     (-/1)
        // A64 (3/3 cycles):
        // ASRV <- RORV <- ADD          (3/3)
        //    ^ <- NEG                  (2/-)
        //    ^ *imm                    (-/1)
        var lefted = unchecked((int)BitOperations.RotateRight(source, count + offset));
        return lefted >> -count;
    }

    [Benchmark]
    [BenchmarkCategory("Int32", "LSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public int Int32LSBRorToShiftVar(uint source, int offset, int count)
    {
        return Int32LSBRorToShift(source, offset, count);
    }

    [Benchmark]
    [BenchmarkCategory("Int32", "LSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public int Int32LSBRorToShiftConst(uint source, int offset)
    {
        return Int32LSBRorToShift(source, offset, 7);
    }

    #endregion

    #region ShiftToShift

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Int32LSBShiftToShift(uint source, int offset, int count)
    {
        // X86: (4/4 cycles)
        // SAR <- SHL <- SUB <- NEG         (4/-)
        // SAR <- SHL <- ADD <- NEG         (-/4)
        //   ^ *imm                         (-/1)
        // A32: (5/4 cycles)
        // ASR <- LSL <- AND <- SUB <- RSB  (5/-)
        // ASR <- LSL <- AND <- RSB         (-/4)
        //   ^ *imm                         (-/1)
        // A64: (4/4 cycles)
        // ASRV <- LSLV <- SUB <- NEG       (4/-)
        // ASR <- LSLV <- SUB <- MOV        (-/4)
        //   ^ *imm                         (-/1)
        var secondShift = -count;
        var firstShift = secondShift - offset;

        return unchecked((int)source) << firstShift >> secondShift;
    }

    [Benchmark]
    [BenchmarkCategory("Int32", "LSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public int Int32LSBShiftToShiftVar(uint source, int offset, int count)
    {
        return Int32LSBShiftToShift(source, offset, count);
    }

    [Benchmark]
    [BenchmarkCategory("Int32", "LSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public int Int32LSBShiftToShiftConst(uint source, int offset)
    {
        return Int32LSBShiftToShift(source, offset, 7);
    }

    #endregion

    #region Extract

    [Benchmark]
    [BenchmarkCategory("Int32", "LSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public int Int32LSBExtractVar(uint source, int offset, int count)
    {
        return ExtractionPrimitives.UncheckedExtractInt32LSB(source, offset, count, offset + count);
    }

    [Benchmark]
    [BenchmarkCategory("Int32", "LSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public int Int32LSBExtractConst(uint source, int offset)
    {
        return ExtractionPrimitives.UncheckedExtractInt32LSB(source, offset, 7, offset + 7);
    }

    #endregion



    #region ShiftToShift

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Int32MSBShiftToShift(uint source, int offset, int count)
    {
        // X86: (3/2 cycles)
        // SAR <- SHL           (2/2)
        //   ^ <- ADD <- NEG    (3/-)
        //   ^ *imm             (-/1)
        // ARM: (3/3 cycles)
        // ASR <- LSL <- AND    (2/2)
        //   ^ <- AND <- RSB    (2/-)
        //   ^ *imm             (-/1)
        var lefted = unchecked((int)source) << offset;

        var secondShift = -count;

        return lefted >> secondShift;
    }

    [Benchmark]
    [BenchmarkCategory("Int32", "MSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public int Int32MSBShiftToShiftVar(uint source, int offset, int count)
    {
        return Int32MSBShiftToShift(source, offset, count);
    }

    [Benchmark]
    [BenchmarkCategory("Int32", "MSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public int Int32MSBShiftToShiftConst(uint source, int offset)
    {
        return Int32MSBShiftToShift(source, offset, 7);
    }

    #endregion

    #region Extract

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ExtractInt32MSB(uint source, int offset, int count)
    {
        Debug.Assert(unchecked((ulong)(uint)offset + (ulong)(uint)count <= UIntBitCount));

        var lefted = unchecked((int)source) << offset;

        int secondShift = -count;

        return lefted >> secondShift;
    }

    [Benchmark]
    [BenchmarkCategory("Int32", "MSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public int Int32MSBExtractVar(uint source, int offset, int count)
    {
        return ExtractInt32MSB(source, offset, count);
    }

    [Benchmark]
    [BenchmarkCategory("Int32", "MSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public int Int32MSBExtractConst(uint source, int offset)
    {
        return ExtractInt32MSB(source, offset, 7);
    }

    #endregion


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint UInt32LSBShiftToBzhi(uint source, int offset, int count)
    {
        // s1 = -(count + offset) = -count + -offset = -count - offset
        // SHR <- SHL <- SUB <- NEG     (4/-)
        // SHR <- SHL <- ADD <- NEG     (-/4)
        if (Bmi2.IsSupported)
        {
            // SHRX -> BZHI is faster on BMI2 systems (2/2 cycles)
            // BZHI <- SHRX     (2/2)
            //    ^ <- MOV      (-/2)
            return Bmi2.ZeroHighBits(source >> offset, unchecked((uint)count));
        }

        return UInt32LSBShiftToAnd(source, offset, count);
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public uint UInt32LSBShiftToBzhiVar(uint source, int offset, int count)
    {
        return UInt32LSBShiftToBzhi(source, offset, count);
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public uint UInt32LSBShiftToBzhiConst(uint source, int offset)
    {
        return UInt32LSBShiftToBzhi(source, offset, 7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint UInt32LSBShiftToShift(uint source, int offset, int count)
    {
        // s1 = -(count + offset) = -count + -offset = -count - offset
        // SHR <- SHL <- SUB <- NEG     (4/-)
        // SHR <- SHL <- ADD <- NEG     (-/4)
        var secondShift = -count;

        var firstShift = secondShift - offset;

        return source << firstShift >> secondShift;
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public uint UInt32LSBShiftToShiftVar(uint source, int offset, int count)
    {
        return UInt32LSBShiftToShift(source, offset, count);
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public uint UInt32LSBShiftToShiftConst(uint source, int offset)
    {
        return UInt32LSBShiftToShift(source, offset, 7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint UInt32LSBRorToShift(uint source, int offset, int count)
    {
        // ROR -> SHR is faster/slower on X86 systems (3/3 cycles)
        // SHR <- ROR <- ADD        (3/3)
        //   ^ <- NEG               (2/-)
        //   ^ *imm                 (-/1)
        //var lefted = BitOperations.RotateRight(source, offset + count);

        //var secondShift = -count;

        return BitOperations.RotateRight(source, offset + count) >> -count;
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public uint UInt32LSBRorToShiftVar(uint source, int offset, int count)
    {
        return UInt32LSBRorToShift(source, offset, count);
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public uint UInt32LSBRorToShiftConst(uint source, int offset)
    {
        return UInt32LSBRorToShift(source, offset, 7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint UInt32LSBShiftToAnd(uint source, int offset, int count)
    {
        // X86: (4/2 cycles)
        // AND <- SHR                   (2/2)
        //   ^ <- DEC <- SHL <- MOV     (4/-)
        //   ^ *imm                     (-/1)
        // ARM: (3/2-3 cycles)
        // AND <- LSR                   (2/2)
        //   ^ <- SUB <- MOV            (3/-)
        //   ^ <- SUB <- MOV            (-/3)
        //   ^ *imm                     (-/1) where count <= 8
        // BFC <- LSR                   (-/2)
        var mask = (1U << count) - 1;

        var righted = source >> offset;

        return righted & mask;
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public uint UInt32LSBShiftToAndVar(uint source, int offset, int count)
    {
        return UInt32LSBShiftToAnd(source, offset, count);
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public uint UInt32LSBShiftToAndConst(uint source, int offset)
    {
        return UInt32LSBShiftToAnd(source, offset, 7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint UInt32LSBShiftToAndOpt(uint source, int offset, int count)
    {
        // X86: (4/2 cycles)
        // AND <- SHR                   (2/2)
        //   ^ <- DEC <- SHL <- MOV     (4/-)
        //   ^ *imm                     (-/1)
        // ARM: (3/2-3 cycles)
        // AND <- LSR                   (2/2)
        //   ^ <- SUB <- MOV            (3/-)
        //   ^ <- SUB <- MOV            (-/3)
        //   ^ *imm                     (-/1) where count <= 8
        // BFC <- LSR                   (-/2)
        var mask = ~0U >> -count;

        var righted = source >> offset;

        return righted & mask;
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public uint UInt32LSBShiftToAndOptVar(uint source, int offset, int count)
    {
        return UInt32LSBShiftToAndOpt(source, offset, count);
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public uint UInt32LSBShiftToAndOptConst(uint source, int offset)
    {
        return UInt32LSBShiftToAndOpt(source, offset, 7);
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Variable")]
    [Arguments(0x55555555U, 7, 7)]
    public uint UInt32LSBExtractVar(uint source, int offset, int count)
    {
        return ExtractionPrimitives.UncheckedExtractUInt32LSB(source, offset, count, offset + count);
    }

    [Benchmark]
    [BenchmarkCategory("UInt32", "LSB", "Constant")]
    [Arguments(0x55555555U, 7)]
    public uint UInt32LSBExtractConst(uint source, int offset)
    {
        return ExtractionPrimitives.UncheckedExtractUInt32LSB(source, offset, 7, offset + 7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ExtractUInt32MSB(uint source, int offset, int count)
    {
        Debug.Assert(unchecked((ulong)(uint)offset + (ulong)(uint)count <= UIntBitCount));

        // X86: (2/2 cycles)
        // SHR <- SHL           (2/2)
        //   ^ <- NEG           (2/-)
        //   ^ *imm             (-/1)
        // ARM: (3/2 cycles)
        // LSR <- LSL <- AND    (3/3)
        //   ^ <- AND <- RSB    (3/-)
        //   ^ *imm             (-/2)
        int secondShift = -count;

        return source << offset >> secondShift;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint InsertUInt32LSB(uint previous, uint value, int offset, int count)
    {
        Debug.Assert(unchecked((ulong)(uint)offset + (ulong)(uint)count <= UIntBitCount));

        uint unshiftedMask;

        // The bit count is likely to be a constant, offset is never:
        // algorithms should take this into account (variable/constant cycles)
#if !BZHI_MASK_DISABLE
        if (Bmi2.IsSupported)
        {
            // (5/4 cycles)
            // OR <- ANDN <- SHLX <- BZHI <- MOV    (5/-)
            // OR <- ANDN <- SHLX <- MOV            (-/4)
            //  ^ <- AND <-  *reg
            //         ^ <- SHLX                    (3/3)
            unshiftedMask = Bmi2.ZeroHighBits(unchecked((uint)-1), unchecked((uint)count));
        }
        else
#endif
        {
            // BMI1: (6/4 cycles), X86: (7/5 cycles), ARM: (4/4 cycles)
            // OR <- ANDN <- SHL <- DEC <- SHL <- MOV   (6/-)
            // OR <- ANDN <- SHL <- MOV                 (-/4)
            //  ^ <- AND <-  *reg
            //         ^ <- SHL                         (3/3)
            // OR <- AND <- NOT <- SHL <- DEC <- SHL <- MOV (7/-)
            // OR <- AND <- NOT <- SHL <- MOV               (-/5)
            //  ^ <- AND <-        *reg
            //         ^ <- SHL                             (3/3)
            unshiftedMask = (1U << count) - 1;
        }

        var mask = unshiftedMask << offset;
        var shiftedValue = value << offset;

        var masked = previous & ~mask;
        var insertion = shiftedValue & mask;

        return masked | insertion;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint InsertUInt32MSB(uint previous, uint value, int offset, int count)
    {
        Debug.Assert(unchecked((ulong)(uint)offset + (ulong)(uint)count <= UIntBitCount));


        // The bit count is likely to be a constant, offset is never:
        // algorithms should take this into account (variable/constant cycles)
        // BMI2: (5/4 cycles)
        // OR <- ANDN <- SHLX <- BZHI <- MOV    (5/-)
        // OR <- ANDN <- SHLX <- MOV            (-/4)
        //  ^ <- AND <-  *reg
        //         ^ <- SHLX                    (3/3)
        // BMI1: (6/4 cycles)
        // OR <- ANDN <- SHL <- DEC <- SHL <- MOV   (6/-)
        // OR <- ANDN <- SHL <- MOV                 (-/4)
        //  ^ <- AND <-  *reg
        //         ^ <- SHL                         (3/3)
        // X86: (7/5 cycles)
        // OR <- AND <- NOT <- SHL <- DEC <- SHL <- MOV (7/-)
        // OR <- AND <- NOT <- SHL <- MOV               (-/5)
        //  ^ <- AND <-        *reg
        //         ^ <- SHL                             (3/3)
        // ARM: (4/3-4 cycles)
        // OR <- BIC <- SUB <- MOV  (4/4)
        //  ^ <- AND <- *reg
        //         ^ <- LSL         (3/3)
        // OR <- BIC                (-/2) when count <= 8
        //  ^ <- AND <- LSL         (-/3)
        var unshiftedMask = GenerationPrimitives.GetPowerOfTwoMaskUInt32(count);

        var mask = unshiftedMask << offset;
        var shiftedValue = value << offset;

        var masked = previous & ~mask;
        var insertion = shiftedValue & mask;

        return masked | insertion;
    }

    [Benchmark]
    [Arguments(7)]
    public ulong TestMaskCreation(int shift)
    {
        return GenerationPrimitives.GetPowerOfTwoMaskUInt64(shift);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong InsertUInt64LSB(ulong previous, ulong value, int offset, int count)
    {
        Debug.Assert(unchecked((ulong)(uint)offset + (ulong)(uint)count <= UIntBitCount));

        ulong unshiftedMask = GenerationPrimitives.GetPowerOfTwoMaskUInt64(count);

        var mask = unshiftedMask << offset;
        var shiftedValue = value << offset;

        var masked = previous & ~mask;
        var insertion = shiftedValue & mask;

        return masked | insertion;
    }


}
