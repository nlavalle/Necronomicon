using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BitWork;

public ref struct AlignedLsBitReader
{
    private readonly ReadOnlyAlignedBitSpan _span;
    private ReadOnlyLsBitWindow _window;
    private long _offset;

#if DEBUG
    public readonly List<BitPositionDebugView> DebugInfo { get; } = new();
#endif

    public readonly long Offset => _offset;
    public readonly long Remaining => _span.Length - _offset;

    [Conditional("DEBUG")]
    public readonly void ResetDebugInfo()
    {
#if DEBUG
        DebugInfo.Clear();
#endif
    }

    [Conditional("DEBUG")]
    public readonly void CreateDebugInfo(BitPositionDebugView.InterpretationType type, long bitLength, string description)
    {
#if DEBUG
        DebugInfo.Add(_span.GenerateDebugInfo(type, _offset, bitLength, description));
#endif
    }

    [Conditional("DEBUG")]
    public readonly void AttachDebugNote(string note)
    {
#if DEBUG
        DebugInfo[DebugInfo.Count - 1].Notes.Add(note);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal AlignedLsBitReader(ReadOnlyAlignedBitSpan span, int offset)
    {
        _span = span;
        _window = default;
        _offset = offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AlignedLsBitReader Create(ReadOnlySpan<byte> buffer)
    {
        return new AlignedLsBitReader(ReadOnlyAlignedBitSpan.Create(buffer), 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdvance(uint bitCount)
    {
        var offset = _offset + bitCount;
        if ((ulong)offset > (ulong)_span.Length)
            return false;

        CreateDebugInfo(BitPositionDebugView.InterpretationType.LsBit, bitCount, "null[skip]");

        _offset = offset;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRevert(long offset)
    {
        if (!_span.IsBitOffsetInBounds(offset))
            return false;

        CreateDebugInfo(BitPositionDebugView.InterpretationType.LsBit, offset, "null[revert]");

        _offset = offset;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBool(out bool value)
    {
        if (!_window.TryExtractBool(_offset, out value))
        {
            if (!_span.IsBitOffsetInBounds(_offset))
                return false;

            var result = _span.ConcatenateUnsignedFarWindow<uint>(ref _window, _offset, 1);

            value = (result & 1U) != 0;
        }

        CreateDebugInfo(BitPositionDebugView.InterpretationType.LsBit, 1, "bool[read]");

        _offset++;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUnsigned<T>(int bitCount, out T value) where T : unmanaged
    {
        if (!_window.TryExtractUnsigned<T>(_offset, bitCount, out value))
        {
            if (!_span.IsBitSpecInBounds(_offset, bitCount))
                return false;

            value = _span.ConcatenateUnsignedFarWindow<T>(ref _window, _offset, bitCount);
        }

        CreateDebugInfo(BitPositionDebugView.InterpretationType.LsBit, bitCount, "unsigned[read]");

        _offset += (uint)bitCount;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadSigned<T>(int bitCount, out T value) where T : unmanaged
    {
        if (!_window.TryExtractSigned<T>(_offset, bitCount, out value))
        {
            if (!_span.IsBitSpecInBounds(_offset, bitCount))
                return false;

            value = _span.ConcatenateSignedFarWindow<T>(ref _window, _offset, bitCount);
        }

        CreateDebugInfo(BitPositionDebugView.InterpretationType.LsBit, bitCount, "signed[read]");

        _offset += (uint)bitCount;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeekBool(out bool value)
    {
        if (!_window.TryExtractBool(_offset, out value))
        {
            if (!_span.IsBitSpecInBounds(_offset, 1))
                return false;

            var result = _span.ConcatenateUnsignedNearWindow<uint>(ref _window, _offset, 1);

            value = (result & 1U) != 0;
        }

        CreateDebugInfo(BitPositionDebugView.InterpretationType.LsBit, 1, "bool[peek]");

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeekSigned<T>(int bitCount, out T value) where T : unmanaged
    {
        if (!_window.TryExtractSigned<T>(_offset, bitCount, out value))
        {
            if (!_span.IsBitSpecInBounds(_offset, bitCount))
                return false;

            value = _span.ConcatenateSignedNearWindow<T>(ref _window, _offset, bitCount);
        }

        CreateDebugInfo(BitPositionDebugView.InterpretationType.LsBit, bitCount, "unsigned[peek]");

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeekUnsigned<T>(int bitCount, out T value) where T : unmanaged
    {
        if (!_window.TryExtractUnsigned<T>(_offset, bitCount, out value))
        {
            if (!_span.IsBitSpecInBounds(_offset, bitCount))
                return false;

            value = _span.ConcatenateUnsignedNearWindow<T>(ref _window, _offset, bitCount);
        }

        CreateDebugInfo(BitPositionDebugView.InterpretationType.LsBit, bitCount, "signed[peek]");

        return true;
    }

    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     public bool TryGetByteAlignedBuffer(int byteCount, out ReadOnlySpan<byte> buffer)
    //     {
    //         return _span.TryGetByteAlignedBuffer(Offset, byteCount, out buffer);
    //     }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyTo(scoped Span<byte> buffer)
    {
        var bitCount = (long)(uint)buffer.Length << 3;
        if (!_span.IsBitSpecInBounds(_offset, bitCount))
            return false;

        _span.CopyBytesLSB(_offset, bitCount, buffer);

        CreateDebugInfo(BitPositionDebugView.InterpretationType.Byte, bitCount, "byte-copy[copy]");

        _offset += bitCount;

        return true;
    }

    public bool TryCopyTo(scoped Span<byte> buffer, int bitCount)
    {
        if (!_span.IsBitSpecInBounds(_offset, bitCount))
            return false;

        _span.CopyBytesLSB(_offset, bitCount, buffer);

        CreateDebugInfo(BitPositionDebugView.InterpretationType.Byte, bitCount, "bit-copy[copy]");

        _offset += bitCount;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyBytesUntilZero(scoped Span<byte> buffer, out int copied)
    {
        var offset = _offset;

        var result = _span.TryCopyBytesUntilZeroLSB(ref _window, offset, buffer, out var bitsCopied);
        if (result)
        {
            // Adjust for zero byte
            offset += 8U;
        }

        CreateDebugInfo(BitPositionDebugView.InterpretationType.Byte, bitsCopied + 8U, "until-zero[copy]");

        _offset = offset + bitsCopied;
        copied = (int)(bitsCopied >> 3);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetByteCountUntilZero()
    {
        return _span.GetByteCountUntilZeroLSB(in _window, _offset);
    }
}
