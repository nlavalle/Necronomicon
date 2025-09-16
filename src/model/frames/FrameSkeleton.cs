using necronomicon.source;
using Snappier;
using Steam.Protos.Dota2;

namespace necronomicon.model.frames;

public class FrameSkeleton
{
    private readonly byte[] _streamCache;

    public bool IsCompressed { get; }
    public EDemoCommands FrameCommand { get; }

    public int FrameTick { get; }

    internal FrameSkeleton(InputStreamSource inputStreamSource, int command, int tick, int dataSize)
    {
        const int IsCompressedConstant = (int)EDemoCommands.DemIsCompressed;

        if ((command & IsCompressedConstant) != 0)
        {
            IsCompressed = true;
            command ^= IsCompressedConstant;
        }

        FrameCommand = (EDemoCommands)command;
        FrameTick = tick;
        
        var array = new byte[dataSize];
        inputStreamSource.ReadBytes(array);
        _streamCache = array;
    }

    public TProtobuf? GetAsProtobuf<TProtobuf>(EDemoCommands command) where TProtobuf : class
    {
        byte[] protobufCache;
        if (IsCompressed)
        {
            int uncompressedLength = Snappy.GetUncompressedLength(_streamCache);
            protobufCache = new byte[uncompressedLength];
            Snappy.Decompress(_streamCache, protobufCache);
        }
        else
        {
            protobufCache = _streamCache;
        }
        return command switch
        {
            EDemoCommands.DemFileInfo => CDemoFileInfo.Parser.ParseFrom(protobufCache) as TProtobuf,
            EDemoCommands.DemFileHeader => CDemoFileHeader.Parser.ParseFrom(protobufCache) as TProtobuf,
            EDemoCommands.DemSendTables => CDemoSendTables.Parser.ParseFrom(protobufCache) as TProtobuf,
            EDemoCommands.DemClassInfo => CDemoClassInfo.Parser.ParseFrom(protobufCache) as TProtobuf,
            EDemoCommands.DemPacket => CDemoPacket.Parser.ParseFrom(protobufCache) as TProtobuf,
            EDemoCommands.DemSignonPacket => CDemoPacket.Parser.ParseFrom(protobufCache) as TProtobuf,
            EDemoCommands.DemFullPacket => CDemoFullPacket.Parser.ParseFrom(protobufCache) as TProtobuf,
            _ => null,
        };
    }
}
