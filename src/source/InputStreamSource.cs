namespace necronomicon.source;

public class InputStreamSource
{
    private readonly BufferedStream bufferedStream;
    private readonly BinaryReader binaryReader;
    public InputStreamSource(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Unable to locate: {fileName}");
        }

        FileStream fs = File.OpenRead(fileName);

        bufferedStream = new BufferedStream(fs, 65536);
        binaryReader = new BinaryReader(bufferedStream, System.Text.Encoding.Default, leaveOpen: true);
    }

    public long GetFileSize()
    {
        return bufferedStream.Length;
    }

    public long GetPosition()
    {
        return bufferedStream.Position;
    }

    public void SetPosition(long newPosition)
    {
        if (bufferedStream.Position > newPosition)
        {
            throw new ArgumentException("Cannot rewind input stream.");
        }
        bufferedStream.Seek(newPosition - bufferedStream.Position, SeekOrigin.Current);
    }

    public void SkipBytes(int num)
    {
        bufferedStream.Seek(num, SeekOrigin.Current);
    }

    public byte[] ReadEngineHeader()
    {
        return binaryReader.ReadBytes(8);
    }

    public byte[] ReadBytes(int size)
    {
        return binaryReader.ReadBytes(size);
    }

    public int ReadFixedInt32()
    {
        return binaryReader.ReadInt32();
    }

    public uint ReadVarUInt32()
    {
        uint result = 0;
        int working, shift;
        shift = 0;

        while (true)
        {
            if (shift > 28)
                throw new InvalidDataException();

            working = binaryReader.ReadByte();

            unchecked
            {
                result |= (uint)(working & 0x7F) << shift;
            }

            if ((working & 0x80) != 0x80)
                break;

            shift += 7;
        }

        return result;
    }

    public int ReadVarInt32()
    {
            int result = 0;
            int working, shift;

            shift = 0;

            while (true)
            {
                if (shift > 28)
                    throw new InvalidDataException();

                working = binaryReader.ReadByte();

                unchecked
                {
                    result |= (working & 0x7F) << shift;
                }

                if ((working & 0x80) != 0x80)
                    break;

                shift += 7;
            }

            return result;
    }
}