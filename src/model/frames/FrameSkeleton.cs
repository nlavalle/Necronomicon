using necronomicon.source;
using Snappier;
using Steam.Protos.Dota2;

namespace necronomicon.model.frames;

public class FrameSkeleton
{
    private static readonly int isCompressedConstant = (int)EDemoCommands.DemIsCompressed;
    private static readonly int notCompressedConstant = ~isCompressedConstant;

    private int _frameTick;
    private byte[] _streamCache;

    public bool IsCompressed { get; private set; }
    public EDemoCommands FrameCommand { get; private set; }

    public int FrameTick
    {
        get { return _frameTick; }
    }

    internal FrameSkeleton(InputStreamSource inputStreamSource, int command, int tick, int dataSize)
    {
        FrameCommand = (EDemoCommands)(command & notCompressedConstant);
        if ((command & isCompressedConstant) == isCompressedConstant)
            IsCompressed = true;
        _frameTick = tick;
        _streamCache = inputStreamSource.ReadBytes(dataSize);
    }

    public TProtobuf? GetAsProtobuf<TProtobuf>(EDemoCommands command) where TProtobuf : class
    {
        switch (command)
        {
            case EDemoCommands.DemFileHeader:
                return CDemoFileHeader.Parser.ParseFrom(_streamCache) as TProtobuf;
            case EDemoCommands.DemSendTables:
                return CDemoSendTables.Parser.ParseFrom(_streamCache) as TProtobuf;
            case EDemoCommands.DemClassInfo:
                return CDemoClassInfo.Parser.ParseFrom(_streamCache) as TProtobuf;
            case EDemoCommands.DemPacket:
                if (IsCompressed)
                {
                    int uncompressedLength = Snappy.GetUncompressedLength(_streamCache);
                    byte[] uncompressed = new byte[uncompressedLength];
                    Snappy.Decompress(_streamCache, uncompressed);
                    return CDemoPacket.Parser.ParseFrom(uncompressed) as TProtobuf;
                }
                return CDemoPacket.Parser.ParseFrom(_streamCache) as TProtobuf;
            case EDemoCommands.DemSignonPacket:
                return CDemoPacket.Parser.ParseFrom(_streamCache) as TProtobuf;
            case EDemoCommands.DemFullPacket:
                return CDemoFullPacket.Parser.ParseFrom(_streamCache) as TProtobuf;
        }
        return null;
    }
}