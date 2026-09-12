using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace BitWork.Primitives;

public static class GenerationPrimitives
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetPowerOfTwoMaskUInt32(int shift)
    {
#if !BZHI_MASK_DISABLE
        if (Bmi2.IsSupported)
        {
            return unchecked(Bmi2.ZeroHighBits(~0U, (uint)shift));
        }
#endif

        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
            case Architecture.X64:
            case Architecture.Arm:
            case Architecture.Arm64:
            case Architecture.Wasm:
                return ~0U >> -shift;
            default:
                return (1U << shift) - 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetPowerOfTwoMaskUInt64(int shift)
    {
#if !BZHI_MASK_DISABLE
        if (Bmi2.X64.IsSupported)
        {
            return unchecked(Bmi2.X64.ZeroHighBits(~0U, (uint)shift));
        }
#endif

        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
            case Architecture.X64:
            case Architecture.Arm:
            case Architecture.Arm64:
            case Architecture.Wasm:
                return ~0UL >> -shift;
            default:
                return (1UL << shift) - 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPowerOfTwoMaskInt32(int shift)
        => unchecked((int)GetPowerOfTwoMaskUInt32(shift));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetPowerOfTwoMaskInt64(int shift)
        => unchecked((long)GetPowerOfTwoMaskUInt64(shift));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetReferenceByteAlignment(scoped ref readonly byte ptr)
    {
        return CalculateMisalignment(in ptr, sizeof(ulong));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint CalculateAddress<T>(scoped ref readonly T ptr)
    {
        unsafe
        {
            return (nuint)Unsafe.AsPointer(in ptr);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint CalculateMisalignment<T>(scoped ref readonly T ptr, nuint alignment)
    {
        if (!nuint.IsPow2(alignment))
            throw new ArgumentOutOfRangeException(nameof(alignment), "Argument must be power of 2.");

        return CalculateAddress(in ptr) & (alignment - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CalculateMisalignment<T>(scoped ref readonly T ptr, uint alignment)
    {
        if (!uint.IsPow2(alignment))
            throw new ArgumentOutOfRangeException(nameof(alignment), "Argument must be power of 2.");

        return (uint)CalculateAddress(in ptr) & (alignment - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint CalculateAlignment<T>(scoped ref readonly T ptr)
    {
        var address = unchecked((nint)CalculateAddress(in ptr));
        return unchecked((nuint)(address & -address));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int TzcLowestZeroHighestBit(ulong value)
    {
        var zero = (value - 0x0101_0101_0101_0101UL) & (~value & 0x8080_8080_8080_8080UL);
        return BitOperations.TrailingZeroCount(zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int TzcLowestZeroHighestBit(uint value)
    {
        var zero = (value - 0x0101_0101U) & (~value & 0x8080_8080U);
        return BitOperations.TrailingZeroCount(zero);
    }

    public static bool TryFindLowestZeroByte(ulong value, int length, out int position)
    {
        var tzc = TzcLowestZeroHighestBit(value);
        position = tzc >> 3;
        return tzc < length;
    }
}
