using necronomicon.model.frames;
using necronomicon.source;
using Steam.Protos.Dota2;

namespace necronomicon.model.engine;

public sealed class DemFile
{
    private HashSet<FrameSkeleton> _frameSkeletons;
    private InputStreamSource _inputStreamSource;

    public DemFile(InputStreamSource inputStreamSource)
    {
        _frameSkeletons = new HashSet<FrameSkeleton>();
        _inputStreamSource = inputStreamSource;
    }


    public HashSet<FrameSkeleton> ParseFilePackets()
    {
        long index = _inputStreamSource.GetPosition(),
            size = _inputStreamSource.GetFileSize();
        int encodedCommand,
            dataSize;
        int frameTick;
        FrameSkeleton frame;

        _frameSkeletons.Clear();

        while (index < size)
        {
            encodedCommand = _inputStreamSource.ReadVarInt32();
            frameTick = _inputStreamSource.ReadVarInt32();
            dataSize = _inputStreamSource.ReadVarInt32();
            index = _inputStreamSource.GetPosition();

            frame = new FrameSkeleton(_inputStreamSource, encodedCommand, frameTick, dataSize);
            _frameSkeletons.Add(frame);

            index += dataSize;
            _inputStreamSource.SetPosition(index);
        }

        return _frameSkeletons;
    }

    public CDemoFileHeader GetFileHeader()
    {
        if (_frameSkeletons.Count == 0)
            ParseFilePackets();

        FrameSkeleton? headerSkeleton = _frameSkeletons.FirstOrDefault(fs => fs.FrameCommand == EDemoCommands.DemFileHeader);
        if (headerSkeleton == null)
            throw new InvalidDataException("Missing File Header");
        
        CDemoFileHeader? cDemoFileHeader = headerSkeleton.GetAsProtobuf<CDemoFileHeader>(EDemoCommands.DemFileHeader);

        if (cDemoFileHeader == null)
            throw new InvalidDataException("Unable to parse file header protobuf");
        return cDemoFileHeader;
    }
}