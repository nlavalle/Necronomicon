using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace BitWork;

public static class MemoryPrimitives
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong LocalizeEndiannessLSB(ulong value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            return BinaryPrimitives.ReverseEndianness(value);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong LocalizeEndiannessMSB(ulong value)
    {
        if (BitConverter.IsLittleEndian)
        {
            return BinaryPrimitives.ReverseEndianness(value);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint LocalizeEndiannessLSB(uint value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            return BinaryPrimitives.ReverseEndianness(value);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint LocalizeEndiannessMSB(uint value)
    {
        if (BitConverter.IsLittleEndian)
        {
            return BinaryPrimitives.ReverseEndianness(value);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort LocalizeEndiannessLSB(ushort value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            return BinaryPrimitives.ReverseEndianness(value);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort LocalizeEndiannessMSB(ushort value)
    {
        if (BitConverter.IsLittleEndian)
        {
            return BinaryPrimitives.ReverseEndianness(value);
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt64UncheckedLSB(ref byte reference, ulong value)
    {
        Unsafe.WriteUnaligned(ref reference, LocalizeEndiannessLSB(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt64UncheckedMSB(ref byte reference, ulong value)
    {
        Unsafe.WriteUnaligned(ref reference, LocalizeEndiannessMSB(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt32UncheckedLSB(ref byte reference, uint value)
    {
        Unsafe.WriteUnaligned(ref reference, LocalizeEndiannessLSB(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt32UncheckedMSB(ref byte reference, uint value)
    {
        Unsafe.WriteUnaligned(ref reference, LocalizeEndiannessMSB(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt16UncheckedLSB(ref byte reference, ushort value)
    {
        Unsafe.WriteUnaligned(ref reference, LocalizeEndiannessLSB(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt16UncheckedMSB(ref byte reference, ushort value)
    {
        Unsafe.WriteUnaligned(ref reference, LocalizeEndiannessMSB(value));
    }

    internal static ulong JoinDualShiftLSB(ulong lower, ulong upper, int alignment)
    {
        return LowerShiftLSB(lower, alignment) | UpperShiftLSB(upper, -alignment);
    }

    internal static ulong LowerShiftLSB(ulong lower, int alignment)
    {
        return lower >> alignment;
    }

    internal static ulong UpperShiftLSB(ulong upper, int invertedAlignment)
    {
        return upper << invertedAlignment;
    }
}
