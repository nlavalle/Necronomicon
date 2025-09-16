using System.Buffers;
using System.Buffers.Binary;

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
