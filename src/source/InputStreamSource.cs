using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.Intrinsics.X86;

namespace necronomicon.source;

public class InputStreamSource : IDisposable
{
    private const int BufferSize = 65536;

    private readonly BufferedStream _bufferedStream;

    public InputStreamSource(string fileName)
        : this(File.OpenRead(fileName))
    { }

    public InputStreamSource(Stream stream)
    {
        _bufferedStream = new BufferedStream(stream, BufferSize);
    }

    public long GetFileSize()
    {
        return _bufferedStream.Length;
    }

    public long GetPosition()
    {
        return _bufferedStream.Position;
    }

    public void SetPosition(long newPosition)
    {
        var currentPosition = _bufferedStream.Position;
        if (currentPosition > newPosition)
        {
            throw new ArgumentException("Cannot rewind input stream.");
        }
        else if (currentPosition != newPosition)
        {
            _bufferedStream.Seek(newPosition, SeekOrigin.Begin);
        }
    }

    public void SkipBytes(int num)
    {
        _bufferedStream.Seek(num, SeekOrigin.Current);
    }

    public void ReadBytes(Span<byte> span)
    {
        if (_bufferedStream.Read(span) != span.Length)
            throw new InvalidDataException();
    }

    public int ReadFixedInt32()
    {
        const int SizeOf = sizeof(int);
        Span<byte> buffer = stackalloc byte[SizeOf];

        ReadBytes(buffer);

        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    public uint ReadVarUInt32() => unchecked((uint)ReadVarInt32());

    public int ReadVarInt32()
    {
        int result = 0;
        int working, shift;
        shift = 0;

        while (true)
        {
            if (shift > 28)
                throw new InvalidDataException();

            working = _bufferedStream.ReadByte();
            if (working < 0)
                throw new EndOfStreamException();

            result |= (working & 0x7F) << shift;

            if ((working & 0x80) == 0)
                break;

            shift += 7;
        }

        return result;
    }

    public void Dispose()
    {
        _bufferedStream.Dispose();
    }

    public static bool TryReadVarInt32(ref ReadOnlySpan<byte> buffer, out int value)
    {
        int result = 0;
        int shift = 0;
        int index = 0;

        while (index < buffer.Length)
        {
            if (shift > 28)
                break;

            int working = buffer[index++];

            result |= (working & 0x7F) << shift;

            if ((working & 0x80) == 0L)
            {
                buffer = buffer.Slice(index);
                value = result;
                return true;
            }

            shift += 7;
        }

        value = 0;
        return false;
    }

    public static bool TryReadVarInt32Alternate0(ulong buffer, out int value)
    {
        const ulong FlagMask = 0b1000_0000_1000_0000_1000_0000_1000_0000_1000_0000UL;
        const uint ValueMask = 0b0111_1111U;

        ulong flags = ~buffer & FlagMask;

        int ctz = BitOperations.TrailingZeroCount(flags);

        uint result = (uint)buffer & ValueMask;

        switch (ctz)
        {
            case 8 * 1 - 1:
                goto SHIFT1;
            case 8 * 2 - 1:
                goto SHIFT2;
            case 8 * 3 - 1:
                goto SHIFT3;
            case 8 * 4 - 1:
                goto SHIFT4;
            case 8 * 5 - 1:
                goto SHIFT5;
            default:
                value = 0;
                return false;
        }

    SHIFT5:
        result |= (uint)(buffer >> 4) & (ValueMask << (7 * 4));

    SHIFT4:
        result |= (uint)(buffer >> 3) & (ValueMask << (7 * 3));

    SHIFT3:
        result |= (uint)(buffer >> 2) & (ValueMask << (7 * 2));

    SHIFT2:
        result |= (uint)(buffer >> 1) & (ValueMask << (7 * 1));

    SHIFT1:
        value = (int)result;
        return true;
    }

    public static bool TryReadVarInt32Alternate1(ulong buffer, out int value)
    {
        // MOV (1)
        const ulong FlagMask = 0b1000_0000_1000_0000_1000_0000_1000_0000_1000_0000UL;
        const uint ValueMask = 0b0111_1111U;

        // ANDN (2)
        ulong flags = ~buffer & FlagMask;

        // TZCNT (3)
        int ctz = BitOperations.TrailingZeroCount(flags);

        // AND (1)
        uint result = (uint)buffer & ValueMask;

        // TEST+JE (4)
        if (ctz == 8 * 1 - 1)
            goto DONE;

        // OR <- AND <- SHR (3-4*)
        result |= (uint)(buffer >> 1) & (ValueMask << (7 * 1));

        // TEST+JE (4-5)
        if (ctz == 8 * 2 - 1)
            goto DONE;
            
        // OR <- AND <- SHR (4-5*)
        result |= (uint)(buffer >> 2) & (ValueMask << (7 * 2));

        // TEST+JE (5-6)
        if (ctz == 8 * 3 - 1)
            goto DONE;

        // OR <- AND <- SHR (5-6*)
        result |= (uint)(buffer >> 3) & (ValueMask << (7 * 3));

        // TEST+JE (5-7)
        if (ctz != 8 * 4 - 1)
        {
            value = 0;
            return false;
        }

        // OR <- AND <- SHR (6-7*)
        result |= (uint)(buffer >> 4) & (ValueMask << (7 * 4));

    DONE:
        // 4+J, 5+J, 6+J, 7+J, 7
        value = (int)result;
        return true;
    }

    public static bool TryReadVarInt32Alternate2(ulong buffer, out int value)
    {
        if (Bmi2.X64.IsSupported)
        {
            // MOV (1)
            const ulong FlagMask = 0b1000_0000_1000_0000_1000_0000_1000_0000_1000_0000UL;

            // ANDN (2)
            ulong flags = ~buffer & FlagMask;

            // TZCNT (3)
            int ctz = BitOperations.TrailingZeroCount(flags);

            // CMP+JLE (4)
            if (ctz <= 31)
            {
                // NOT -> BZHI (5)
                var mask = Bmi2.X64.ZeroHighBits(~FlagMask, (uint)ctz);

                // PEXT (6+)
                value = (int)Bmi2.X64.ParallelBitExtract(buffer, mask);
                return true;
            }
            else
            {
                value = 0;
                return false;
            }
        }
        else
        {
            return TryReadVarInt32Alternate1(buffer, out value);
        }

    }

    public static bool TryReadVarInt32(ReadOnlySequence<byte> buffer, ref SequencePosition position, out int value)
    {
        int result = 0;
        int shift = 0;

        var current = position;
        var next = current;

        while (buffer.TryGet(ref next, out var memory))
        {
            var span = memory.Span;
            int index = 0;

            while (index < span.Length)
            {
                if (shift > 28)
                    goto Exit;

                int working = span[index++];

                result |= (working & 0x7F) << shift;

                if ((working & 0x80) == 0)
                {
                    position = buffer.GetPosition(index, current);
                    value = result;
                    return true;
                }

                shift += 7;
            }

            current = next;
        }

    Exit:
        value = 0;
        return false;
    }

    public static bool TryReadVarInt32(ref ReadOnlySequence<byte> buffer, out int value)
    {
        var position = buffer.Start;

        if (TryReadVarInt32(buffer, ref position, out value))
        {
            buffer = buffer.Slice(position);
            return true;
        }

        return false;
    }
}
