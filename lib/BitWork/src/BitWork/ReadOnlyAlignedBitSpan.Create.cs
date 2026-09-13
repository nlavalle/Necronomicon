using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BitWork.Primitives;

namespace BitWork;

public readonly ref partial struct ReadOnlyAlignedBitSpan
{
    public static ReadOnlyAlignedBitSpan Create(ReadOnlySpan<byte> span)
    {
        var spanBitLength = (long)(uint)span.Length << 3;

        ref var reference = ref AlignReferenceDown(ref MemoryMarshal.GetReference(span), out var alignment);

        return new(ref reference, alignment, spanBitLength);
    }

    public static ReadOnlyAlignedBitSpan Create(ReadOnlySpan<byte> span, long bitLength)
    {
        var spanBitLength = (ulong)(uint)span.Length << 3;

        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)bitLength, spanBitLength, nameof(bitLength));

        ref var reference = ref AlignReferenceDown(ref MemoryMarshal.GetReference(span), out var alignment);

        return new(ref reference, alignment, bitLength);
    }

    public static ReadOnlyAlignedBitSpan Create(ReadOnlySpan<byte> span, long bitStart, long bitLength)
    {
        var spanBitLength = (ulong)(uint)span.Length << 3;

        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)bitStart, spanBitLength, nameof(bitStart));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)bitLength, spanBitLength - (ulong)bitStart, nameof(bitLength));

        ref var longReference = ref AlignReferenceDown(ref MemoryMarshal.GetReference(span), out var byteAlignment);
        bitStart += byteAlignment;

        var alignment = (uint)bitStart & 63U;
        ref var reference = ref Unsafe.Add(ref longReference, (nuint)(bitStart >> 6));

        return new(ref reference, alignment, bitLength);
    }

    public static ReadOnlyAlignedBitSpan Create(ReadOnlySpan<ulong> span)
    {
        var spanBitLength = (long)(uint)span.Length << 6;

        ref var reference = ref MemoryMarshal.GetReference(span);

        return new(ref reference, 0, spanBitLength);
    }

    public static ReadOnlyAlignedBitSpan Create(ReadOnlySpan<ulong> span, long bitLength)
    {
        var spanBitLength = (ulong)(uint)span.Length << 6;

        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)bitLength, spanBitLength, nameof(bitLength));

        ref var reference = ref MemoryMarshal.GetReference(span);

        return new(ref reference, 0, bitLength);
    }

    public static ReadOnlyAlignedBitSpan Create(ReadOnlySpan<ulong> span, long bitStart, long bitLength)
    {
        var spanBitLength = (ulong)(uint)span.Length << 6;

        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)bitStart, spanBitLength, nameof(bitStart));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)bitLength, spanBitLength - (ulong)bitStart, nameof(bitLength));

        var alignment = (uint)bitStart & 63U;
        ref var reference = ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nuint)(bitStart >> 6));

        return new(ref reference, alignment, bitLength);
    }

    private static ref ulong AlignReferenceDown(ref byte reference, out uint bitAlignment)
    {
        var byteAlignment = GenerationPrimitives.GetReferenceByteAlignment(in reference);
        bitAlignment = byteAlignment << 3;
        return ref Unsafe.As<byte, ulong>(ref Unsafe.Subtract(ref reference, byteAlignment));
    }
}
